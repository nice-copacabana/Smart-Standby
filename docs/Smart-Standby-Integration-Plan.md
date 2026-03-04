# Smart-Standby 集成与优化落地方案（针对当前仓库）

> 仓库：`/home/liroot/github_repos/Smart-Standby`
> 
> 目标：把“调研结论（S0/S3/WOL差异）”转成可执行的产品能力，提升 **远程可达性**、**待机可靠性**、**能耗表现** 和 **可解释性**。

---

## 0. 当前项目结构与可利用入口

从现有代码看，项目已具备智能待机基础能力：

- UI 层（WinUI 3）：
  - `src/SmartStandby/SmartStandby/Views/*`
  - `src/SmartStandby/SmartStandby/ViewModels/*`
  - `src/SmartStandby/SmartStandby/Services/TrayIconService.cs`
- Core 层：
  - `PowerMonitorService.cs`（电源状态）
  - `SleepService.cs`（睡眠执行）
  - `BlockerScanner.cs`（阻塞扫描）
  - `NetworkManager.cs`（网络控制）
  - `ProcessGuardian.cs`（进程守护）
  - `DatabaseService.cs`（数据记录）
  - `SystemTweaker.cs`（系统调优）
  - `UpdateService.cs`（更新）
- 配置模型：
  - `Models/AppConfig.cs`

**结论：** 你不需要重造框架，应该在现有服务之上新增“策略层 + 诊断层”。

---

## 1. 产品定义（建议）

### 1.1 Smart-Standby 的一句话定位

**一个“策略驱动”的 Windows 待机可靠性引擎：按设备能力自动选择最稳的待机/唤醒方案，并给出可解释诊断。**

### 1.2 关键用户价值

1. 对 S0 设备（如部分 Legion）避免“看似睡眠、实则耗电发热”
2. 对支持 S3/WOL 的设备提供更稳定远程唤醒
3. 失败时明确告诉用户“为什么失败、怎么修”

---

## 2. 建议新增模块（最小侵入）

在 `src/SmartStandby.Core/Services` 新增：

```text
Services/
  CapabilityProbeService.cs      # 探测 S0/S3/WOL/网卡能力
  StandbyPolicyEngine.cs         # 策略决策
  WakeOrchestrator.cs            # 唤醒执行+回退链路
  SleepDiagnosticsService.cs     # 失败归因+建议
  StandbyProfileService.cs       # 场景模板（办公/外出/夜间）
```

在 `Models` 新增：

```text
Models/
  CapabilityProfile.cs
  PolicyDecision.cs
  WakeResult.cs
  DiagnosticReport.cs
  StandbyProfile.cs
```

在 UI 层新增：

```text
Views/
  SleepHealthPage.xaml           # 一键体检
  PolicyExplainPage.xaml         # 策略解释

ViewModels/
  SleepHealthViewModel.cs
  PolicyExplainViewModel.cs
```

---

## 3. 核心架构（可落地图）

```text
[PowerMonitorService]   [BlockerScanner]   [NetworkManager]   [System info]
         \                   |                    |                /
          \___________________|____________________|_______________/
                               ↓
                    [CapabilityProbeService]
                               ↓
                     CapabilityProfile (结构化能力画像)
                               ↓
                     [StandbyPolicyEngine]
                               ↓
                      PolicyDecision (目标状态+动作链)
                               ↓
          [SleepService] + [WakeOrchestrator] + [ProcessGuardian]
                               ↓
                 执行结果写入 DatabaseService + 事件日志
                               ↓
                [SleepDiagnosticsService] 输出用户可读诊断
                               ↓
                      Dashboard/Settings/Tray 提示
```

---

## 4. 能力探测（Capability Probe）设计

### 4.1 探测目标

- 支持的电源状态（`powercfg /a`）：S0 / S3 / S4
- 网卡能力：是否支持 Magic Packet
- 当前链路：Ethernet / Wi-Fi
- 系统开关：是否允许设备唤醒、是否仅魔术包
- 电源上下文：AC/Battery、当前电量
- 历史可靠性：最近 N 次唤醒成功率

### 4.2 输出模型示例

```csharp
public sealed class CapabilityProfile
{
    public bool HasS0 { get; set; }
    public bool HasS3 { get; set; }
    public bool HasHibernate { get; set; }

    public bool EthernetConnected { get; set; }
    public bool WifiConnected { get; set; }

    public bool WolLanSupported { get; set; }
    public bool WolLanConfigured { get; set; }
    public bool WoWlanSupported { get; set; }

    public bool IsLaptop { get; set; }
    public bool IsAcPowered { get; set; }
    public int BatteryPercent { get; set; }

    public double WakeSuccessRate7d { get; set; }
    public string? RiskHint { get; set; } // 例如: "S0-only device"
}
```

### 4.3 实现建议

- 使用已有 `PowerShellHelper` + `Win32Utils` 组合获取底层信息
- 采样频率：
  - 启动后立即
  - 进入睡眠前
  - 唤醒后
  - 设置变更后

---

## 5. 策略引擎（StandbyPolicyEngine）

### 5.1 输入

- `CapabilityProfile`
- 用户配置（AppConfig）
- 当前情境（是否即将远程连接、时间段、AC/Battery）

### 5.2 输出

```csharp
public sealed class PolicyDecision
{
    public string TargetState { get; set; } = "LightSleep"; // LightSleep/DeepSleep/Hibernate/KeepAlive
    public List<string> ActionChain { get; set; } = new();
    public List<string> Preconditions { get; set; } = new();
    public string ExplainText { get; set; } = string.Empty;
}
```

### 5.3 首版规则（建议）

1. **S3 + Ethernet + WOL配置完整**
   - `TargetState = DeepSleep`
   - `ActionChain = [ScanBlockers, OptionalKill, Sleep]`

2. **S0-only + Laptop（典型 Legion 风险）**
   - `TargetState = KeepAlive`（关屏+降功耗，不深睡）
   - `ActionChain = [ScreenOff, NetworkPolicy, GuardWake]`

3. **Battery < 阈值（如20%）**
   - 强制 `Hibernate` 或 `EcoKeepAlive`

4. **连续唤醒失败 >= 3**
   - 自动降级到“可达性优先模板”并弹提示

---

## 6. 唤醒编排（WakeOrchestrator）

### 6.1 回退链路

```text
Step1: WOL (LAN Magic Packet)
Step2: 重试 WOL（指数回退）
Step3: 若仍失败 -> 标记能力下降 + 建议切换 KeepAlive
Step4: 记录诊断报告
```

> 注：当前项目看起来偏本机工具，若未来有“跨网远程入口”，可在 Step3 扩展中继唤醒。

### 6.2 结果模型

```csharp
public sealed class WakeResult
{
    public bool Success { get; set; }
    public string Method { get; set; } = "WOL";
    public int Attempts { get; set; }
    public long LatencyMs { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureDetail { get; set; }
}
```

---

## 7. 诊断系统（SleepDiagnosticsService）

### 7.1 错误码标准化

- `CAP_S0_ONLY`
- `CAP_WOL_UNSUPPORTED`
- `CFG_MAGIC_PACKET_DISABLED`
- `CFG_ALLOW_WAKE_DISABLED`
- `NET_ETHERNET_DISCONNECTED`
- `WAKE_TIMEOUT`
- `WAKE_UNSTABLE_CONSECUTIVE_FAIL`

### 7.2 每个错误码附带

- 用户可读解释
- 操作步骤（最多 3 条）
- 是否可自动修复（true/false）

### 7.3 现有 Wake-up Health Report 联动

你已经分析 Event IDs 41/107/109，可把事件分析结果并入诊断报告：

- `PowerSessionQualityScore`
- `UnexpectedWakeDetected`
- `ThermalRiskAfterResume`

---

## 8. AppConfig 扩展建议

在 `AppConfig.cs` 增加：

```csharp
public sealed class SmartStandbyOptions
{
    public string Mode { get; set; } = "Balanced"; // Reachability/Balanced/Eco
    public bool AutoProbeOnStartup { get; set; } = true;
    public int ProbeIntervalMinutes { get; set; } = 360;

    public int BatteryLowThresholdPercent { get; set; } = 20;
    public int ConsecutiveWakeFailToDowngrade { get; set; } = 3;

    public bool EnablePolicyExplain { get; set; } = true;
    public bool EnableAutoDowngrade { get; set; } = true;

    public int WakeTimeoutMs { get; set; } = 45000;
    public int WakeMaxRetries { get; set; } = 2;
}
```

并在设置页增加可视化开关。

---

## 9. UI/交互改造建议

### 9.1 Dashboard（现有页增强）

新增卡片：

- 当前设备睡眠类型：S0 / S3
- 当前策略模式：可达性优先 / 平衡 / 省电
- 最近 24h 唤醒成功率
- 当前风险提示（如：S0-only）

### 9.2 Settings（现有页增强）

新增模块：

- 智能策略模式切换
- 自动降级开关
- 电池阈值与重试次数
- 一键运行“睡眠体检”按钮

### 9.3 Tray（现有增强）

右键新增：

- `Run Sleep Health Check`
- `Switch to Reachability Mode`
- `Switch to Eco Mode`

---

## 10. 数据持久化与指标

### 10.1 数据表建议（若 SQLite）

- `capability_snapshots`
- `policy_decisions`
- `wake_attempts`
- `diagnostic_reports`

### 10.2 关键指标

- 唤醒成功率（24h/7d）
- 平均唤醒耗时
- 连续失败次数
- 自动降级触发次数
- 每模式平均耗电（估算）

---

## 11. 与现有服务的具体集成点

### 11.1 `SleepService.cs`

- 在执行睡眠前调用 `StandbyPolicyEngine.Decide()`
- 按 `PolicyDecision` 执行动作链

### 11.2 `PowerMonitorService.cs`

- 在 resume 后触发 `CapabilityProbeService.Probe()`
- 触发 `SleepDiagnosticsService.AnalyzeLastSession()`

### 11.3 `NetworkManager.cs`

- 根据策略动态执行“睡前断网/唤醒重连”
- 对 S0 模式优化为“限制网络活性”而非粗暴断开（可选）

### 11.4 `BlockerScanner.cs` + `ProcessGuardian.cs`

- 将 blocker 严重级别映射到策略决策：
  - high -> 提示/强制处理
  - medium -> 警告
  - low -> 仅记录

### 11.5 `DatabaseService.cs`

- 增加策略与唤醒记录写入接口

---

## 12. 分阶段实施计划（建议）

### Phase 1（1 周）：策略可见

- 完成 CapabilityProbeService
- 完成 StandbyPolicyEngine v1（规则表）
- Dashboard 显示“设备类型+当前策略+风险提示”

### Phase 2（1~2 周）：执行闭环

- SleepService 接入策略链
- WakeOrchestrator + 失败归因
- Settings 增加模式切换与阈值配置

### Phase 3（1 周）：体验强化

- Sleep Health Page（一键体检）
- 诊断建议可复制
- Tray 快速切换策略

### Phase 4（可选，1~2 周）：自学习

- 基于历史成功率自动调参
- 设备画像模板（Legion/台式机/轻薄本）

---

## 13. 测试计划

### 13.1 单元测试

- 策略引擎输入矩阵测试（S0/S3、Ethernet/Wi-Fi、AC/Battery）
- 错误码映射测试
- 配置边界值测试

### 13.2 集成测试

- 睡前扫描 -> 策略决策 -> 执行 -> 记录 -> 诊断全链路
- 连续失败自动降级

### 13.3 实机矩阵

- Legion（S0-only）
- 支持 S3 的台式机
- 支持 S3 的旧笔记本
- AC 与电池场景

验收线建议：

- S3+有线：唤醒成功率 ≥ 95%
- S0 场景：策略误判率 < 5%
- 自动降级命中率 100%

---

## 14. 风险与回滚

### 风险

- 不同厂商驱动命名差异导致探测误判
- 过度 aggressive 的阻塞清理可能影响用户任务

### 回滚策略

- 所有新策略受 `SmartStandbyOptions.Enabled` 总开关控制
- 保留“Legacy Sleep Path”可一键回退
- 配置变更支持导出/恢复

---

## 15. 可以立刻做的三件事（高 ROI）

1. **先做 Capability Probe + Dashboard 展示**（最快让用户“看懂自己的机器”）
2. **接入策略引擎 v1**（先规则，不上复杂学习）
3. **补齐失败归因文案**（把“失败”变成“可修复行动”）

---

## 16. 命名建议（对外）

- 功能名：`Smart-Standby Engine`
- UI 文案：`智能待机策略`
- 子功能：
  - `Sleep Health Check`（睡眠体检）
  - `Policy Explain`（策略解释）
  - `Wake Reliability`（唤醒可靠性）

---

## 17. 结论

Smart-Standby 已有坚实底座（电源监控、阻塞扫描、网络管理、会话分析）。
下一步不是堆功能，而是把这些能力放进“探测 -> 决策 -> 执行 -> 诊断”的闭环，形成可解释、可稳定复用的智能待机系统。

## Implemented in code

- Added models in `src/SmartStandby.Core/Models`:
  - `CapabilityProfile.cs`
  - `PolicyDecision.cs`
  - `SmartStandbyOptions.cs`
- Added services in `src/SmartStandby.Core/Services`:
  - `CapabilityProbeService.cs`
  - `StandbyPolicyEngine.cs`
- Updated `AppConfig.cs` with `[Ignore]` property:
  - `SmartStandbyOptions SmartStandbyOptions { get; set; } = new();`
- Updated `SleepService.cs`:
  - Added `PlanSleepAsync()` to call capability probe + policy engine
  - Wired `ExecuteSmartSleepAsync()` to invoke planning path without changing existing execution behavior
  - Added `LastPolicyDecision` and policy explain logging
- Updated `PowerMonitorService.cs`:
  - Added `ResumeReprobeRequested` event
  - Raised this event after resume handling logic
- Updated DI registrations in `src/SmartStandby/SmartStandby/App.xaml.cs`:
  - `CapabilityProbeService`
  - `StandbyPolicyEngine`
- Enhanced `DatabaseService.cs`:
  - Added `GetSmartStandbyOptionsAsync()` / `SetSmartStandbyOptionsAsync()` JSON persistence helpers
- Enhanced dashboard policy visibility:
  - `DashboardViewModel` now exposes `CurrentPolicyTarget`, `PolicyExplainText`, `CapabilitySummary`
  - `DashboardPage.xaml` now shows policy/capability summary and adds `Policy Refresh` action

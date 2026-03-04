# Smart-Standby 实施清单（代码落地版）

## Milestone A：策略闭环最小可用（本轮）

- [x] 新增 `CapabilityProfile` 模型
- [x] 新增 `PolicyDecision` 模型
- [ ] 新增 `DiagnosticReport` 基础模型
- [x] 新增 `CapabilityProbeService`（S0/S3、网络、基础上下文探测）
- [x] 新增 `StandbyPolicyEngine`（规则引擎 v1）
- [x] 在 `SleepService` 增加策略接入点（预决策）
- [x] 在 `PowerMonitorService` 增加 resume 后重新探测钩子
- [x] 在 `AppConfig` 增加 `SmartStandbyOptions`
- [ ] 单元测试：策略引擎规则测试

## Milestone B：可解释与可诊断

- [ ] 新增 `SleepDiagnosticsService`
- [ ] 统一失败码与建议文案
- [x] Dashboard 展示“当前能力画像+策略解释”
- [x] 设置页增加策略模式与阈值开关

## Milestone C：唤醒编排与可靠性

- [ ] 新增 `WakeOrchestrator`
- [ ] 回退链路：WOL -> 重试 -> 降级建议
- [ ] 连续失败自动降级策略
- [ ] 写入统计数据（成功率、耗时、失败原因）

## 验收标准（建议）

- [ ] 不破坏现有 Smart Sleep 主流程
- [ ] S0 设备能正确识别并输出降级策略
- [ ] S3 场景能输出 DeepSleep 决策
- [ ] 策略解释文案可在 UI 侧直接展示

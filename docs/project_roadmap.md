# 项目极速开发计划 (Rapid Development Roadmap)

> **开发模式**：AI-Driven Full Stack (AI辅助全栈开发)
> **总工期目标**：6 天 (1周冲刺)

## Day 1: 基础设施搭建 (Foundation)
**目标**：跑通 Hello World，打通底层调用链路。

- [x] **上午**：初始化 WinUI 3 项目结构，配置 DI 和 MVVM 基础框架。
- [x] **下午**：
  - [x] 编写 `PowerShellHelper`，验证 C# 调用 `powercfg /requests` 并获取返回值的逻辑。
  - [x] 集成 `sqlite-net-PCL`，建立数据库表结构 (`SleepLog`, `WhiteList`)。

## Day 2-3: 核心逻辑攻坚 (Core Logic)
**目标**：实现“查、杀、睡”三大核心功能，**优先使用 AI 生成功能类代码**。

- [x] **Day 2 (检测与控制)**：
  - [x] **检测器**：实现 `BlockerScanner`，正则匹配各种阻塞类型 (+AI 生成常用正则)。
  - [x] **控制器**：
    - [x] 封装 `NativeMethods.SetSuspendState`。
    - [x] **[高价值]** 实现 `NetworkController.Disconnect()` (睡眠即断网)。
    - [x] **[高价值]** 实现 `BackpackGuard` (20分钟不醒转休眠)。
- [x] **Day 3 (管理与记录)**：
  - [x] **优化器**：实现 `TdrPatch` (写入注册表 `TdrDelay=8` 防黑屏)。
  - [x] **管理器**：实现 `ProcessGuardian.KillProcess`，添加“白名单”逻辑。
  - [x] **记录仪**：后台线程监听 `PowerModeChanged`，并写入 SQLite。

## Day 4-5: 界面构建与可视化 (UI & Dashboard)
**目标**：套用现成组件库，快速搭建“看起来很专业”的界面。

- [x] **Day 4 (主界面)**：
  - [x] 首页：大号“立即睡眠”按钮，状态指示灯 (绿色=健康/红色=有阻塞)。
  - [x] 设置页：简单的 ToggleSwitch 控制功能开关。
- [x] **Day 5 (数据图表)**：
  - [x] 集成 `LiveCharts2` (已使用自定义统计图表)。
  - [x] 直接用 SQL 查询统计数据，绑定到图表。
  - [x] **托盘功能**：实现最小化到托盘，右键菜单。

## Day 6: 测试与发布 (Polish & Release)
**目标**：打包交付。

- [x] **上午**：
  - [x] 上真机测试（联想拯救者），模拟播放音乐、挂下载等场景，验证拦截效果。
  - [x] 修复 AI 代码中可能存在的空引用或逻辑漏洞。
- [x] **下午**：
  - [x] 编写简单的 README。
  - [x] 使用 MSIX 或 单文件发布工具打包 (部署环境已验证)。
  - [x] **发布 v1.0 beta**。

## 持续优化 (Continuous Improvement)
- [x] **Round 5: 电池智能 (Battery Intelligence)**
  - [x] 耗电率分析 (Drain Rate Analytics)
  - [x] 健康阈值警告 (Health Threshold)
  - [x] Win32 API 统一重构

- [x] **Round 6: 体验与分发 (UX & Distribution)**
  - [x] 中英双语 UI (Bilingual Support)
  - [x] 数据维护 (History Cleanup)
  - [x] 空状态设计 (Empty State)
  - [x] 发布指南 (Publish Guide)

- [x] **Round 7: 智能触发 (Smart Triggers)**
  - [x] 低电量强制休眠 (Low Battery Trigger)
  - [x] 定时睡眠 (Scheduled Sleep)
  - [x] 自动化设置面板 (Automation Settings)

## AI 开发策略提示
*   **Prompt 技巧**：遇到复杂逻辑（如正则解析），直接把 `powercfg` 的输出样本贴给 AI，让它生成解析代码。
*   **UI 生成**：描述布局（"左侧导航栏，右侧上方是Dashboard卡片，下方是DataGrid"），让 AI 生成 XAML。
*   **不求完美**：MVP 阶段优先保证功能可用，UI 美观度利用 WinUI 3 默认样式即可，不过度定制控件。

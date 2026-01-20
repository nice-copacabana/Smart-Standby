# 详细模块设计 (Detailed Module Design) - Windows Sleep Guardian

## 1. 总体架构 (Architecture Overview)
由于追求 **AI全栈快速开发**，本项目采用 **单体特权应用 (Monolithic Admin App)** 架构。
主程序 **SleepGuardian.exe** 默认以 **管理员权限 (RequireAdministrator)** 启动，同时承载 UI 展示、后台监控和底层控制逻辑。这种设计极大地降低了开发复杂度（无需处理 Service 与 UI 的 IPC 通信），适合个人工具类软件。

```mermaid
graph TD
    User[用户] <--> UI[UI Layer (WinUI 3)]
    UI <--> Logic[Business Logic Layer]
    Logic <--> Sys[System Integration (PowerShell/Native API)]
    Logic <--> DB[(SQLite Database)]
```

## 2. 核心模块详解 (Core Modules)

对应立项文档的 5 大功能模块：

### 2.1 智能阻塞检测器 (Smart Blocker Detector)
*   **职责**：识别“谁在阻止睡眠”。
*   **核心类**：`BlockerScanner`
*   **功能**：
    *   `ScanCurrentBlockers()`: 调用 `powercfg /requests`，正则解析输出。
    *   `EnrichProcessInfo()`: 将 PID/DriverID 映射为友好的应用名称（如 `Audiodg.exe` -> "Windows 音频图引擎"）。
    *   `GetBlockerType()`: 分类为 Display, System, AwayMode, Execution。

### 2.2 进程安全管理器 (Process Safety Manager)
*   **职责**：处理阻塞源，维护白名单，以及 **执行系统级优化**。
*   **核心类**：`ProcessGuardian`, `SystemOptimizer`
*   **功能**：
    *   `KillProcess(pid)`: 强制结束非核心阻塞进程。
    *   `DevideDisableWake(deviceId)`: 禁用硬件设备（如鼠标、网卡）的唤醒权限 (Input Isolation)。
    *   `ApplyTdrPatch()`: **[关键优化]** 修改注册表 `TdrDelay` 至 8-10s，防止唤醒黑屏/死机 (Black Screen Mitigation)。
    *   `WhitelistManager`: 加载/保存 `whitelist.json`。

### 2.3 睡眠控制与验证器 (Sleep Control & Validator)
*   **职责**：执行睡眠动作，并在唤醒后“验尸”。
*   **核心类**：`SleepCommander`
*   **功能**：
    *   `SafeSleep()`:
        1.  执行 `BlockerScanner` -> 清理阻塞。
        2.  **[关键优化] Network Silencer**: 调用 `WlanDisconnect` 主动断网，防止邮件/更新在背包中唤醒电脑。
        3.  调用 `SetSuspendState` 进入 S0/S3。
        4.  开启 "Backpack Guard" 计时器：若睡眠时长 > 20分钟 且 未唤醒 -> 自动转入 **Hibernate** 彻底断电。
    *   `ValidateWakeup()`:
        1.  在系统 `PowerModeChanged` (Resume) 事件触发后延时 5秒 执行。
        2.  检查 `System Event Log` 中是否有 Error (ID 41, 109, 506)。
        3.  返回 `WakeupHealthReport` (成功/失败，耗时，错误代码)。

### 2.4 睡眠事件记录仪 (Sleep Event Logger)
*   **职责**：静默记录全生命周期数据。
*   **核心类**：`EventLogger`
*   **功能**：
    *   `StartListening()`: 订阅 `SystemEvents.PowerModeChanged` 和 `EventLogWatcher`。
    *   `LogSession(session)`: 将一次完整的 Sleep-Wake Cycle 存入 `SleepHistory` 表。
    *   **数据表设计**：
        *   `TimeStamp` (DateTime)
        *   `Action` (Sleep/Wake/Block/Crash)
        *   `Source` (App Name / Wake Source)
        *   `BatteryLevel` (Int)

### 2.5 数据可视化面板 (Data Dashboard)
*   **职责**：展示健康度与排行榜。
*   **核心类**：`DashboardViewModel`
*   **功能**：
    *   `GetWeeklyHealthScore()`: 计算 (成功睡眠次数 / 总尝试次数) * 100。
    *   `GetTopOffenders()`: SQL 聚合查询，返回最常阻止睡眠的应用列表。
    *   **图表**：
        *   饼图：阻塞原因分布 (Audio / Network / App)。
        *   柱状图：过去7天睡眠成功率。

## 3. 关键业务流程 (Key Flows)

### 3.1 "一键修复并睡眠" 流程
1.  用户点击 UI "立即睡眠" 按钮。
2.  `BlockerScanner` 扫描当前请求。
3.  发现 `Chrome.exe` (Audio Routine) 正在阻塞。
4.  `ProcessGuardian` 检查白名单 -> Chrome 不在白名单中。
5.  `ProcessGuardian` 弹窗提示或自动 Kill 掉 Chrome 进程（根据设置）。
6.  `SleepCommander` 调用底层 API 触发睡眠。
7.  系统进入低功耗模式。

### 3.2 "唤醒后体检" 流程
1.  用户开盖，系统唤醒。
2.  `EventLogger` 捕获取 `Resume` 事件。
3.  `SleepCommander` 等待 5秒（等待系统服务完全加载）。
4.  调用 PowerShell 查询 `Get-WinEvent -LogName System -MaxEvents 10`。
5.  分析是否包含 "上次意外关闭" 也不包含 "Hibernation 失败" 日志。
6.  系统托盘弹出 Toast 通知："系统已从睡眠唤醒，耗时 1.2s，状态健康。"

## 4. 目录结构规划 (Project Structure)
```
/SmartStandby
  /Core
    /Helpers       # PowerShellHelper.cs, NativeMethods.cs
    /Models        # SleepSession.cs, BlockerInfo.cs
    /Data          # DatabaseContext.cs (SQLite)
  /Modules
    /Detection     # BlockerScanner.cs
    /Management    # ProcessGuardian.cs
    /Control       # SleepCommander.cs
  /UI
    /Views         # DashboardPage.xaml, SettingsPage.xaml
    /ViewModels    # MVVM Logic
  App.xaml.cs      # Entry Point
```

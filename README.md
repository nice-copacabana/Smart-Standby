# Smart Standby (S3 Enforcer)

**Smart Standby** is a Windows utility designed to fix the "Modern Standby" (S0 Low Power Idle) issues that plague many modern laptops (battery drain, overheating in bags). It attempts to enforce strict sleep behavior by monitoring power states, disconnecting networks, and aggressively handling blocking processes.

## ✨ Key Features

*   **Smart Sleep Orchestration**: Automatically manages S3/Modern Standby transitions.
*   **Backpack Guard**: Detects unexpected wake-ups in a bag and forces hibernation after 20 minutes if inactive.
*   **Wake-up Health Report**: Automatically analyzes system event logs (IDs 41, 107, 109) after resume to detect power session quality.
*   **Enhanced Tray Control**: Right-click context menu (Sleep Now, Exit) and double-click to restore window.
*   **Blocker Awareness**: Scans for 7+ types of system-wide sleep blockers (Audio, Execution, Display, etc.) and optionally kills them.
*   **Network Silence**: Optionally disconnects Wi-Fi on sleep to prevent "Wake for network" drain, and reconnects on wake.
*   **Asset Safety**: Whitelist support for critical processes.
*   **Auto-Update**: Built-in version check and update notifications.

## 🛠️ Installation & Build

### Prerequisites
*   Windows 10 (1903+) or Windows 11
*   .NET 8.0 SDK
*   Visual Studio 2022 (with WinUI 3 / Windows App SDK workloads)

### Building from Source
1.  Clone the repository.
2.  Open `SmartStandby.sln` in Visual Studio 2022.
3.  Restore NuGet packages.
4.  Build the solution (Recommended Configuration: `Release` | `x64`).
5.  Deploy/Run the `SmartStandby` (Package) project.

## 📖 Usage Guide

### Dashboard
*   **Status**: Shows if the system is ready for sleep or if blockers are detected.
*   **Smart Sleep**: Click this button to initiate the "Safe Sleep" sequence.
*   **Recent Activity**: A chart showing your recent sleep durations.

### Settings
*   **Network Disconnect**: Toggle to enable/disable Wi-Fi cutting on sleep.
*   **TDR Patch**: Apply registry fix for GPU timeouts (Requires Admin restart to fully take effect).
*   **Whitelist**: Add process names (e.g., `notepad.exe`) to prevent them from being killed by the "Force Sleep" logic.

## ⚠️ Disclaimer
This tool modifies system power settings and network interfaces. While safety checks are in place, the authors are not responsible for any data loss or system instability. The "TDR Patch" involves Windows Registry modification.

## 📄 License
Apache 2.0

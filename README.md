# Smart Standby (S3 Enforcer)

**Smart Standby** is a Windows utility designed to fix the "Modern Standby" (S0 Low Power Idle) issues that plague many modern laptops (battery drain, overheating in bags). It attempts to enforce strict sleep behavior by monitoring power states, disconnecting networks, and aggressively handling blocking processes.

## 🚀 Key Features

*   **Smart Sleep Trigger**: A "One-Click" sleep button that prepares the system for deep sleep.
*   **Network Silence**: Automatically disconnects Wi-Fi on sleep to prevent "Wake for network" battery drain, and reconnects on wake.
*   **Blocker Killer**: Scans for known processes (like Steam downloads or Video Players) ensuring the PC stays awake, and optionally kills them to force sleep. **Includes safety guards** to prevent killing critical system processes.
*   **Deep Sleep Statistics**: Visualizes your sleep/wake cycles and **logs battery consumption** per session.
*   **Run on Startup**: Optional setting to launch Smart Standby effectively when Windows starts.
*   **TDR Patch Management**: optional registry tweaks to fix black screen/timeout issues on NVIDIA/AMD GPUs during sleep transitions.
*   **Process Whitelist**: Configurable list of critical processes that should never be killed.

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

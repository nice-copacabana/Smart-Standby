# Release Notes - Smart Standby v1.0.0-beta

**Build Date**: 2026-01-22
**Status**: Feature Complete / Beta

## 🌟 Highlights
Smart Standby is now a comprehensive power management utility for Windows Modern Standby devices.

- **Intelligent Sleep**: Blocks unwanted wake-ups and enforces strict sleep policies.
- **Battery Analytics**: Tracks drain rate (%/hour) for every session to identify energy hogs.
- **Smart Triggers**: Proactively sleeps your device on **Low Battery** or **Schedule**.
- **Localization**: Fully supports **English** and **Chinese (Simplified)**.

## 🚀 New Features (v1.0.0)
- **Dashboard**: realtime status, proactive health warnings, and 7-day sleep duration charts.
- **Backpack Guard**: Automatically hibernates the device if it wakes up in a bag (20-min timeout).
- **Network Killswitch**: Optional setting to disconnect Wi-Fi during sleep to prevent background updates.
- **Process Whitelist**: Prevent specific applications (e.g., downloads) from being killed.
- **Automation**:
    - Low Battery Force Sleep (configurable threshold).
    - Scheduled Sleep Time.

## 🛠 Improvements
- **Win32 Refactor**: Consolidated all native calls for better stability.
- **TDR Patch**: Built-in fix for NVIDIA "Black Screen on Wake" issues.
- **Data Management**: One-click history cleanup and easy log access.

## 📦 How to Install
See [PUBLISH_GUIDE.md](docs/PUBLISH_GUIDE.md) for instructions on building a single-file portable EXE.

---
*Developed with AI-Driven Engineering.*

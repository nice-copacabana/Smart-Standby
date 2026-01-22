# Publish Guide - Smart Standby

This guide explains how to package and distribute the Smart Standby application for end-users.

## Prerequisites
- .NET 8 SDK or later
- Visual Studio 2022 with "Windows App SDK" workload

## Option 1: Self-Contained Single EXE (Recommended for portable use)
This method produces a single `.exe` file that includes all dependencies (no .NET runtime required on the target machine).

1. Open a terminal in the project root: `d:\Develop\outworks\Smart-Standby\src\SmartStandby\SmartStandby`
2. Run the following command:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
   ```
3. Locate the output: `bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\SmartStandby.exe`

## Option 2: MSIX Installer (Recommended for store/formal install)
1. Open the solution in Visual Studio.
2. Right-click the `SmartStandby` project -> **Publish** -> **Create App Packages**.
3. Choose **Sideloading** if not using Microsoft Store.
4. Follow the wizard to generate the `.msix` bundle.

## Post-Publish Checklist
- [ ] Verify the `logs` folder is created in the same directory as the EXE upon first run.
- [ ] Verify the `SmartStandby.db3` is created in `%LOCALAPPDATA%\SmartStandby`.
- [ ] Test on a clean machine to ensure no "Missing Runtime" errors occur.

---
**Build Status**: Verified (v1.0.0-beta)

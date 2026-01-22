# Manual Test Cases - Smart Standby

Follow these steps to verify the newly implemented features in Round 1-3.

## 1. Tray Icon & Window Management
- [ ] **TC-01: Double-Click Restore**
    - Minimize the application to the tray (Close the window or minimize).
    - Double-click the tray icon.
    - **Expected**: Application window restores to front.
- [ ] **TC-02: Context Menu**
    - Right-click the tray icon.
    - **Expected**: Menu appears with "Open", "Sleep Now", and "Exit".
- [ ] **TC-03: Quick Sleep**
    - Right-click tray -> Select "Sleep Now".
    - **Expected**: System enters sleep mode immediately.

## 2. Backpack Guard (Inactivity Hibernation)
- [ ] **TC-04: Watchdog Trigger**
    - Put the system to sleep via the app.
    - Wake the PC up (manually or simulate).
    - Open the log file (or observe Debug output).
    - **Expected**: Log should show "Backpack Guard Watchdog started... 20m".
- [ ] **TC-05: Watchdog Cancellation**
    - Wake the PC.
    - Restore the app window from the tray within 20 minutes.
    - **Expected**: Log should show "Backpack Guard: Watchdog stopped due to user activity."

## 3. Wake-up Health Report
- [ ] **TC-06: Automatic Check**
    - Resume system from sleep.
    - Wait 5-10 seconds.
    - Go to the Dashboard.
    - **Expected**: "Wake Health" status should show (Healthy or Warning) and a descriptive message should appear.

## 4. Updates & Settings
- [ ] **TC-07: Version Check**
    - Go to Settings -> About & Updates.
    - Click "Check for Updates".
    - **Expected**: Should show "v1.0.0" and "Latest version already installed" (based on placeholder logic).

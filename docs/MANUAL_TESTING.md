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
    - **Expected**: Should show "v1.0.0" and "Latest version already installed" (based on placeholder logic).

## 6. Round 5 Enhancements (Battery Intelligence)
- [ ] **TC-11: Battery Drain Analytics**
    - Perform a sleep session (at least 5-10 mins).
    - Go to the Dashboard -> Recent Sessions.
    - **Expected**: A value like "0.5%/h" should appear in bold next to the duration.
- [ ] **TC-12: Health Threshold Trigger**
    - Go to Settings -> Battery Drain Health Threshold.
    - Set the threshold to a very low value (e.g., 0.5%).
    - Perform a sleep session where the drain is likely higher than 0.5%.
    - **Expected**: The session in history should have a **Red** or **Orange** drain rate color, and "Wake Health" status should show a warning about high drain.
- [ ] **TC-13: UI Refinement (Color Coding)**
    - Observe the session list.
    - **Expected**: Healthy drain rates (e.g., < 2%) should be Gray/Neutral. High drain rates should be clearly distinct (Orange/Red).

## 7. Round 6 Enhancements (UX & Maintenance)
- [ ] **TC-14: Bilingual UI (Localization)**
    - Switch your Windows Display Language to Chinese (or English if already in CN).
    - Launch the app.
    - **Expected**: UI text (Dashboard, Settings titles) should match the system language.
- [ ] **TC-15: History Cleanup**
    - Go to Settings.
    - Click **"Clear Session History"**.
    - Go back to Dashboard.
    - **Expected**: The history list should be empty, and the "No sessions recorded yet" message should appear.
- [ ] **TC-16: Log Access**
    - Go to Settings.
    - Click **"Open Logs Folder"**.
    - **Expected**: Windows Explorer should open to the `logs` directory within the application folder.

## 8. Round 7 Enhancements (Automation)
- [ ] **TC-17: Low Battery Trigger (Simulation)**
    - Go to Settings -> Automation.
    - Enable "Low Battery Force Sleep". Set threshold to a value higher than current battery (if unplugged) OR mock logic.
    - *Note*: For real test, set to e.g. 50% and unplug power while battery is 40%.
    - **Expected**: Notification "Low Battery (40%). Entering Smart Sleep..." appears, followed by system sleep.
- [ ] **TC-18: Scheduled Sleep**
    - Go to Settings -> Automation.
    - Enable "Scheduled Sleep".
    - Set time to 1 minute from now.
    - Wait.
    - **Expected**: At the target time, Notification "Scheduled Sleep time reached..." appears, followed by system sleep.

## 5. Round 4 Enhancements (Persistence & Safety)
- [ ] **TC-08: Health Status Persistence**
    - Ensure a "Wake Health" status is visible on the Dashboard (e.g., from TC-06).
    - Close the application completely (Exit from Tray).
    - Restart the application.
    - **Expected**: The "Wake Health" status and message should remain visible and unchanged.
- [ ] **TC-09: Session History**
    - Complete a full sleep/wake cycle.
    - Go to the Dashboard.
    - **Expected**: A new entry should appear in the "Recent Sessions" list with correct time and calculated duration.
- [ ] **TC-10: Whitelist Enforcement**
    - Go to Settings -> Process Whitelist.
    - Add `notepad.exe` (or another safe app you have open).
    - Go to Dashboard -> Click "SMART SLEEP" (which uses Force mode).
    - Wake the PC.
    - **Expected**: `notepad.exe` should still be running. Other non-whitelisted apps (if previously configured to be blocked) should be killed or ignored depending on current logic.

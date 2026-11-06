# Troubleshooting and removal

This guide covers the v1.0.0 GitHub preview. Use [Support](../SUPPORT.md) to report
problems and [Security](../SECURITY.md) for private vulnerability reports.

## Windows rejects warmth or dimming

A readable gamma ramp means the driver returned its current table. It does not
establish write support. Manual and automatic modes use the same driver path;
switching modes cannot grant missing capabilities.

The app bounds generated ramps to Windows' supported range. Strong dimming and
warmth may be reduced; the control status explains this. A limited result differs
from rejection or an unconfirmed write. Slider values show requested settings,
not measured brightness or colour temperature.

If the current package still reports rejection, use Windows display settings,
Windows Night light or monitor controls. Check whether HDR, a full-screen app
or another colour tool controls the output. Administrator access and disabling
security software are not general workarounds. Include the status message and
driver version in a report. Use a calibrated workflow for colour-sensitive work;
HDR and concurrent calibration are not validated here.

## Hardware brightness does not respond

DDC/CI must be supported and enabled in the monitor menu. Docks, adapters and
cables may block it. Try a direct supported connection and Refresh. A sent
command without readback is unconfirmed. MonitorMate keeps rejected values
retryable rather than caching them as successfully applied.

## Restore colour and leave an overlay

Pause care or exit normally to attempt software colour restoration. Hardware
brightness stays at its last value unless explicitly reset or changed with monitor
controls. Reset reports failures instead of claiming unconditional restoration.
Avoid deleting gamma records while MonitorMate is active. Forced termination
cannot perform cleanup; use system/monitor controls if normal restoration fails.

Esc ends focus when Windows grants the registration. The Focus page and tray
provide other exits. Manually requested breaks remain skippable. Strict scheduled
Windows breaks have different skip behavior. Linux/macOS preview breaks are
skippable; closing the app stops reminders.

## Automatic changes and startup

If MonitorMate is in the tray after sign-in but the display stays unchanged,
check Settings > General. **Launch at Windows sign-in** starts the app.
**Restore my last profile on launch** applies the saved display adjustments.
Enable both for that behavior and approve the app's automatic-control prompt.
The live summary explains what will happen. A daily schedule takes priority
when enabled, and paused care remains paused. No administrator elevation is needed.

Restoration uses saved per-display values, including custom brightness and warmth.
Self-contained release builds use the native apphost; framework-dependent DLL
copies still need the .NET Desktop Runtime. Windows Task Manager can independently
disable an app's startup entry. MonitorMate does not override that Windows choice.


First launch offers manual or automatic display control. You can use manual
controls after declining automation and revisit the choice later. Scheduling
and launch/resume restoration require app consent. Start with Windows is a
separate per-user Run entry. No choice requests administrator or capture permission.

## Update or uninstall Windows

1. Exit normally from the tray or Settings. Closing the main window can leave
   MonitorMate running in the tray.
2. Run the current installer. It includes the runtime and installs for your
   account into `%LOCALAPPDATA%\Programs\MonitorMate`.
3. To uninstall, disable Start with Windows first if enabled, exit normally,
   then use Windows Settings > Apps > MonitorMate. The installer checks that
   the app is closed.
4. Uninstall preserves settings/history. To discard data too, note Settings >
   Open data folder before exit, then remove that resolved folder. The default
   is `%APPDATA%\MonitorMate`; an environment override may change it.
5. Review backups and exports separately. Data removal is not secure erasure
   and may be irreversible.

Older script-installed or extracted copies may lack an Apps entry. Remove only
their chosen app folder and shortcuts after exit. Source/framework-dependent
builds need .NET 10 Desktop Runtime; release installers include the runtime.
Store/MSIX preparation files do not establish a shipped Store edition.

## Remove Linux or macOS reminders

Close the app, then remove the AppImage or matching app from Applications. To
discard saved rhythm and totals, remove `MonitorMate/ReminderPreview` under the
OS local application-data directory. App removal alone does not remove settings.
These builds have no background service or auto-start.

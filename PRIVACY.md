# MonitorMate privacy

Updated 5 November 2026. Publisher: Benedictus RH
([@benedictusrey](https://github.com/benedictusrey)). This policy describes the
v1.0.0 GitHub preview. Windows and Linux/macOS reminders have separate data stores.

## What stays on your device

MonitorMate works without an app account or network connection. Reviewed source
contains no app-operated telemetry client, advertising SDK, upload service,
payment flow or account database. It does not capture screen images, keystroke
content, clipboard content, window titles, browsing history or per-app activity.

| App | Stored information | Default location |
|---|---|---|
| Windows | Display identifiers/preferences, typed solar coordinates, gamma baselines, aggregate activity totals, settings and logs. | `%APPDATA%\MonitorMate` |
| Linux/macOS reminders | Work/break rhythm, completed-break total and unreadable-settings backups. | OS local application-data directory under `MonitorMate/ReminderPreview` |

Windows checks idle duration, presentation/full-screen notification state and
pointer position for reminder/focus behavior. It registers chosen global shortcuts.
These checks do not record the content of your work. Copy diagnostics writes text
to the clipboard at your request. Solar scheduling uses coordinates you type,
without requesting system location access.

The reminder preview does not measure idle time, screen time or display state.
Closing it stops reminders. Its settings do not migrate into Windows history
or gamma records.

## Files, backups and retention

On Windows, `MONITOR_MATE_DATA_DIR` overrides the default folder. Settings > Open
data folder shows the resolved location. Files include `settings.json`,
`usage.json`, `gamma-baselines.json`, logs and requested diagnostics. Logs may
contain monitor names, paths and exception details.

Windows history filters records older than 120 days when loaded and persists
the filtered store on a later dirty flush. This is not immediate secure erasure.
Logs rotate around 1 MB, retaining the current file and one rotated file.
Settings, baselines, corruption backups and exported diagnostics have no automatic
expiry. The portable reminder total has no automatic expiry either.

MonitorMate does not encrypt these files. Other accounts with file access,
administrators, backup services and roaming/sync software may read copies.
Choose a private data folder if your records are sensitive.

## Delete your data

Reset history clears Windows usage history while retaining settings, baselines,
logs and exports. Windows uninstall preserves the data folder. To remove it,
first exit normally so colour restoration can run, then delete only the resolved
MonitorMate data folder. Review backups and exported copies separately.

For Linux/macOS reminders, close the app and remove its `ReminderPreview` folder
to discard the saved rhythm and total. Removing the AppImage or Mac app alone
does not remove those files. See [removal guidance](store/SUPPORT.md).
MonitorMate has no server copy to delete.

## External links and future Store distribution

The About link opens GitHub in your browser. GitHub, the operating system,
download security checks and backup providers apply their own data policies.
These services are separate from MonitorMate's local processing.

The GitHub preview has no timed trial, subscription or purchase checks. A future
paid Microsoft Store edition may involve Microsoft-managed transaction data.
That integration must be reviewed and disclosed before release. No Store service
is described here as already available.

Use [Support](SUPPORT.md) for ordinary issues and [private security reporting](SECURITY.md)
for vulnerabilities. Review diagnostics before sharing them, even privately.

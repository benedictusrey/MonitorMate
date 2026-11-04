# Architecture and data flow

Current preview scope and audit: [portable preview](PORTABLE_PREVIEW.md) and [UI audit](UI_AUDIT.md).
Earlier implementation/planning notes below are retained as history.

Reviewed against the local source on 4 November 2026. MonitorMate targets
`net10.0-windows`, x64, using WPF plus Windows Forms for the tray icon.
The project declares no external NuGet package references.

## Components

| Layer | Responsibility |
|---|---|
| `App.xaml.cs` | Composition, local-session mutex, hotkeys/tray, launch and shutdown. |
| `MainWindow.xaml(.cs)` | Custom chrome, page host, close-to-tray and navigation animation. |
| `Core/Models` | Settings, display state, presets, gamma records and usage totals. |
| `Core/ViewModels` | Seven page models, commands, formatting and overlay models. |
| `Core/Services/AppServices.cs` | Service composition and disposal order. |
| `DisplayService` | Display enumeration, DDC probe/write/readback and physical-handle leases. |
| `GammaService` | Read/compose/write/restore gamma and baseline persistence. |
| `ComfortService` | Presets, per-display applies, pause, refresh and manual holds. |
| `ScheduleService`, `SolarCalculator` | Fixed/solar period resolution and 30-second schedule ticks. |
| `ReminderService`, `OverlayService` | Work/break state and per-display overlay windows. |
| `FocusSessionService`, `HotkeyService` | Focus lifecycle, countdown and Escape/global keys. |
| `UsageTracker`, `UsageStore` | Minute sampling, counters and JSON history. |
| `SettingsService`, `LogService` | Debounced JSON settings and rotating diagnostics. |
| `Themes`, `Controls`, `Views` | XAML resources, custom drawing, pages and overlays. |

## Startup and writes

App creates services and view models, scans displays, then initializes reminders
and usage tracking. Completed setup allows automatic schedule/restore behavior.
Manual apply paths do not all enforce setup, which is an open defect.

Comfort serializes display applies with a semaphore. Gamma work runs on a worker
and uses a service lock. DDC work uses a separate display-service lock and worker.
An apply composes gamma, then sends changed hardware values. Failure/readback
semantics are imperfect; see [audit](AUDIT.md). There is no hardware simulator or
dedicated automated test project in the authored tree.

## Persistence

`AppPaths` resolves `MONITOR_MATE_DATA_DIR`, otherwise Roaming ApplicationData
plus `MonitorMate`. Files include `settings.json`, `usage.json`,
`gamma-baselines.json`, `diagnostics.txt`, and `logs/monitormate.log`.
Settings use a 900 ms debounce; usage flushes every 45 seconds. Gamma records
are saved with signatures. JSON writes use a temporary file and overwrite move.

Settings and gamma attempt corrupt-file backups. Usage load failure starts empty
without the same backup path. Usage filters records older than 120 days on load;
logs retain a rotated `.1` file at an approximately 1 MB threshold. Backups and
exports have no automatic expiry. There is no encryption-at-rest layer.

## Shutdown and native limits

Normal shutdown disposes focus/reminders/schedule/overlays, then comfort and gamma
attempt restoration. Forced termination cannot execute this cleanup. Restart
currently starts another instance before service disposal completes, creating a
restoration/persistence race. The installer also uses a forced stop for matching
installed `dotnet` processes.

Display IDs include device information that can change with ports/topology.
Gamma is a legacy driver interface, not HDR/ICC calibration. Multi-monitor hot
plug, DPI boundaries and concurrent colour tools require hardware testing.


## Preview distribution boundary

The v1.0.0 GitHub preview adds no expiry, StoreContext, purchase flow or account
service to this architecture. Paid Store distribution is only a
[proposal](RELEASE_MODEL.md). Source-license adoption is still pending.
The new documentation checker and CI compile/check files without launching the app.


## Proposed portable host

A separate Avalonia host and OS-specific adapters are proposed in
[CROSS_PLATFORM.md](CROSS_PLATFORM.md). The current WPF/Win32 application remains
unchanged. Shared models alone do not port display control, overlays or session APIs.

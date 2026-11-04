# Architecture and data flow

MonitorMate has a Windows WPF host, a portable Avalonia reminder host and a small
shared reminder engine. One interactive user runs each app. Windows serializes
display requests; this is not a multi-user service or remote display controller.

## Hosts and services

| Component | Responsibility |
|---|---|
| `src/MonitorMate` | Full Windows app, seven pages, native monitor controls, focus/break overlays, tray and shortcuts. |
| `ComfortService` | Per-display preferences, presets, apply serialization and explicit outcomes. |
| `GammaService` | Baseline composition, Windows gamma bounds, readback tolerance and restoration records. |
| `DisplayService` | Monitor enumeration, DDC/CI write/readback and physical-handle lifetimes. |
| `ScheduleService` and `SolarCalculator` | Fixed/solar periods, local dates and manual holds. |
| `ReminderService`, `FocusSessionService`, `OverlayService` | Windows timer and overlay lifecycles. |
| `SettingsService`, `UsageStore`, `LogService` | Local persistence, recovery and rotating logs. |
| `src/MonitorMate.Reminders` | Pure portable reminder state/timing, without display APIs. |
| `src/MonitorMate.Desktop` | Avalonia reminder UI and independent local settings. |

## Consent and display outcomes

Startup scans displays without applying settings. Completed manual setup permits
direct controls. Automatic scheduling and launch/resume restoration also require
explicit app consent. Disabled displays do not receive apply writes.

A semaphore serializes display applies. Gamma uses a locked worker path; DDC/CI
uses its own locked display service. Target gamma values come from the saved
baseline, then stay inside Windows' allowed deviation from identity. Readback
accepts one 8-bit step of quantization and retains the observed signature to avoid
recapturing MonitorMate's own tint. Limited targets are disclosed.

Display outcomes distinguish confirmed, limited, partial, unconfirmed and failed
requests. Read support is not write support. View models summarize these outcomes
instead of inferring success from a completed Task. Hardware support and HDR
remain external constraints; see [verification](VERIFICATION.md).

## Persistence and shutdown

Windows resolves `MONITOR_MATE_DATA_DIR`, otherwise Roaming ApplicationData plus
`MonitorMate`. Settings, usage and gamma records use temporary-file replacement.
Unreadable files have recovery/backup handling. History filters a 120-day window
on load; logs rotate around 1 MB. Backups and exports have no automatic expiry.
The [privacy policy](../PRIVACY.md) defines the stored data and removal behavior.

Normal restart disposes services before launching a replacement. Normal exit
attempts software gamma restoration; forced termination cannot do so. Installer
updates require normal exit and do not force-stop an active app. Hardware
brightness remains at its last setting unless explicitly reset.

Portable settings use the OS local application-data directory under
`MonitorMate/ReminderPreview`. Closing that host stops reminders. It has no
background service, screen-time tracker, display adapter or cross-host migration.

## Distribution

The source uses MIT. The v1.0.0 GitHub preview has a per-user Windows installer,
Linux AppImage and DMG with separate Intel/Apple Silicon apps. The native workflow
may reuse a successful ARM bundle only after matching every declared build/test
input and verifying the artifact digest. A paid Store edition remains a future
plan; no expiry, account, purchase service or automatic updater is present.

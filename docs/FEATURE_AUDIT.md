# Feature audit, 5 November 2026

What this repo does: MonitorMate provides local Windows display comfort, reminders,
focus and aggregate activity. Linux/macOS provide the approved smaller reminder
preview. Expected load is one interactive user, a few monitors and serialized
hardware requests; no remote users or server workloads are assumed.

## Must fix findings addressed

1. **Dark/warm presets exceed Windows gamma limits.** The old composition could
   put channels more than half-range below identity, including the reported Night
   and Cinema requests. The driver double now enforces this boundary and reproduced
   failure before the fix. Generated ramps are bounded and the UI explains reduced
   dimming/warmth. Without this repair, ordinary presets can fail in both modes.
   See [GammaService](../src/MonitorMate/Core/Services/GammaService.cs).
2. **Preset/match/reset messages claim success after rejection.** These view models
   previously treated a completed async call as success. They now use explicit
   per-display outcomes and check hardware restoration. Driver-rejection tests
   cover manual/automatic modes and reset. Without this repair, users cannot tell
   saved targets from applied changes. See [ComfortService](../src/MonitorMate/Core/Services/ComfortService.cs)
   and [DisplaysViewModel](../src/MonitorMate/Core/ViewModels/DisplaysViewModel.cs).
3. **Normal driver quantization looks like failure and can contaminate recapture.**
   Exact 16-bit equality rejected normal 8-bit readback. The comparison permits
   one 8-bit step and stores the observed owned signature. A regression checks
   both confirmation and baseline recapture. Without this repair, a tint can be
   mistaken for an external baseline. See the gamma service and domain regressions.
4. **Security/support text contradicts the repaired release.** Preparation-era
   documents called fixed defects open and omitted a working private report path.
   The policies now describe the shipped scope, actual permissions, local data,
   removal, signing and recovery. Private vulnerability reporting is enabled.

## Feature coverage and release boundary

| Area reviewed | Current implementation and checks | Remaining acceptance |
|---|---|---|
| First launch/automation | Manual and automatic consent, decline/retry, disabled-display guards; view-model/domain checks. | Visible native dialog and full keyboard pass. |
| Display controls | Baseline composition, gamma bounds, readback, DDC retry, preset/match/reset outcomes. | User's RTX 5070, physical panels, HDR/ICC and docks. |
| Scheduling | Fixed ordering, solar local dates, tomorrow's events and manual holds; regressions. | Real time-zone/resume transitions with hardware. |
| Breaks | Manual skip, strict scheduled behavior, elapsed-time accounting and stopped-timer restart. | Native overlays across desktops and sleep. |
| Focus | Reading band/spotlight, Escape preservation, page/tray exits. | Click-through, compositor stacking and accessibility on actual desktops. |
| Activity | Aggregate usage, malformed/corrupt history recovery and short-break accumulation. | Independent real-use sampling and retention behavior over time. |
| Settings/data | Local atomic replacement, backups, reset and separate portable schema. | Filesystem denial, external backup and clean-machine migration scenarios. |
| UI | Seven Windows page templates; portable tabs at three widths with pointer/Space events. | Narrator, native animation timing, mixed DPI and custom chrome gestures. |
| Packaging | Installer/runtime, AppImage, two Mac bundles, signing order and DMG checks. | Trusted signatures, notarization and clean-machine user upgrades. |
| Linux/macOS | Runnable reminder engine, start/pause/resume/stop, skippable breaks and persisted rhythm. | Native display controls, focus, tray, hotkeys and activity are explicitly unsupported. |

The existing supplied icon/artwork remains intact. No replacement image was needed.
The historical audit remains available for traceability; its original blocked
verdict is not the current release status.

Verdict: the reproduced gamma and misleading-status defects are repaired in
source with regression checks. Ship as the documented v1.0.0 feedback preview;
do not describe untested physical-driver behavior as flawless.

Not checked: exhaustive physical hardware, full native click-through, screen
readers and every OS/security product. No third-party penetration test was performed.

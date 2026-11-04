# ADR 0004: First-run consent and preset caps

## Status

Accepted.

## Context

Display tools can surprise users: a preset may raise brightness, change contrast
or leave a gamma tint after the app is closed. CareUEyes applies colour modes
immediately. MonitorMate is intended to be safer and more trustworthy.

## Decision

- `SetupCompleted` starts false. The first-run card offers **Use manually** and
  **Enable automatic**. Manual use completes setup without applying a preset.
- `AutomaticDisplayControlAllowed`, `ScheduleEnabled` and `RestoreOnLaunch`
  start false. Enabling automatic control requires confirmation. Declining the
  first prompt completes setup in manual mode; later attempts ask again.
- Turning off scheduling returns to manual mode and also turns off launch restore.
  Launch restore can be enabled separately in Settings after confirmation.
- Automatic startup, resume and display reconnect paths require the saved opt-in.
  Manual refresh scans displays without applying or restoring gamma. Explicit
  presets and sliders remain usable after manual setup.
- Older settings without the new opt-in load with scheduling and launch restore
  disabled. Users can enable either again; unrelated preferences are preserved.
- This is application consent, not a Windows security grant. The Windows manifest
  stays `asInvoker`. No administrator elevation or screen-capture permission is
  requested. Unsupported monitor controls still need a supported driver, DDC/CI
  configuration or the monitor's physical controls.
- On first control of a display, capture the original hardware brightness.
- A hardware-brightness preset is capped at that original value. It may lower
  brightness, never raise it above the pre-MonitorMate value.
- Do not send contrast until the user moves the contrast slider; track
  `ContrastTouched`.
- Gamma is composed from the baseline and restored on pause/exit.
- Hardware brightness is left where the user set it, with a reset action in
  Displays.

## Consequences

- First run is a consent step, not a setup wizard that changes the screen while
  the user reads.
- A user who wants a brighter display can still move the slider manually; the
  cap applies to presets, not to direct control.
- The app may look conservative on first launch. That is intentional.
- The baseline and original brightness are stored per display, so the rules
  survive restarts.

## Alternatives considered

- Apply a default preset immediately: matches CareUEyes, but breaks the safety
  promise.
- Cap all brightness changes: too restrictive; the user must be able to raise
  brightness deliberately.
- Apply contrast by default: can visibly wash out a monitor; opt-in is safer.


## Verification, 4 November 2026

Automated checks cover first-run choices, declined prompts and retries, accepted
consent persistence, legacy settings and manual display writes with a simulated
hardware boundary. The real view models use a supplied confirmation answer in
these checks; no native dialog or physical monitor is exercised.

The earlier Codex UI automation initialization failure came from its workspace
filesystem policy. This application choice does not repair that host failure.
Native UI, monitor/driver acceptance and non-Windows implementations remain
release gates in [release readiness](../RELEASE_READINESS.md).

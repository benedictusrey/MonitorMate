# MonitorMate v1.0.1 — GitHub preview

MonitorMate combines display comfort and break reminders. This preview provides the full Windows app and a smaller reminder app on Linux/macOS.
Created by **Benedictus RH (@benedictusrey)**. MIT licensed; no timed trial or expiry.

## Patch update

v1.0.1 fixes Windows button text contrast. A global text style had overridden
button foregrounds, leaving pale labels on coral actions. Labels now inherit their
control's intended color. New page-template checks cover the effective label color.
The original v1.0.0 release and tag remain intact.

## Windows

Per-display brightness and opt-in contrast where DDC/CI is supported, six comfort
presets, colour warmth, fixed/solar schedules, break reminders, reading-band and
spotlight focus, tray controls, shortcuts and local activity history.

First launch offers manual or automatic control. Automatic scheduling and launch
restore require approval; declining leaves manual controls available. Fixes include
consent guards, gamma recapture, restart cleanup, display reset/match, focus Escape,
manual-break timing, persistence recovery and schedule validation. Text readability,
Focus description wrapping, disabled-button feedback and zero-activity bars were
also corrected.

## Linux and macOS

A smaller Avalonia app supports timed reminders, start/pause/resume/stop, skippable
breaks, saved rhythm and a local completed-break total. Its Capabilities page lists
unsupported features. It sends no display commands and has no focus overlay,
global shortcut, tray mode, auto-start, screen-time or idle detection.
Closing the app stops reminders. Sleep or a delayed tick pauses them.

## Downloads and requirements

- Windows x64: per-user `.exe` installer, runtime included.
- Linux x64: AppImage, initial Ubuntu 22.04+ X11/XWayland target.
- macOS 14+: one DMG with separate Apple Silicon and Intel apps.
- `SHA256SUMS.txt`: compare the downloaded file before running it.

These are platform packages, not universal binaries. Windows/Linux builds are
unsigned; Mac bundles are ad-hoc signed, not Apple notarized. Operating-system
reputation or Gatekeeper checks may block installation or launch. This is not a
Microsoft Store release.

## Verification limits

Source/build checks, deterministic regressions, framework page rendering, and
headless pointer/keyboard tests are automated. Native-runner results are visible in
GitHub Actions. Physical monitor/driver behavior, real sleep/hot-plug, compositor
visibility, screen readers, clean-machine updates and notarization remain acceptance
work. The preview is for feedback; it does not claim flawless behavior or feature
parity across platforms. Generated promotional illustrations are separate from
interface captures. See [verification](docs/UI_AUDIT.md) and [installation](docs/INSTALLATION.md).

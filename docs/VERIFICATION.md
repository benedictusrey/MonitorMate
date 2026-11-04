# Verification and evidence boundaries

The current [v1.0.0 preview](https://github.com/benedictusrey/MonitorMate/releases/tag/v1.0.0)
includes the full Windows app and smaller Linux/macOS reminders. This page
separates automated checks, native build evidence and remaining acceptance work.

## Automated behavior and UI

| Check | What it exercises | What it cannot establish |
|---|---|---|
| Windows domain regression | Real display policy, gamma math/limits, simulated driver rejection and quantization, presets, reset, consent, schedule, usage recovery and break timing. | A physical monitor accepts the generated request. |
| Consent and schedule view models | First-run manual/automatic choices, declined approval and schedule validation. | OS permission or visible native-dialog usability. |
| Restart integration | Real app/service shutdown before replacement launch, using an inert launcher. | Every external process or driver state. |
| Windows page rendering | Seven real page templates, bindings, font floor, contrast, disabled states and empty charts. | Complete keyboard traversal, Narrator or mixed-DPI behavior. |
| Portable reminder engine | Start/pause/resume/stop, immediate breaks, delayed ticks and completion counts. | Every OS sleep/compositor behavior. |
| Portable headless UI | Real framework pointer/Space events, settings recovery and three tabs at three widths. | Native desktop accessibility or window stacking. |
| Packaging tools | Failure handling, signing order, version/platform contract and verified ARM artifact reuse. | Publisher identity, notarization or clean-machine user acceptance. |
| Documentation | Local links, fence integrity and exactly four README product images. | Truth of untested hardware claims. |

The driver double now rejects ramps beyond the documented Windows gamma range.
This reproduced the reported Night/Cinema failure before the fix. Separate cases
reject writes, silently ignore them and quantize readback to 8-bit values. These
are controlled reproductions, not tests on the user's RTX 5070.

## Native builds and artifact provenance

[Three-platform preview](../.github/workflows/preview.yml) runs Windows installer
checks, Linux executable/window smoke tests, native Mac tests and DMG verification.
The Windows installer is silently installed/uninstalled only on disposable CI.

The ARM job can reuse one successful native bundle only when all tracked app,
dependency-notice, packaging and test inputs match that build exactly. It checks
successful native execution, artifact availability and the downloaded archive's
SHA-256. Changed inputs or missing evidence require a fresh ARM runner. Reuse is
labelled in CI and release provenance; it is not a new native ARM test run.
DMG assembly runs on an Intel Mac and verifies the resulting image.

Build/documentation checks and native package checks are separate workflows.
Inspect each conclusion rather than treating one green badge as all checks passing.
Historical failed/cancelled attempts remain in Actions history. Public release
checksums identify package bytes even when v1.0.0 filenames stay unchanged.

## Current acceptance limits

Codex's native computer-use helper fails before initialization under this
workspace policy. This host failure does not establish an app permission defect.
No full native click-through or animation-timing pass is claimed.

Physical DDC/CI/gamma, HDR/calibration interactions, mixed DPI, hot-plug, sleep,
screen readers and clean-machine upgrades still need acceptance testing. The app
cannot grant driver support through a consent dialog. Windows is unsigned and
Mac apps are ad-hoc signed without notarization. Compilation, screenshots and
checksums do not certify security, accessibility, medical efficacy or display safety.

## Reproduce checks

Use the commands in [release readiness](RELEASE_READINESS.md). Native workflow
logs record the exact source, OS and package steps. [UI audit](UI_AUDIT.md) records
prior rendering work; [feature audit](FEATURE_AUDIT.md) records the compatibility
repair. [The original audit](AUDIT.md) is historical, not today's defect status.

## Saved interface evidence

The README retains three generated promotional images and the authentic Today
capture from 29 October 2026. [Image provenance](IMAGE_ASSETS.md) distinguishes
artwork from runtime evidence. The [portable capture](screenshots/reminder-preview.png)
comes from the framework-rendered test UI, not the native OS window manager.

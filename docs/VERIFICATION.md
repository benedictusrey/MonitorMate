# Verification and evidence boundaries

Current preview scope and audit: [portable preview](PORTABLE_PREVIEW.md) and [UI audit](UI_AUDIT.md).
Earlier implementation/planning notes below are retained as history.

Current candidate status: [RELEASE_READINESS.md](RELEASE_READINESS.md).
Earlier failure/open-status statements below describe the original audit snapshot.

Audit date: 4 November 2026. Source review, build results, old screenshots and
generated promotional imagery are distinct evidence classes.

## Manual and automatic display control

First-run choices and repeatable confirmation are tested through the real view
models with supplied prompt answers. The domain regression also checks explicit
manual preset writes and read-only refresh while automatic control is disabled.
See [the consent decision](adr/0004-first-run-consent.md) for migration behavior.
This does not test a visible dialog or grant operating-system permissions.

## Current audit

- All 22 existing authored Markdown files inventoried and reviewed, excluding
  SDK, package-cache and build-generated documentation.
- Source reviewed for display control, consent, gamma, scheduling, reminders,
  focus, persistence, hotkeys, launch, packaging and UI claims.
- MSIX/portable/desktop files and manifest inspected locally; no installation or
  public submission performed.
- Link, image and document inventory checks are recorded in
  [audit checks](audit-checks.json).
- Build outcome is recorded in [AUDIT.md](AUDIT.md). A successful build does not
  validate DDC/CI behavior, display safety or accessibility.

Current Debug and Release rebuilds passed with zero warnings and zero errors.
Isolated model/math probes reproduced stale reset/match preferences, ignored
preset holds and solar-date mismatches without initializing display services.
Recorded commands and results: [audit-build-results.json](audit-build-results.json).

## Historical runtime evidence

The retained captures in `research/evidence/runtime/final` show the earlier
1040-by-650 layout. Today visibly displays Thursday, 29 October. Prior notes
reported successful navigation, About hyperlink discovery, break opening/skip,
tray hide/show, DLL launch and a readable focus-overlay surface.

Those reports were not rerun during this documentation pass. The prior hardware
label (1920 by 1080, RTX 3070) is historical and is not a current hardware inventory.
A `PrintWindow` focus capture proves rendering of a surface, not correct desktop
z-order, click-through behavior or usability in every context.

## Authentic captures

- [Today](screenshots/today.png)
- [Displays](screenshots/displays.png)
- [Comfort](screenshots/comfort.png)
- [Breaks](screenshots/breaks.png)
- [Focus](screenshots/focus.png)
- [Activity](screenshots/activity.png)
- [Settings](screenshots/settings.png)
- [Break overlay](screenshots/break-overlay.png)
- [Focus surface](screenshots/focus-print.png)

Existing scripts under `research/tools` can apply settings, finish setup or open
overlays. Review them before use. This audit used saved captures as image-generation
inputs and did not finish setup or change monitor settings to collect new evidence.

## Outstanding checks

| Area | Required check |
|---|---|
| First run | Every path, including direct controls, refresh, schedule and hotkeys. |
| Disabled display | No gamma/DDC write from any apply path. |
| Gamma | Active-tint recapture, restart ordering, driver rejection and other colour tools. |
| Hardware | Two panels, docks, hot plug, failed reads/writes, retry and mixed DPI. |
| Focus | Escape after hotkey rebuild/disable, overlay failure and sleep/resume. |
| Breaks | Immediate skip of a long break, strict scheduled versus manual behavior. |
| Activity | Idle, sleep, date rollover, corrupt data and retention rewrite. |
| Schedule | Manual preset hold, invalid times, time-zone changes and polar conditions. |
| Accessibility | Keyboard-only completion, Narrator, high contrast and Windows text scale. |
| Deployment | Clean target, downloaded artifact, normal upgrade, startup and uninstall. |
| Store | Actual identity, signature, package launch/data path and certification workflow. |

No claim of independent security certification, accessibility conformance,
SmartScreen exemption, medical effectiveness or multi-monitor correctness is made.


## Repository-documentation checks

The GitHub preparation adds `tools/check_docs.py` with a parser self-check and a
read-only Windows build/documentation workflow. Hosted CI has not run. Four README
image paths are checked; only three are AI-generated. Existing compile/probe
results above are historical to this source revision, not proof that a trial,
purchase flow, Store install or public release was tested. No expiry was added.
See [GitHub preparation](GITHUB_PREPARATION.md).


## README and portability-method checks

The revised README retains four product visuals and adds five static badges.
The documentation checker distinguishes badges from product images and validates
linked badge destinations. A read-only release planner rejects the current WPF
project and the absent portable host. Planner tests do not establish Linux/macOS
runtime support. No app build, native installer or cross-platform hardware test
was performed for this documentation revision.

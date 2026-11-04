# Changelog

## v1.0.0 compatibility and documentation refresh

- Bound software gamma ramps to Windows safety limits and report reduced adjustments.
- Accept normal 8-bit readback without recapturing the app's tint as a baseline.
- Report actual preset, match and reset outcomes instead of unconditional success.
- Enable private vulnerability reporting and clarify security, privacy and removal.
- Verify unchanged native ARM build inputs before reusing a successful artifact.


## v1.0.0 preview refresh — 5 November 2026

- Fix Windows button labels overriding their intended text colors.
- Leave untouched saved gamma baselines alone during display cleanup.
- Add an effective label-color regression to the page-template harness.
- Keep all app and package versions at v1.0.0; record refreshed build provenance and checksums.

## 1.0.0 preview — 4 November 2026

- Add a Linux/macOS reminder preview with explicit capability limits.
- Add native-runner builds and three platform package definitions.
- Improve Windows text readability, Focus wrapping, disabled/press feedback and reduced motion.
- Reject contradictory schedule times and render empty activity charts accurately.
- Cover reminder state, persistence, pointer/keyboard input and page rendering.


The public release remains v1.0.0. Earlier preparation entries below are historical;
current package provenance appears on the release page.

## Unreleased

- Reworked all existing Markdown for GitHub navigation and accurate preview status.
- Added a four-visual README: three generated illustrations and one authentic screenshot.
- Prepared contributor, support, security, authorship and third-party guidance.
- Added issue forms, a pull-request template, build/documentation CI and repository metadata proposals.
- Documented the future commercial Store plan without adding a timed trial.
- Prepared licensing guidance; MIT was subsequently adopted in the root LICENSE.

## 1.0.0 preview, prepared 4 November 2026

- Windows WPF display controls, comfort presets and schedules.
- Break reminders, reading-band/spotlight focus, tray and hotkeys.
- Local activity history and diagnostic tools.
- Initial source audit and model/math probes identified eight P1 findings, subsequently addressed in the repaired preview.

Read [release notes](RELEASE_NOTES.md) and [audit findings](docs/AUDIT.md).


## README and portability-method revision, 4 November 2026

- Refined the visitor README, preserving four visuals and adding five factual badges.
- Prepared About, website and topics locally following the owner's direction.
- Added an Avalonia/platform-backend migration method and three-package target contract.
- Added a read-only planner and regression checks; no native port or installers are claimed.

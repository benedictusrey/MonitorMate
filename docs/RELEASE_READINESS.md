# v1.0.0 preview readiness

The owner approved a smaller Linux/macOS reminder preview alongside the full
Windows app. Native display-feature parity is not a release claim. The source is
MIT licensed, credited to **Benedictus RH (@benedictusrey)**, with no timed trial.

The [published preview](https://github.com/benedictusrey/MonitorMate/releases/tag/v1.0.0)
records current package provenance and hashes. The [feature audit](FEATURE_AUDIT.md)
explains the gamma compatibility repair and remaining physical acceptance.

## Verification

Windows domain regressions, consent, restart handoff and the seven-page render
harness cover the repaired source. The portable host adds deterministic timer tests,
corrupt-settings recovery, real headless pointer/keyboard input and three-width
render checks. The current [UI audit](UI_AUDIT.md) records fixes and evidence limits.
The [historical audit](AUDIT.md) describes the original code, not current pass/fail status.

The [native preview workflow](../.github/workflows/preview.yml) builds Windows x64,
Linux x64, Mac Intel and Mac Apple Silicon. It constructs three primary downloads:
a per-user Windows installer, Linux AppImage and dual-app Mac DMG. Publication
requires successful native-runner validation and verified downloaded artifact hashes.
All versions remain v1.0.0. Reused platform packages must have unchanged verified
build inputs and explicit per-asset source provenance.

## Preview limitations

- Linux/macOS implement reminders only; see [portable scope](PORTABLE_PREVIEW.md).
- The local native computer-use runtime remains unavailable due to workspace policy.
- Physical monitor commands, multi-monitor/mixed DPI, sleep/hot-plug, native
  accessibility and exhaustive interaction/animation acceptance remain unverified.
- Windows installers are unsigned; Mac apps are ad-hoc signed, not notarized.
- The preview is not a Microsoft Store submission or a stable-quality guarantee.

## Reproduce local checks

```powershell
dotnet run --project tests/MonitorMate.Regression/MonitorMate.Regression.csproj
dotnet run --project tests/MonitorMate.AutomationConsent/MonitorMate.AutomationConsent.csproj -p:PublishSingleFile=false
dotnet run --project tests/MonitorMate.Restart/MonitorMate.Restart.csproj -p:PublishSingleFile=false
dotnet run --project tests/MonitorMate.UiAudit/MonitorMate.UiAudit.csproj -p:PublishSingleFile=false
dotnet run --project tests/MonitorMate.Reminders.Tests/MonitorMate.Reminders.Tests.csproj
dotnet run --project tests/MonitorMate.Desktop.Tests/MonitorMate.Desktop.Tests.csproj
python -m unittest discover -s tests -p "test_*.py"
python tools/check_docs.py
```

The four Windows projects require Windows. The portable engine and Avalonia
headless tests also run on Linux/macOS. See GitHub Actions for exact native-runner
results; package build success does not replace real user/driver acceptance.

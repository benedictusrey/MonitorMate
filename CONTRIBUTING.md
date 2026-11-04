# Contributing to MonitorMate

Start with [the audit](docs/AUDIT.md). A small repair with a clear regression check
is more useful than an unrelated refactor. The intended release is a GitHub preview;
no timed-trial or Store billing integration is part of the current source.

## License

MonitorMate uses the [MIT License](LICENSE), credited to Benedictus RH (@benedictusrey).
Preserve notices and submit only work you have the right to contribute. No
copyright assignment is requested by this guide.

## Build

Use Windows x64, the .NET 10 SDK, Git and Python 3 for the documentation checker.
The application declares no external NuGet package references.

```powershell
dotnet build src\MonitorMate\MonitorMate.csproj -c Debug
dotnet build src\MonitorMate\MonitorMate.csproj -c Release -p:PublishSingleFile=false
python tools\check_docs.py --self-test
python tools\check_docs.py
```

Compilation does not exercise physical monitors. If you run the app, review the
open display-state defects first and use a disposable data folder via
`MONITOR_MATE_DATA_DIR`. That isolates files, not hardware display changes.
Exit normally so restoration can run. Do not experiment on a calibrated workflow
without understanding gamma behavior.

## Changes and evidence

- Keep services responsible for native work and view models responsible for UI state.
- Preserve validation, error handling, author credit and supplied artwork.
- Add focused checks for new behavior; distinguish mocks, source reasoning and live hardware evidence.
- Record monitor/connection, Windows scale and driver for display/overlay changes.
- Test pause, normal exit, restart, failed calls and hotkey/overlay exits when affected.
- Keep docs and actual package behavior aligned. Never convert a build pass into a safety claim.

Use the [pull-request template](.github/PULL_REQUEST_TEMPLATE.md). Describe the
problem, resulting behavior, checks and known limits. Security reports follow
[SECURITY.md](SECURITY.md); do not post private logs or settings publicly.

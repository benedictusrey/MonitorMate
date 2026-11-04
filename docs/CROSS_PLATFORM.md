# Platform architecture and preview packages

MonitorMate now has two desktop hosts:

- [MonitorMate](../src/MonitorMate/MonitorMate.csproj): the full Windows WPF app.
- [MonitorMate.Desktop](../src/MonitorMate.Desktop/MonitorMate.Desktop.csproj): an
  Avalonia reminder preview for Linux/macOS, also buildable on Windows.
- [MonitorMate.Reminders](../src/MonitorMate.Reminders/ReminderEngine.cs): the
  platform-neutral reminder state machine used by the smaller host.

The owner approved the smaller Linux/macOS scope. Display hardware backends,
focus overlays, global shortcuts and tray behavior remain Windows-only. The
portable host reports these limits rather than simulating successful display writes.
[Full portable scope](PORTABLE_PREVIEW.md).

## Three primary packages

| Package | Scope | Architecture |
|---|---|---|
| `MonitorMate-1.0.0-windows-x64.exe` | Full WPF app, per-user NSIS installer. | Windows x64. |
| `MonitorMate-1.0.0-linux-x64.AppImage` | Reminder preview. | Linux x64, initial Ubuntu 22.04+ X11/XWayland target. |
| `MonitorMate-1.0.0-macos.dmg` | Two labelled reminder-preview app bundles. | Intel and Apple Silicon, macOS 14+. |

The DMG is one download with two native apps, not a Universal 2 executable. ARM
Windows/Linux are not included. Packaging does not establish hardware or UI parity.

The [preview workflow](../.github/workflows/preview.yml) builds on native runners,
checks deterministic and headless UI behavior, smoke-tests native reminder
executables, and verifies the DMG. Windows/macOS distribution identities and
notarization are not configured. The [planner](../tools/plan_desktop_release.py)
only reports source preconditions and commands; it still creates no installers.

## References

[Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms),
[Linux deployment](https://docs.avaloniaui.net/docs/deployment/linux),
[macOS deployment](https://docs.avaloniaui.net/docs/deployment/macos),
[headless testing](https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform).
These framework capabilities are not a claim that MonitorMate's omitted features
are implemented or that native hardware acceptance has been completed.

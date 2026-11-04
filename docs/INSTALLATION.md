# Install or build MonitorMate

Download packages only from [MonitorMate releases](https://github.com/benedictusrey/MonitorMate/releases).
Compare their SHA-256 values with the release's `SHA256SUMS.txt`. The preview does
not expire and has no payment mechanism. Refreshed preview files keep the same
v1.0.0 version; compare the current release checksums when replacing an older download.

## Windows x64

The `.exe` installer includes the runtime and installs for the current user in
`%LOCALAPPDATA%/Programs/MonitorMate`. No administrator elevation is requested.
Close MonitorMate normally before install, update or uninstall. A Start-menu
shortcut and Windows Apps uninstall entry are created. Uninstall preserves app
settings/history in `%APPDATA%/MonitorMate`.

The installer is unsigned; Windows may show a reputation warning. This preview
is not a certified Microsoft Store package. On first launch choose manual control
or approve automatic changes. Hardware support varies by monitor/driver.

## Linux x64

The initial target is Ubuntu 22.04+ with X11 or XWayland. Make the AppImage
executable and open it. A system without FUSE may use the AppImage runtime's
`--appimage-extract-and-run` option. The reminder preview has no display control,
focus overlay, tray/background mode or global shortcuts.

## macOS 14 and later

Open the DMG and copy the app matching your processor to Applications: Apple
Silicon or Intel. The two apps are native builds, not one Universal 2 binary.
Bundles are ad-hoc signed, not notarized; Gatekeeper may block this preview.
No screen-capture, accessibility or administrator permission is requested by the
reminder app. See [portable scope](PORTABLE_PREVIEW.md).

## Source builds

Install the .NET 10 SDK. For Windows use:

```powershell
dotnet run --project src/MonitorMate/MonitorMate.csproj -p:PublishSingleFile=false
```

For the smaller reminder preview on Windows/Linux/macOS:

```sh
dotnet run --project src/MonitorMate.Desktop/MonitorMate.Desktop.csproj
```

The [native build workflow](../.github/workflows/preview.yml) records package
construction. Current [verification limits](UI_AUDIT.md) remain relevant even
when a build passes. Existing Store scripts are separate from these GitHub packages.

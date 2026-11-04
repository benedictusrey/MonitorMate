# Third-party notices and provenance

Reviewed 4 November 2026. This is an inventory for release preparation, not a
complete legal clearance or a substitute for notices bundled with dependencies.

| Component or material | Treatment |
|---|---|
| .NET / WPF / Windows Forms | Microsoft runtime/framework. The Windows WPF host has no external UI packages; the portable host adds Avalonia. Preserve runtime notices when distributing a self-contained build. |
| Windows display APIs | Native operating-system interop, not bundled monitor-vendor code. |
| Bahnschrift / Segoe UI | System font references. These system fonts are referenced by the Windows host, not redistributed. |
| Supplied MonitorMate icons | Existing project artwork, preserved. Publisher must confirm distribution rights. |
| Three promotional illustrations | AI-generated from saved MonitorMate screenshots; full prompts and hashes retained. |
| Authentic app captures | Saved MonitorMate UI captures; review visible content before distribution. |
| Pillow | Optional development tool used by the existing icon script, not an app dependency. The script does not recreate the supplied current artwork. |
| Reference-product research | Extracted/carved third-party resources are excluded from the public source preparation. They are not MonitorMate assets. |

Do not ship local SDK caches, signing keys, app-data files, carved competitor
resources or diagnostic logs. A framework-dependent package does not bundle the
same runtime material as a self-contained Store package. Audit the actual release
payload before finalizing notices. See [release checklist](docs/GITHUB_PREPARATION.md)
and [image provenance](docs/IMAGE_ASSETS.md).

## Portable preview dependencies

Avalonia 12.1.4, its embedded Inter font, SkiaSharp, HarfBuzzSharp, MicroCom and
Tmds.DBus are used by the portable host. The exact restored inventory is retained
in [DEPENDENCIES.json](licenses/DEPENDENCIES.json). Bundled upstream notices and
license texts, including Inter's SIL Open Font License, are in [licenses](licenses).
The publish projects include these files. Preserve runtime and native-library
notices from the actual self-contained outputs as well. Source URLs for separately
retained upstream texts are recorded in [SOURCES.json](licenses/SOURCES.json).

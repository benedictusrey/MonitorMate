# MonitorMate support

Current candidate: [release readiness](../docs/RELEASE_READINESS.md).
MonitorMate now uses MIT. Older defect descriptions below refer to the audited
local build; the repaired candidate still requires native acceptance testing.

Reviewed 4 November 2026. Local build: 1.0.0. Proposed issue tracker:
[MonitorMate issues](https://github.com/benedictusrey/MonitorMate/issues).
Availability has not been verified. Use a confirmed contact before publishing a listing.

## Report a problem

Include version, Windows build, monitor model, cable/dock, driver, steps,
expected behavior and actual behavior. Review diagnostics before sharing: they
can include display identifiers, paths and exception details. Do not share private
screenshots, credentials or an entire app-data folder.

## Brightness, warmth and restoration

Enable DDC/CI in the monitor's on-screen menu if supported. Try a direct cable
when a dock or adapter interferes. Software gamma also depends on driver support.
A successful API return does not guarantee visible change.

Pause care attempts to restore stored gamma; it leaves hardware brightness at
its last setting. Exit normally before upgrades. Forced termination cannot run
restoration. Recapturing while a tint is active can save the tint as a baseline.
Do not use recapture as a general repair without first understanding that state.
HDR, ICC and concurrent colour tools are not validated.

Setup completion gates automatic launch behavior but does not block all direct
controls. Disabled display preferences are not enforced by every path. These
are open defects; see [the audit](../docs/AUDIT.md).

## Breaks and focus

Scheduled strict breaks hide skip; manually requested breaks remain skippable.
Focus uses click-through overlays. Esc normally ends focus if Windows registers
the key. Editing/disabling shortcuts during a session can remove that registration;
the Focus page or tray can end the session. Small captions and screen-reader
behavior need further testing. Screen-time is an estimate while the app runs,
and break minutes currently overcount skipped breaks of at least one minute.

## Startup and delivery

Desktop Start with Windows uses a per-user Run key. The packaged UI hides that
toggle; StartupTask integration is not implemented. Desktop and portable builds
need .NET 10 Desktop Runtime x64. Keep portable supporting files together.
Code signing and SmartScreen are separate; no warning-free guarantee applies.
See [signing](../docs/SIGNING.md).

## Remove a desktop or portable copy

1. Disable Start with Windows in MonitorMate if enabled.
2. Locate Settings > Open data folder and note its location if you want to retain or remove data.
3. Exit MonitorMate normally from its tray menu or Settings.
4. Remove `%LOCALAPPDATA%\Programs\MonitorMate` for the script-installed copy,
   or the portable folder you chose. Remove the MonitorMate Desktop and Start
   Menu shortcuts if present. The script installer does not register an Apps uninstaller.
5. Optionally remove the resolved data folder, normally `%APPDATA%\MonitorMate`,
   only if you intend to lose history, settings and gamma baselines. Review
   backup/export locations separately.

For an installed MSIX, use Windows Settings > Apps to uninstall the package.
Packaged data cleanup and any desktop-copy data must be checked separately.
The draft package has not been installed and tested by this audit.

MonitorMate is a comfort utility and does not diagnose or treat medical conditions.


## Current release-plan clarification

The GitHub preview is for evaluation without a configured expiry. Paid Store
distribution is a future proposal, so the current documentation makes no paid
support or subscription commitment. The public [SUPPORT.md](../SUPPORT.md) links
this guide and preserves the same recovery/removal limitations. See
[release model](../docs/RELEASE_MODEL.md).

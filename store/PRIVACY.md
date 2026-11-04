# MonitorMate privacy policy

Current candidate: [release readiness](../docs/RELEASE_READINESS.md).
MonitorMate now uses MIT. Older defect descriptions below refer to the audited
local build; the repaired candidate still requires native acceptance testing.

Documentation revision: 4 November 2026. Publisher: Benedictus RH.
Review this text against the final shipped package before publishing it.

## Data on your PC

MonitorMate works without an app account. Reviewed source contains no telemetry
SDK, upload client or MonitorMate server integration. Settings and activity are
stored on the PC rather than sent to a MonitorMate service.

Stored data includes display identifiers and preferences, typed solar coordinates,
gamma ramps, aggregate screen/break/focus counters, logs and optional diagnostics.
Logs may contain device names, file paths and exception details. Review them
before choosing to share them.

The app checks last-input time and Windows presentation/full-screen notification state for idle/full-screen
behavior and pointer position for focus overlays. Global shortcuts are registered
with Windows. It does not record keystroke content, screen images, clipboard
contents, window titles, browsing history or per-application usage in reviewed code.
Copy diagnostics writes text to the clipboard at your request; it does not read
clipboard contents. Solar coordinates are entered manually; no Windows location
permission is requested by the reviewed application.

## Location of files

Desktop data normally lives in `%APPDATA%\MonitorMate`. The environment variable
`MONITOR_MATE_DATA_DIR` overrides this location. Settings > Open data folder opens
the resolved directory. No app-level encryption-at-rest is implemented. Windows
permissions, backup, roaming profiles and sync software can affect these files.

Windows may redirect a packaged desktop app's AppData writes. The exact location
for the final MonitorMate package has not been verified. Do not assume the draft
package uses a separately implemented LocalState migration.

## Retention and removal

Usage records older than the 120-day cutoff are filtered when history is loaded;
the filtered history is persisted on a later dirty flush. This is not guaranteed
immediate deletion from disk. Logs rotate around 1 MB, keeping the current file
and one rotated file. Settings, baselines, corruption backups and exported
diagnostics have no automatic expiry in reviewed code.

Reset history clears the app's usage store. It does not remove all settings,
baselines, logs, backups or previously exported/shared copies. To remove desktop
data, first exit normally so gamma restoration can run, then remove the resolved
data directory if you intend to discard it. See [support](SUPPORT.md) for distinct
desktop and packaged removal paths. No MonitorMate server copy exists.

## External services

Core features do not require a network connection. Clicking the About link opens
the developer's GitHub profile in your default browser. GitHub then handles that
request under its own policy. Windows, Microsoft Store and any backup/sync
services have their own data practices, separate from MonitorMate's source.

The app has no advertising SDK, purchases or account database in reviewed source.
It is a general-purpose utility, not a service directed at children.

## Contact and changes

Publisher profile: [Benedictus RH](https://github.com/benedictusrey).
Proposed issue tracker: [MonitorMate issues](https://github.com/benedictusrey/MonitorMate/issues).
Its availability was not verified in this audit. Supply a working public support
URL and a monitored private contact in the release listing before publication.
Do not post private information in public issues. Update this document when
the shipped app's data handling changes.


## Preview and future purchase clarification

The preceding description of no purchase flow concerns current source, not a
promise that a future Store package will be free. The immediate plan is a GitHub
preview with no timed trial. A future paid Store edition may involve Microsoft
purchase data and must be assessed before submission. No account, trial or
payment integration was added. See [release model](../docs/RELEASE_MODEL.md)
and the public [privacy entry point](../PRIVACY.md).

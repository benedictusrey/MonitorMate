# Microsoft Store preparation

Current candidate: [release readiness](../docs/RELEASE_READINESS.md).
MonitorMate now uses MIT. Older defect descriptions below refer to the audited
local build; the repaired candidate still requires native acceptance testing.

The immediate release plan is a **v1.0.0 GitHub preview**, with no timed trial.
Future paid Store distribution is planned; price, billing and support terms are
undecided. No Store licensing or entitlement logic was added.

This folder contains the manifest, build script, existing tile assets and draft
submission documents. The local MSIX may exist in the working folder, but compiled
packages are excluded from the clean public source bundle. Manifest identity is
still a placeholder. No submission or packaged install was verified.

| Document | Purpose |
|---|---|
| [STORE_LISTING.md](STORE_LISTING.md) | Proposed listing fields. |
| [PRIVACY.md](PRIVACY.md) | Current local-app data policy. |
| [SUPPORT.md](SUPPORT.md) | Troubleshooting and removal. |
| [RELEASE_NOTES.md](RELEASE_NOTES.md) | Future Store notes, not a live release. |
| [AGE_RATING.md](AGE_RATING.md) | Questionnaire worksheet. |
| [ACCESSIBILITY.md](ACCESSIBILITY.md) | Source evidence and test gaps. |
| [ASSETS.md](ASSETS.md) | Existing tiles, captures and promotional art. |
| [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) | Submission gates. |

The package script deletes prior output before publishing and does not immediately
check the publish exit code. Do not run it against the only good artifact; resolve
[the audit findings](../docs/AUDIT.md) first. Packaged startup is hidden; no
StartupTask integration is present.

See [release model](../docs/RELEASE_MODEL.md) and [licensing](../docs/LICENSING.md)
before making pricing, trial or open-source claims.

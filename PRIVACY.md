# MonitorMate privacy

Updated 4 November 2026 for GitHub preview preparation. The complete current
data-handling policy is [store/PRIVACY.md](store/PRIVACY.md), covering the same
local application source. It describes settings, typed coordinates, baselines,
usage sampling, logs, backups and retention.

Reviewed source contains no app telemetry client, upload service, purchase flow,
trial entitlement check or MonitorMate account. Core features run locally.
This documentation change does not add a timed trial or payment processing.

A future commercial Store edition may involve Microsoft-managed purchase data.
Its actual integration must be reviewed and disclosed before release. No planned
service should be described as already implemented. See [release model](docs/RELEASE_MODEL.md).

Do not send private settings, raw logs or personal screenshots to public issues.
Use the [support](SUPPORT.md) and [security](SECURITY.md) guidance.

The Linux/macOS reminder preview stores only its chosen rhythm and completed-break
total under the OS local application-data directory, in `MonitorMate/ReminderPreview`.
It does not sample screen time, capture screen content or send display commands.
See [portable scope](docs/PORTABLE_PREVIEW.md). Its settings schema is separate
from the Windows host's history and gamma baselines.

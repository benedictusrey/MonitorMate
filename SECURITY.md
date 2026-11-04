# Security policy

MonitorMate v1.0.0 is a public feedback preview maintained by Benedictus RH
([@benedictusrey](https://github.com/benedictusrey)). This policy covers the full
Windows app and the smaller Linux/macOS reminder preview. It does not establish
a support deadline, independent certification or response guarantee.

## Report a vulnerability privately

Use [Report a vulnerability](https://github.com/benedictusrey/MonitorMate/security/advisories/new).
GitHub private vulnerability reporting is enabled. Include your package SHA-256
or source revision, operating system, sanitized steps and observed impact. For a
display issue, explain whether normal exit restores the screen.

Do not post exploit details, credentials, personal paths, coordinates or your
complete data folder in a public issue. If the private form is unavailable,
request a private channel through [the maintainer profile](https://github.com/benedictusrey)
without disclosing the vulnerability publicly.

## Permissions and automatic control

The Windows installer runs for the current user. MonitorMate does not require
administrator elevation, screen recording or accessibility permission. Manual and
automatic choices are application consent: approval allows scheduling and automatic
restoration, not additional Windows privileges. Declining keeps manual controls
and reminders available. Enabling automatic changes later asks again.
Start with Windows is a separate opt-in per-user startup entry.

Display commands depend on the monitor, connection and graphics driver. Reading
a gamma ramp does not prove write support. Administrator access cannot be promised
to fix rejection. Linux/macOS reminders send no display commands and request no
device access.

## Display changes and recovery

Windows hardware brightness uses DDC/CI where supported. Warmth and software
dimming use a stored gamma baseline. Generated ramps stay within Windows' gamma
range, and readback allows one 8-bit step of driver quantization. The app separates
confirmed, limited, partial, rejected and unconfirmed results. Slider percentages
and kelvin values are targets, not measured light output or calibration evidence.

Pause and normal exit attempt to restore software colour. Hardware brightness
stays at its last setting unless explicitly reset. Forced termination, driver
failure, HDR and other colour tools can prevent or overwrite changes. Keep the
monitor controls and Windows display settings available. Read [recovery](store/SUPPORT.md)
before deleting baseline files.

Microsoft documents [gamma-control limitations](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-setdevicegammaramp),
including undefined HDR and calibration interactions. Use Windows Night light or
monitor controls when the driver refuses colour control. MonitorMate does not
install a driver or disable system protection to bypass these restrictions.

## Local data and distribution trust

Settings, baselines, activity and logs use files without app-level encryption.
File permissions, device encryption and backup/sync services determine who can
access copies. Read [Privacy](PRIVACY.md) before sharing diagnostics.

Download from [the official repository release](https://github.com/benedictusrey/MonitorMate/releases/tag/v1.0.0)
and compare its current `SHA256SUMS.txt`. Refreshed previews keep the v1.0.0 version;
save the checksum and build revision with a bug report. Checksums detect different
bytes. They do not prove publisher identity, malware clearance or safety if both
the file and checksum source are compromised.

Windows installers are unsigned. Mac apps use ad-hoc signatures without Apple
Developer ID or notarization. Reputation and Gatekeeper warnings may appear.
Keep system protection enabled. Build from source if you cannot establish trust
in a downloaded package. See [signing](docs/SIGNING.md).

## Scope of assurance

Automated checks cover policy, persistence, reminder behavior and rendered UI.
Physical-monitor compatibility, complete native accessibility and independent
security certification are not established. See [verification](docs/VERIFICATION.md).
The preview has no account service, automatic updater, billing integration or
expiry enforcement. Any future Store service needs its own security/privacy review.

The 5 November 2026 NuGet advisory check reported no known vulnerable packages
in the portable app's direct/transitive dependency tree using the configured
sources. This is a dated advisory lookup, not a guarantee that dependencies
contain no undiscovered flaws.

# Security policy

MonitorMate changes monitor state through native Windows APIs. Treat display
restoration, overlay exits and persisted baselines as security-relevant reliability
boundaries. This document is not a security certification.

## Supported state

Version 1.0.0 is being prepared as a GitHub preview, with known P1 findings.
No stable support window, response SLA, independent audit or signed release is promised.
See [AUDIT.md](docs/AUDIT.md) and [verification](docs/VERIFICATION.md).

## Report a vulnerability

Do not put exploit details, full data folders, private paths or personal screenshots
in a public issue. Once the repository and private vulnerability reporting are
enabled, use GitHub's Security reporting flow. A private contact has not yet been
configured. If no private channel is available, contact the maintainer through
an existing verified contact or request a private channel without disclosing details.
Publisher profile: [@benedictusrey](https://github.com/benedictusrey).

Include affected source revision, Windows build, a minimal sanitized reproduction,
impact and whether the display can be restored. Do not send credentials or user data.

## Practical boundaries

- No app-operated telemetry/upload client was found in reviewed source.
- Settings, gamma records, logs and diagnostics are not encrypted by MonitorMate.
- A successful driver API call is not proof that a requested display state was applied.
- A process kill cannot perform normal cleanup. Baseline recapture and restart have known defects.
- Global Escape registration can be lost during hotkey rebuild; keep another focus exit available.
- Checksums establish byte integrity, not publisher identity or malware clearance.
- Keep Windows protection enabled. A signed host does not sign the app's DLL or scripts.

A future Store purchase/entitlement integration would require its own threat,
privacy and failure-mode review. No expiry or payment enforcement was added here.

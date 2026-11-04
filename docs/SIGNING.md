# Package trust and signing

The v1.0.0 preview uses three platform downloads. Signing state is separate from
build/test results and from the SHA-256 values on the release page.

| Package | Current signing state | Meaning |
|---|---|---|
| Windows per-user installer | Unsigned | Windows may warn about an unknown publisher or low reputation. |
| Linux AppImage | No publisher signature | Compare the release checksum and verify where you downloaded it. |
| macOS dual-app DMG | Apps ad-hoc signed, not Developer ID signed or notarized | Bundle integrity checks ran; Apple has not established publisher trust. Gatekeeper may block launch. |

Ad-hoc signing is not publisher authentication. A checksum proves that bytes
match the listed digest, not that the app is safe or independently reviewed.
An attacker controlling both download and checksum can replace both. Use the
[official release](https://github.com/benedictusrey/MonitorMate/releases/tag/v1.0.0),
inspect its build provenance and keep system protections enabled.

The Windows installer includes the runtime; a Microsoft-signed runtime does not
sign MonitorMate's app files. Neither a signature nor a successful CI build
promises warning-free installation. Microsoft describes reputation checks in
[SmartScreen guidance](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation).

## Future signed distribution

A Windows certificate/signing service and Apple Developer ID/notarization need
publisher-owned credentials and downloaded-package validation. Never commit keys,
install trust roots silently or disable security checks to make a release appear
ready. No signing identity or notarization credential was added here.

Store/MSIX files remain preparation material. Follow the [Store checklist](../store/RELEASE_CHECKLIST.md)
with the actual publisher identity and validate launch, data location, startup,
upgrade and uninstall before submission. No Store acceptance is claimed.

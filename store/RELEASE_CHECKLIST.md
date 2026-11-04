# Store release checklist

Reviewed 4 November 2026. All unchecked items remain gates, not completed work.

## Source and artifacts

- [ ] Resolve P1 findings in [AUDIT.md](../docs/AUDIT.md), with focused regression checks.
- [ ] Correct the package script's destructive pre-clean and unchecked publish failure.
- [ ] Build Debug and Release from the same recorded source snapshot.
- [ ] Rebuild portable and self-contained MSIX artifacts; retain hashes and build output.
- [ ] Confirm a failed build preserves the last known good package.

## Package identity and tests

- [ ] Reserve the product and replace manifest identity/publisher with Partner Center values.
- [ ] Set a version accepted for the next submission; do not guess from a draft listing.
- [ ] Validate manifest, assets, architecture, executable and full-trust capability.
- [ ] Sign only in the intended distribution/test workflow; never ship private keys or test roots.
- [ ] Test install, launch, update, normal exit and removal on a clean Windows VM.
- [ ] Locate packaged data and verify backup/removal behavior.
- [ ] Test monitor support on physical hardware, two panels and mixed DPI.
- [ ] Verify tray, hotkeys, focus exits, strict/manual breaks and sleep/resume.
- [ ] Verify keyboard, Narrator, text scaling and contrast before accessibility claims.

## Listing and public pages

- [ ] Publish reviewed privacy and support pages at working public HTTPS URLs.
- [ ] Supply a monitored publisher contact and verify the support destination.
- [ ] Confirm pricing and metadata in the actual account.
- [ ] Complete age-rating questions for the final package; do not assign 3+ manually.
- [ ] Capture the actual submitted package; never substitute generated UI art for screenshots.
- [ ] Review listing/release notes so they do not promise universal setup protection,
      permanent brightness caps, warning-free launch or untested accessibility.
- [ ] Confirm artwork and any shipped third-party material have distribution rights.
- [ ] Review applicable license, privacy and export questions; this draft is not legal approval.

## Submission

- [ ] Confirm current portal requirements and package acceptance in Partner Center.
- [ ] Upload, review certification feedback and retain the submission receipt.
- [ ] After acceptance, verify the live listing and a fresh Store installation.

No account action, signing, certificate installation, upload or publication was
performed by the documentation audit.


## Commercial-plan correction

Any earlier Free/no-trial listing text is superseded by the current
[release model](../docs/RELEASE_MODEL.md): GitHub preview first, future paid Store
distribution with price undecided, and no 15-day trial applied. Select the actual
source license and review commercial/runtime notices before submission.
Use the [root preview notes](../RELEASE_NOTES.md) for GitHub, not Store notes.

# GitHub repository and preview settings

Target repository: [benedictusrey/MonitorMate](https://github.com/benedictusrey/MonitorMate).
The owner authorized public source publication and a v1.0.1 GitHub pre-release.
The license is MIT and the author credit is **Benedictus RH (@benedictusrey)**.

Repository About description, homepage and topics are recorded in
[repository-metadata.json](../.github/repository-metadata.json). The homepage is
the repository README, not an invented Store listing or separately deployed site.
Issues are enabled; wiki and discussions remain disabled. Issue forms, support,
privacy and security documents are included.

The [preview workflow](../.github/workflows/preview.yml) builds all native payloads
and packages. Only successful artifacts from one reviewed commit may be attached
to the release. Three primary packages plus `SHA256SUMS.txt` are expected.
The release is marked pre-release and kept off Latest. No trial expiry or billing
is added. Check remote publication state rather than inferring it from this file.

The public [WhatsNow](https://github.com/benedictusrey/WhatsNow-for-WhatsApp) and
[IG-Now](https://github.com/benedictusrey/IG-Now) repositories informed document
organization; their capabilities or performance claims are not copied.
See [release readiness](RELEASE_READINESS.md) and [UI audit](UI_AUDIT.md).

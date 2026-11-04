# Reference-product research record

The historical local research described CareUEyes 2.7.7, a native x86 binary,
with recorded SHA-256 `d1fc4a54dd5c4d36658fa10a7d6e0b73ab2e639a60489c01706846acad55b664`.
The artifact identity was not revalidated during this documentation preparation.

Extracted strings, import reports and carved third-party assets stay local and
are excluded from the public source preparation. They are not MonitorMate artwork
and are not required to build the application. Public app captures are kept in
[docs/screenshots](screenshots).

The old report identified a visible SetDeviceGammaRamp import without visible
GetDeviceGammaRamp or dxva2 imports. Static import absence cannot establish that
dynamically loaded code never performs a function, that tint stacks or that
restoration is impossible. Resources also do not prove runtime behavior.

MonitorMate's own source explicitly contains DDC calls, gamma reads, baseline
composition, reminders, scheduling, focus and local activity. Review its own
[architecture](ARCHITECTURE.md), [audit](AUDIT.md) and [verification](VERIFICATION.md)
rather than treating reference-product comparisons as proof of superiority.

Historical inspiration links: [Twinkle Tray](https://github.com/xanderfrangos/twinkle-tray)
and [LightBulb](https://github.com/Tyrrrz/LightBulb). Current external feature parity
and licensing were not exhaustively tested. The GitHub preview adds no timed trial
or commercial service; see [release model](RELEASE_MODEL.md).

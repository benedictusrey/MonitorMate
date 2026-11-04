# Glossary

| Term | Meaning in MonitorMate |
|---|---|
| Baseline | Stored 768-entry gamma ramp, 256 entries per colour channel. Adjustments compose from it. |
| Baseline recapture | Replacement with the currently readable ramp. An active tint can be captured accidentally. |
| DDC/CI | Monitor command protocol used for hardware brightness and contrast via `dxva2.dll`. Support varies. |
| Gamma ramp | GPU/driver lookup table used for warmth and software dimming. Successful API return is not proof of visual output. |
| Extra dim | Gamma scaling in addition to supported hardware brightness. |
| Preset | Daylight, Evening, Night, Reading, Focus or Cinema values. |
| Preset cap | Brightness limited to the original value read in the current scan, not a persisted first-ever value. |
| Display identity | Hash of monitor/device information. Port or topology changes can change the key. |
| Enabled display | Saved preference used when selecting preset targets; currently not enforced by all apply paths. |
| ContrastTouched | Saved permission flag set when contrast is explicitly changed. |
| Day ribbon | Visual summary of the fixed or estimated solar schedule and current time. |
| Solar schedule | Offline sunrise/sunset estimate from user-entered coordinates and local time zone. |
| Gentle break | Small reminder card. |
| Full-screen break | Overlay covering enumerated displays, with a skip control. |
| Strict break | Scheduled break without skip. A manually started break is converted to a skippable style. |
| Focus band / spotlight | Click-through overlay with a clear horizontal band or pointer area. |
| Screen minutes | One-minute activity samples while the app runs and idle time is below two minutes. |
| Focus minutes | Rounded elapsed focus-session time recorded when the session ends. |
| Break minutes | Completed elapsed seconds, including carried sub-minute remainder; skipped breaks count only time actually taken. |
| MonitorMate face | Original vector CRT status character, not a network assistant. |
| Managed DLL delivery | Framework-dependent DLL started by the .NET host through a launcher. Not a code signature. |
| Apphost | Native executable that starts the managed app; optional desktop/portable and required by the Store build script. |
| MSIX | Windows application package; a locally built package is not evidence of Store acceptance. |
| Historical evidence | Prior captures and recorded results, not a fresh runtime test. |
| Promotional illustration | AI-generated composition based on a screenshot; not a runtime capture or QA artifact. |

See [audit](docs/AUDIT.md) for current implementation gaps.


## Preview and release terms

**GitHub preview** is the intended v1.0.0 evaluation release, without a configured
expiration. **Paid Store edition** is a future distribution proposal, not a current
billing service. **Source license** is MIT, selected by the owner.
See [release model](docs/RELEASE_MODEL.md) and [licensing](docs/LICENSING.md).
The [README](README.md) uses three generated illustrations and one authentic capture.

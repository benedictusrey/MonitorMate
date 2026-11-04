# Interface and behavior audit — 4–5 November 2026

This audit covers the seven Windows pages and the new three-tab reminder preview.
It separates framework rendering/input checks from native desktop and hardware
acceptance. No flawless-operation guarantee is made.

## Design direction

The requested frontend-design and design-taste-frontend skills were applied to
app screens. The dealt style was **True-black flat**, seed **346668927**, keeping
the existing palette and typography. No redraw was used. Existing olive surfaces
are retained as the brand exception; the design uses flat tone steps and the
light-of-day rail as MonitorMate's distinctive instrument. Icons remain the
existing consistent outline set and supplied app mark; no replacement artwork was
needed. The browser-only inspector does not support WPF/Avalonia and was not run.

Windows keeps Segoe UI text and Bahnschrift display roles. The portable app embeds
Inter so Linux/Mac do not depend on a Windows font. Body text is 14 DIPs in the
portable host; secondary labels are 12. Windows page text now has an 11-DIP floor,
with existing 12.5-DIP body and 24-DIP page headings. Font and wrapping checks do
not establish screen-reader or user text-scaling acceptance.

| Screen | First question | Primary action |
|---|---|---|
| Today | What is my current rhythm and display state? | Choose manual/automatic setup or take a break. |
| Displays | What controls does each display support? | Adjust a supported slider. |
| Comfort | Which preset or schedule is active? | Select a preset or save valid schedule times. |
| Breaks | When is the next break? | Start/take/pause a break. |
| Focus | Is focus active and how much time remains? | Start/end focus. |
| Activity | What totals have been recorded locally? | Refresh or explicitly reset history. |
| Settings | Which preferences and capabilities are available? | Change a labelled preference. |
| Portable Today | When is my next break? | Start/pause/resume/stop reminders. |
| Portable Rhythm | Which work/break interval should I use? | Save a valid rhythm. |
| Portable Capabilities | Which features are actually implemented? | Read the explicit limits; display control is disabled. |

## Reproduced findings and repairs

- A final effective-color check found the global text style overriding button
  foregrounds. v1.0.1 removes that override so filled actions use dark, readable
  labels; template checks cover the actual rendered text color. Derived windows
  explicitly apply the existing shared Window style so ordinary text also inherits
  the intended foreground and font. The harness now constructs the real main
  window and verifies its foreground.
- Read-only sessions could rewrite previously saved gamma baselines on exit.
  Cleanup now skips records without a pending applied adjustment; explicit reset/compare
  actions remain available. Tests check both untouched and actively changed ramps.
- Several Windows labels were smaller than 11 DIPs. The page/template checks now
  enforce a consistent floor; chart labels were brought into line.
- Focus-choice descriptions clipped at the default content width. They now wrap.
- Flat, primary, danger and icon button templates lacked disabled-state feedback.
  They now dim disabled chrome, keep keyboard focus visible, reserve outline space,
  and give flat/icon buttons press feedback.
- Navigation now respects both the app's reduced-motion preference and Windows'
  client-area animation setting. No decorative motion was added to the portable app.
- Contradictory fixed schedule times could be saved without a field error. They
  are now rejected before persistence, matching the schedule service's ordering rule.
- The empty Displays page told users to pick a nonexistent screen. It now names
  the empty state and offers a connection/Refresh recovery step.
- Zero activity drew a nonzero bar. The rendering test now checks an actual pixel
  above the baseline; zero values produce no bar.
- Portable progress used pending input values instead of the active interval.
  The engine now exposes its active duration.
- A manual break before Start returned to the default work interval. It now uses
  the chosen rhythm.
- Start confirmation was overwritten by a saved-settings message asking for another
  restart. Successful starts now confirm that reminders are running.
- Empty unsaved rhythm fields prevented completed totals from being saved.
  Completion persistence now uses the last saved rhythm independently of edits.

The relevant tests failed before these fixes and passed afterward. The new reminder
state-machine tests first ran against a no-op implementation, then against the
implemented timer. No synthetic screen-time totals or customer proof were added.

CI also exposed a test that assumed Tokyo's sunrise would be the next event in
all host time zones. It now compares the earliest actual next-day boundary.
Mac bundle construction now signs every payload file before the apphost and bundle,
including managed assemblies. This follows the framework's bundle-signing sequence;
it does not add Developer ID signing or notarization.

## Checks and evidence

- Windows domain/display-policy regressions use the real services with simulated
  native display/hotkey boundaries.
- Consent and schedule checks use real view models with supplied dialog answers.
- The Windows UI harness loads all seven actual page templates, measures/renders
  them at 800 DIPs, and treats binding errors as failures. It checks text sizes,
  disabled states and zero-bar rendering without enumerating physical displays.
- Portable tests exercise actual Avalonia pointer and Space-key input, persistence,
  timer behavior and all three tabs at 680, 820 and 1100 pixels. Captures are generated
  by Skia through the headless platform, not by the OS window manager.
- Native-runner builds, executable/window smoke tests and package construction are
  recorded in [Three-platform preview](../.github/workflows/preview.yml). A passing
  window initialization test is not a complete manual interaction pass.

[Portable reminder capture](screenshots/reminder-preview.png) is a framework-rendered
interface capture from the test app. Existing promotional illustrations and earlier
native captures remain clearly labelled in the README.

Mac 15 hosted jobs later failed to acquire runners after repeated attempts,
with GitHub reporting capacity constraints. The matrix uses GitHub's supported
Mac 26 Intel and Apple Silicon labels for the final build. The tests, native
window smoke checks, signing and package verification remain required.

## Remaining acceptance work

Codex's native computer-use runtime still rejects this workspace policy before it
can initialize. Native click-through, actual keyboard focus traversal, accessibility,
custom title-bar gestures and real animation timing could not be inspected here.
Physical monitor gamma/DDC, mixed DPI, sleep/hot-plug and concurrent colour tools
remain unverified. Linux/macOS native display features are intentionally absent.
Windows signing, Apple Developer ID/notarization, Gatekeeper acceptance and
clean-machine user upgrades are not claimed. Existing source builds and test results
must not be described as proof of these outcomes.

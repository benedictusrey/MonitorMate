# Accessibility evidence

Reviewed 4 November 2026. This is an implementation review, not a conformance certificate.

## Source evidence

- WPF navigation and standard controls provide keyboard entry points.
- Named controls and text status are present; meaning is not solely colour-based.
- Main window is resizable, default 1040 by 650 and minimum 920 by 610 WPF units.
- The app-level Reduce motion setting disables navigation fade/slide.
- Skip is available on non-strict breaks; focus registers an Escape exit.

These facts do not prove every control is reachable or correctly announced.
Changing hotkeys during focus can remove Escape registration. Strict breaks
deliberately remove skip. The native dark-only theme has no shipped light/high-contrast
variant, and 9.5 to 10.5 unit captions can be difficult to read.

## Required testing

Record keyboard-only completion of every page, visible focus, Narrator names and
values, slider changes, chart alternatives, 125/150/200% display scales, increased
Windows text size, multiple monitors, overlay escape behavior and high contrast.
Do not infer automatic Windows Reduce motion support from the app setting.

## Store declaration

Select only accessibility claims supported by the final package and actual
tests. Do not mark a general accessibility declaration as complete from this
source review. The exact submission options must be checked in Partner Center.

Use a confirmed public support contact to report barriers; proposed tracker:
[MonitorMate issues](https://github.com/benedictusrey/MonitorMate/issues).


## GitHub visual disclosure

The [four-visual README](../README.md) supplies meaningful alt text and labels
generated artwork. This does not establish keyboard or screen-reader compliance
of the app. The GitHub preview has no expiry, and commercial Store accessibility
declarations remain dependent on final package tests.

# Design

Current preview scope and audit: [portable preview](PORTABLE_PREVIEW.md) and [UI audit](UI_AUDIT.md).
Earlier implementation/planning notes below are retained as history.

## Brief

MonitorMate is a desk instrument, not a dashboard. It is used in short glances
between work. The interface should feel warm, compact and a little handmade:
olive-black metal, ivory type, a persimmon marker, and a small monitor character
who watches the light and the clock.

The 1.0 redesign moved the app from a generic dark-card layout to a denser
instrument panel. The window is 1040×650. The sidebar is 196 px. Most pages fit
without scrolling; Breaks and Settings scroll when their full option set is open.

## Direction

- **Night desk, not neon.** The base is a warm olive-black, not blue-black. Ivory
  text keeps long sessions readable. Persimmon marks active care; sand marks
  daylight; celadon marks calm; slate marks night.
- **One instrument per page.** Today has the 24-hour light rail. Displays has the
  control table. Breaks has the ring. Focus has the session ring. Activity has
  the week bars. Everything else supports that instrument.
- **A face, not a mascot takeover.** The MonitorMate face is a small CRT face in
  the sidebar. It speaks in one short line. It is not animated, does not pop up,
  and never blocks a control.
- **Compact, not cramped.** 12.5 px body, 9.5–10.5 px captions, 34 px nav rows,
  26 px sliders. The information density is closer to a hardware OSD than a
  marketing site.

## Tokens

| Token | Value | Use |
|---|---|---|
| `Ink0Brush` | `#10130F` | window base |
| `SidebarBrush` | `#141914` | titlebar, sidebar |
| `SurfaceBrush` | `#1A211A` | cards |
| `SurfaceRaisedBrush` | `#222B22` | hover, selected rows |
| `BorderBrush` | `#2F3A2F` | hairlines |
| `BorderStrongBrush` | `#465546` | strong borders |
| `TextPrimaryBrush` | `#F4EFE4` | headings, values |
| `TextSecondaryBrush` | `#B7BFB2` | body |
| `TextMutedBrush` | `#889384` | captions |
| `AmberBrush` | `#E07A5F` | active care, selection |
| `SandBrush` | `#D8B36A` | daylight segment |
| `SlateBrush` | `#91AFC4` | night segment |
| `MintBrush` | `#A9C6B2` | calm, available |
| `DangerBrush` | `#D96A5A` | reset, destructive |
| Display font | Bahnschrift | headings, readouts |
| Body font | Segoe UI Variable Text | body, labels |
| Window | 1040×650 | default |
| Sidebar | 196 px | persistent nav |
| Titlebar | 50 px | custom chrome |
| Page padding | 20/18 px | content frame |
| Card radius | 10 px | panels |
| Control radius | 6–8 px | buttons, inputs |

Spacing uses a 4 px scale. Cards use 14–16 px padding. The page uses a 1 px
hairline system instead of shadows. There is one accent per state; selected
things get a persimmon border, not a full fill.

## Layout

```
┌ titlebar 50: brand · status · pause · break · window ┐
│ sidebar 196 │ page content (scroll when needed)     │
│ Today       │ header + instrument + supporting rows  │
│ Displays    │                                       │
│ Comfort     │                                       │
│ Breaks      │                                       │
│ Focus       │                                       │
│ Activity    │                                       │
│ Settings    │                                       │
│ Today stats │                                       │
│ MonitorMate │                                       │
│ next break  │                                       │
└ privacy ─────────────────────────────────────────────┘
```

Today uses a full-width light rail with the current preset and next change, then
a rhythm card and a six-action quick panel. A compact focus summary and a
shortcut strip close the page. The first-run strip is one row, not a numbered
tour.

Displays uses a horizontal monitor tab row, a dense control table on the left,
and two compact reference panels on the right. Comfort uses a 2-column preset
list, a schedule panel, and a full-width day rail. Breaks uses a 118 px ring,
four rhythm chips, then timing and appearance panels. Focus uses a preview and
fine-tune panel beside a session ring, with three plain-language notes below.
Activity uses one readout strip, a 148 px week chart, and a two-column day read.
Settings uses General and About side by side, then shortcuts and diagnostics.

## Typography

- Bahnschrift Light for H1 (24 px), readouts (16–22 px), and numbers. It gives the
  interface a monitor/OSD character without a custom font download.
- Segoe UI Variable Text for body, labels, buttons and captions. Body is 12.5 px
  with 19 px line height; captions are 9.5–10.5 px.
- No all-caps labels, no numbered eyebrows, no italic accent word. The structure
  carries the hierarchy.

## MonitorMate face

The MonitorMate face is drawn as a vector CRT: rounded rectangle, antenna, two
eyes and a small smile. It appears in the sidebar and in the Activity empty
state. Its lines are short and useful:

- "I'm watching the light and the clock."
- "I'm resting. Your screen is yours."
- "One thing at a time. I'll keep the room quiet."
- "I'll fill this in while you work."

It never covers a control, never animates unprompted, and never gives medical
advice.

## App icon

The shipped icon is the blue monitor with the warm sun dial from
`icon/MonitorMate Icon.png`. It is cropped to its alpha bounds, padded on a
transparent square, and built into a 10-size ICO (16 to 256 px). The same file is
used in the titlebar, taskbar, tray and portable executable. Store tiles are
generated from the same source so the package and the app do not drift.

## Interaction

- **First run** — a single strip with Use manually and Enable automatic.
  Manual setup applies nothing; automatic scheduling requires confirmation.
- **Pause care** — restores gamma and stops the schedule; the status dot and copy
  change. Reversible.
- **Take a break** — starts immediately; skip is available unless strict.
- **Focus** — start/stop from the header or session panel; Esc ends a session.
- **Close** — hides to tray by default; Exit is explicit in the tray and Settings.
- **Identify** — numbers each display on its own monitor.
- **Keyboard** — visible persimmon focus ring; Tab reaches every control; nav
  supports arrows; Esc closes dialogs and focus sessions.

## Accessibility

- Text contrast targets 4.5:1 or better on the olive surfaces.
- Status is not colour-only: labels and values carry the meaning.
- Icon-only buttons have accessible names.
- Nav items expose titles to UI Automation.
- The About credit is a real `Hyperlink` with an accessible name and
  `InvokePattern`; it opens `https://github.com/benedictusrey`.
- The DayRibbon has an accessible name; it is a visual summary, not the only way
  to read the schedule.
- Reduced motion removes non-essential transitions.
- Focus overlays are click-through and do not trap the pointer.

## Motion

Motion is limited to state changes: page fade/slide, ring progress, toggle knob,
break overlay fade. 150–220 ms, ease-out. Reduced motion disables the page and
overlay transitions.

## Avoided defaults

- No warm-cream page, no terracotta-on-cream hero, no acid-green-on-black.
- No SaaS card kit with identical shadows and 18 px radii.
- No numbered 01/02/03 setup tour.
- No all-caps eyebrows, no middle-dot metadata strings, no monospace labels.
- No generic chart library; the light rail and week bars are custom.
- No fabricated stats; Activity says when it has no data.

## Screens

- [Today](screenshots/today.png)
- [Displays](screenshots/displays.png)
- [Comfort](screenshots/comfort.png)
- [Breaks](screenshots/breaks.png)
- [Focus](screenshots/focus.png)
- [Activity](screenshots/activity.png)
- [Settings](screenshots/settings.png)
- [Break overlay](screenshots/break-overlay.png)
- [Focus overlay surface](screenshots/focus-print.png)


## Audit clarification, 4 November 2026

The design direction above is preserved as rationale. Sizes describe WPF
device-independent units, not universal physical pixels. Accessibility statements
are design targets/source observations, not completed keyboard or screen-reader
certification. Reduce motion is an app setting; automatic Windows preference
integration is not established. Setup and restoration claims are qualified by
[AUDIT.md](AUDIT.md). The replaced [master file](../design-system/monitormate/MASTER.md)
now matches the actual WPF theme. No interface redesign was performed in this audit.


## GitHub README and image use

The [public README](../README.md) uses the overview as its hero, Comfort and Focus
as feature illustrations, and the authentic Today page as the fourth visual.
Small details in generated artwork can differ from the capture. Consult
[IMAGE_ASSETS.md](IMAGE_ASSETS.md). Preview wording must not imply trial enforcement,
certification or an already purchasable Store edition.

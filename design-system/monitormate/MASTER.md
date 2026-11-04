# MonitorMate design system

Reviewed 4 November 2026. This file replaces an unrelated generated plant-care
and web-marketing template. MonitorMate is a native WPF display utility.

The canonical implementation is `src/MonitorMate/Themes/Colors.xaml`,
`Typography.xaml`, `Controls.xaml` and `MainWindow.xaml`. The detailed rationale
is [DESIGN.md](../../docs/DESIGN.md). Source wins if documentation drifts.

## Identity and layout

Compact desktop instrument: olive-black surfaces, ivory text, persimmon selection
and the supplied monitor icon. The CRT face remains a small status detail.
Default size: 1040 by 650 WPF device-independent units; minimum: 920 by 610.
Sidebar: 196 units. Titlebar: 50 units. This is not a mobile webpage.

| Resource | Colour |
|---|---|
| Ink0 | `#10130F` |
| Sidebar | `#141914` |
| Surface | `#1A211A` |
| Raised surface | `#222B22` |
| Border | `#2F3A2F` |
| Primary text | `#F4EFE4` |
| Secondary text | `#B7BFB2` |
| Muted text | `#889384` |
| Persimmon / Amber | `#E07A5F` |
| Sand | `#D8B36A` |
| Celadon / Mint | `#A9C6B2` |
| Slate | `#91AFC4` |

Headings/readouts use Bahnschrift; body uses Segoe UI Variable Text. Do not add
Google Fonts, CSS tokens, testimonial sections or JavaScript animation libraries.

## Interaction and review

Keep seven navigation pages: Today, Displays, Comfort, Breaks, Focus, Activity,
Settings. Provide labels for states and accessible names for controls. Preserve
keyboard focus visibility. The app's Reduce motion setting controls navigation
animation; automatic use of the Windows animation preference is not established.

Before claiming accessibility, test all pages with keyboard and Narrator, Windows
text scaling, high contrast and multiple display scales. The current dark-only
implementation is not certified. Avoid using small screenshots to judge contrast.

Promotional image direction: low energy, varied compositions, static output.
Use one dominant app window and restrained accents. Generated imagery must be
labelled and kept separate from runtime screenshots.


## Public visual set

The [README](../../README.md) uses the overview, Comfort and Focus illustrations
plus the authentic Today capture. Keep their evidence labels and original palette.
See [image provenance](../../docs/IMAGE_ASSETS.md). No trial countdown, paid-tier
badge or Store certification mark is part of the GitHub preview.

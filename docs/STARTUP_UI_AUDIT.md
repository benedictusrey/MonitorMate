# Startup and text audit, 6 November 2026

## Diagnosis

The inspected Windows installation had a valid, enabled sign-in entry and was
running in the tray. Its settings disabled both automatic display control and
launch restore. That combination launches MonitorMate without changing the screen.
Starting the app and restoring a profile are separate choices.

The owner explicitly selected last-profile restoration. The local configuration
therefore enables launch restore and automatic-control consent, keeps the daily
schedule off and preserves the saved per-display values. New-user defaults remain
manual until the user approves automatic changes. Startup applies after Windows
sign-in, not before an interactive user session exists.

## Repairs

- Self-contained packages now start their native apphost directly. DLL builds
  retain their hidden launcher. The previous native path unnecessarily depended
  on PowerShell and a globally installed .NET runtime.
- Launch restore applies saved per-display preferences rather than replacing
  custom brightness/warmth with catalog preset defaults. It retains paused and
  disabled-display guards and logs the actual per-display outcome.
- Settings groups sign-in launch and profile restoration together, explaining
  manual mode, paused care and schedule precedence in a live summary.
- The display-selector item explicitly uses the theme foreground in selected
  and unselected states. It no longer inherits black text from Windows controls.
- Loading messages no longer contain corrupted ellipsis characters. Preset
  descriptions and display-control explanations wrap instead of being cut off.
- Nineteen sliders/text inputs have accessible names. Preset buttons expose their
  names through the shared style.

## Design direction

Keep the compact desktop instrument and its daily light rail. Use the existing
palette: ink `#10130F`, surface `#1A211A`, raised surface `#222B22`, text `#F4EFE4`,
muted text `#889384` and selection `#E07A5F`. Body text uses Segoe UI Variable Text;
headings/readouts use Bahnschrift. Existing type sizes and the 11-DIP label floor
remain. No new fonts, icons or promotional assets are required.

Content stays left aligned. Settings retains this hierarchy:

```text
[General: launch and restore] [About and local data]
[Global shortcuts                              ]
[Diagnostics                                   ]
```

This keeps startup behavior visible before optional sound/motion preferences.
The review retained MonitorMate's existing identity and avoided a cosmetic redesign.

## Verification and limits

Regression cases first reproduced native-host launch dependence, black selector
text and the missing exact-preference restore path. Checks now cover native/DLL
launch commands, consent, saved adjustments, paused/disabled guards, text colours,
input names and all seven page templates at 684, 800 and 1064 DIPs of content width.
The source contains no replacement bitmap icons.

These are real WPF framework renders and controlled behavior tests. The native
computer-use helper still cannot initialize under the current workspace policy,
so full live pointer/keyboard traversal and Narrator acceptance are not claimed.
The local sign-in path can be tested by launching the registered command after
normal exit; an actual Windows reboot still requires later user confirmation.

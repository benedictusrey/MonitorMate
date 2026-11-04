# Linux and macOS reminder preview

The owner approved a smaller preview on 4 November 2026. This is a runnable
Avalonia application alongside the full Windows WPF app, not feature parity.

## Available

- Start, pause/resume and stop a work/break timer.
- Take an immediate, skippable break; Esc or the break window's close button skips it.
- Choose 20-20-20, Pomodoro, Deep work or custom whole-number timing.
- Save the rhythm and a local completed-break total, with corrupt-settings backup.
- Use the interface with a pointer or keyboard. Text uses embedded Inter.

Nothing starts automatically. Closing the app stops reminders. A delayed tick or
sleep pauses the session instead of inventing attended work or repeated breaks.
No idle/activity detection is implemented; pause when stepping away.

## Explicit limits

Hardware brightness, contrast, gamma warmth, display scheduling, focus overlays,
global shortcuts, tray/background mode, screen-time history and auto-start are
not implemented. The Capabilities page explains this and disables display control.
No administrator, screen recording, device permission or root prompt is requested.
Use system or monitor controls to adjust the screen manually.

Settings use the OS local application-data directory under
`MonitorMate/ReminderPreview/settings.json`. This schema is separate from the
Windows app's settings and usage history. No network service or billing is added.

## Packages

The native-runner workflow builds Linux x64 and both Mac architectures. The Mac
DMG contains two labelled apps; choose the one matching the Mac's processor.
The first targets are Ubuntu 22.04+ with X11/XWayland and macOS 14+. Other Linux
compositors/distributions and ARM Linux are not claimed as validated.

These preview packages are unsigned (Linux/Windows) or ad-hoc signed (Mac).
They are not Microsoft Store certified or Apple notarized. Gatekeeper or Windows
reputation checks may block them. Source builds remain available.

## Evidence

The reminder engine has deterministic tests. Avalonia's headless platform tests
real pointer/keyboard events and renders all three tabs at 680, 820 and 1100 pixels.
These checks do not prove native compositor behavior, accessibility or notification
visibility on every desktop. See [UI audit](UI_AUDIT.md) and
[workflow](../.github/workflows/preview.yml) for the current evidence boundary.

# ADR 0001: WPF and native control

## Status

Accepted.

## Context

MonitorMate needs a desktop window with custom chrome, per-monitor overlays,
tray integration and direct Win32 calls for DDC/CI, gamma and display
enumeration. The alternatives were Electron/Tauri (web UI, extra runtime),
WinUI 3 (newer packaging/deployment constraints) and WPF (mature, in-box on
Windows, strong XAML styling).

## Decision

Use WPF on `net10.0-windows` x64 with C# and direct P/Invoke. Do not add a UI
framework dependency. Use WPF for the shell, pages and overlays; use Win32 for
the parts Windows exposes natively.

## Consequences

- No web runtime, no Node, no bundled browser.
- XAML gives precise control over the dark visual system and custom controls.
- Native interop is explicit and testable, but it must be kept behind services.
- WPF's `AllowsTransparency` overlays work but need care with topmost/z-order.
- The app is Windows-only, which is the requirement.

## Alternatives considered

- **WinUI 3**: good Windows 11 look, but packaging and per-monitor overlay
  behavior add risk for a local tool.
- **Electron/Tauri**: heavy for a tray utility and weaker native display access.
- **WinForms**: fast for utilities, but the custom visual system and charts would
  be harder to make consistent.


## Audit clarification, 4 November 2026

The original decision remains accepted. WPF here requires the .NET 10 Desktop
Runtime; it is not an inbox runtime guarantee on every supported PC. The review
of alternatives is historical judgment, not a current benchmark or platform comparison.
See [architecture](../ARCHITECTURE.md).


## GitHub preview scope

The repository presentation remains Windows/WPF only. Reference-app support for
other platforms does not apply to MonitorMate. The [preview model](../RELEASE_MODEL.md)
adds no trial service or new app framework.

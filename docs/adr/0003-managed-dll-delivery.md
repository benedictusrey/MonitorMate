# ADR 0003: Managed DLL delivery

## Status

Accepted for the local 1.0.0 delivery.

## Context

Windows SmartScreen blocks unsigned executables. A locally built
`MonitorMate.exe` shows "Windows protected your PC" with "Unknown publisher".
There is no publicly trusted code-signing certificate available for this build.
The user asked for a delivery that does not show the warning.

Options:

1. Sign with a self-signed certificate. Tested: the signature verifies locally
   only after installing a self-signed root, and SmartScreen still treats the
   publisher as unrecognized. It also changes the user's certificate trust store,
   which is a security decision we should not make silently.
2. Disable SmartScreen. Rejected: it weakens the machine and is not a product
   fix.
3. Ship an unsigned exe and tell the user to click "Run anyway". Rejected: the
   warning remains.
4. Ship the app as a managed DLL and launch it through the signed .NET host.

## Decision

Ship `MonitorMate.dll` plus dependencies, icon and a launcher script. The
installer creates a shortcut to the signed Windows PowerShell executable, which
runs the launcher hidden. The launcher starts `dotnet.exe` with the DLL using
`UseShellExecute=false`, `CreateNoWindow=true` and a normal window style. The
project sets `UseAppHost=false`, so builds and publishes do not create an
unsigned apphost executable at all.

The app detects the dotnet host (`AppLaunch.IsDotnetHost`) and uses the same
launcher for "Start with Windows" and Restart.

## Consequences

- The SmartScreen warning is gone on this PC because no unsigned executable is
  launched.
- The app requires the .NET 10 Desktop Runtime. The installer checks for it.
- The process is `dotnet.exe`, not `MonitorMate.exe`; the window, tray icon and
  taskbar icon still identify MonitorMate.
- This is not a substitute for code signing. For distribution to other PCs, sign
  a standalone build with a trusted certificate and publish that. The source and
  installer remain usable.
- A user who manually runs a standalone unsigned exe will still see SmartScreen.
  The package intentionally has no such exe.

## Alternatives considered

- Self-signed certificate + local root trust: rejected as a silent security
  change; also not sufficient for SmartScreen reputation.
- MSIX/Store signing: requires a trusted certificate and packaging work; not
  available here.


## Audit clarification, 4 November 2026

The local observation above is historical. Neither a Microsoft-signed host nor
an OV/EV certificate guarantees warning-free execution of downloaded MonitorMate
files. The DLL and launcher do not inherit the host's publisher signature.
The Store pack now exists as a draft, superseding the earlier absence of packaging.
See [current signing guidance](../SIGNING.md). No certificate trust changes were made.


## Preview delivery record

The current plan is a GitHub preview without expiry, followed only later by a
possible paid Store edition. That plan does not make the old no-warning claim
universal. Use [installation](../INSTALLATION.md), [signing](../SIGNING.md) and
[release model](../RELEASE_MODEL.md) as the current delivery guidance.

# Implementation and release plan

Current preview scope and audit: [portable preview](PORTABLE_PREVIEW.md) and [UI audit](UI_AUDIT.md).
Earlier implementation/planning notes below are retained as history.

Current candidate status: [RELEASE_READINESS.md](RELEASE_READINESS.md).
Earlier failure/open-status statements below describe the original audit snapshot.

Reviewed 4 November 2026. The 1.0.0 source and local delivery folders exist.
Historical implementation phases do not establish current release readiness.
See [audit](AUDIT.md) for evidence and [architecture](ARCHITECTURE.md) for source layout.

## Existing implementation

WPF shell, seven pages, DDC/CI controls, gamma baseline composition, fixed/solar
scheduling, break reminders, focus overlays, local activity, tray and hotkeys.
The default desktop payload is a framework-dependent DLL; optional portable and
Store builds use an executable. Store identity remains a placeholder.

## Required before release

| Priority | Work | Acceptance check |
|---|---|---|
| P1 | Enforce setup consent at shared apply boundaries | Fresh data: presets, sliders, refresh, resume, schedule and hotkeys produce no display writes before consent. |
| P1 | Enforce display exclusion | Disabled display receives no gamma or DDC writes from any apply path. |
| P1 | Safe baseline recapture | Apply tint, recapture, restore: original ramp remains recoverable. |
| P1 | Restore before restart handoff | Old instance cannot overwrite the new instance's ramp or baseline file. |
| P1 | Preserve active focus Escape registration | Changing/disabling hotkeys during focus keeps a reliable exit path. |
| P1 | Start timing when a manual break begins | Manual break after pause/stop counts down and closes normally. |
| P1 | Synchronize copied/reset values with apply preferences | Reset clears warmth/dim, and match-displays sends the selected values. |
| P1 | Stop on failed publish in Store script | A failed publish cannot be followed by packaging a partial payload; preserve last good package. |
| P2 | Correct DDC acknowledgement and retry state | Rejected write never updates applied cache as success; retry same value works. |
| P2 | Define and persist preset cap semantics | Refresh/relaunch does not silently redefine a promised permanent cap. |
| P2 | Correct skipped-break accounting | Immediate skip records elapsed time, not the configured full break. |
| P2 | Validate time ordering and manual holds | Reversed/equal fixed times rejected; preset hold matches slider hold. |
| P2 | Stabilize solar dates and next-change estimate | Time-zone boundary and high-latitude fixtures pass. |
| P2 | Harden loaded data | Null entries, invalid enums and malformed usage cannot crash initialization or silently erase recoverable history. |
| P2 | Complete usability and accessibility pass | Keyboard, Narrator, scaling, contrast and overlay exits are recorded. |

These are proposed repairs, not changes delivered by the documentation audit.
Use small focused regression checks at each responsible service boundary.

## Delivery gates

1. Build Debug and Release from the audited source; retain command output.
2. Validate all P1 paths with a controlled display adapter or native-call seam,
   then test supported hardware without assuming a successful API return is visual proof.
3. Exercise setup, normal exit, restart, sleep/resume, lock/unlock, tray and hotkeys.
4. Test two monitors, hot plug and mixed DPI; record hardware and driver versions.
5. Rebuild portable and MSIX artifacts from the same source; verify hashes and signatures.
6. Replace Partner Center identity and public URL placeholders. Test packaged
   launch/data/removal on a clean VM before submission.
7. Capture authentic screenshots from the packaged build. Use generated art only
   as labelled promotional material.

## Deferred scope

HDR/ICC calibration, cloud sync, accounts, auto-update, per-app rules and
localization remain outside this work. No new framework or runtime dependency is needed.


## GitHub preview preparation

Prepare v1.0.0 as a draft pre-release, with the [README visual set](../README.md),
support/community files, build CI and the [repository checklist](GITHUB_PREPARATION.md).
No trial/expiry is requested. Paid Store distribution remains a proposal.
Resolve the eight P1 findings before public runnable binaries, and adopt the source
license before an open-source claim. No repair phase above is completed by editing docs.


## Three-platform method

The [portability design](CROSS_PLATFORM.md) defines a shared C# core, an Avalonia
host and native capability adapters. Four runtime publishes feed three primary
package formats. This is a design/migration method, not three working installers.
Native feature parity and clean-machine acceptance remain prerequisites.

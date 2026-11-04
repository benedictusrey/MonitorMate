# MonitorMate audit, 4 November 2026

Current candidate status: [RELEASE_READINESS.md](RELEASE_READINESS.md).
Earlier failure/open-status statements below describe the original audit snapshot.

**Verdict: public release readiness is blocked.** Documentation is prepared;
application defects remain open. This was a source/documentation audit with
build verification and saved screenshot inspection, not a complete live hardware
or accessibility certification pass.

## Scope and evidence

Target: `C:\Users\Benedictus\Desktop\MonitorMate`. Version: 1.0.0 in the project;
1.0.0.0 in the Store manifest. All 22 authored Markdown files were inventoried.
SDK/cache/build-generated files were excluded. Source, XAML, launch/install/store
scripts, manifest and saved evidence were inspected. A source hash inventory is
retained in [source-inventory.json](source-inventory.json).

Debug build succeeded in an isolated workspace copy: zero warnings, zero errors,
using installed .NET SDK with `--no-restore -p:PublishSingleFile=false`.
Explicit Debug and Release rebuilds also succeeded with zero warnings and zero errors.
Native-call-free probes reproduced stale reset/match preferences, ignored preset
holds and solar date mismatches. These exercise actual model/math code from the
copied project, without starting the app or initializing display services.
No display writes, installation, settings reset, startup change, signing or Store
upload was performed.
No app code or existing executable/package was modified.

The findings below distinguish a reachable source defect from a timing/hardware
risk. Their regression checks are proposed; they have not all been executed.
The root folder has no Git metadata, so provenance uses hashes rather than a commit ID.

## Assessment

| Area | Assessment |
|---|---|
| Architecture | Small service/view-model structure; no extra runtime framework needed. |
| Compilation | Explicit Debug and Release rebuilds passed, zero warnings/errors. |
| Display safety | Open P1 defects; not release-ready. |
| Scheduling and counters | Several reproducible-by-code edge cases; runtime regressions required. |
| Recovery and deployment | Incomplete corruption handling and unsafe package/upgrade sequencing. |
| Accessibility | Source intent present, full keyboard/Narrator/scale tests outstanding. |
| Documentation | All authored files reviewed and corrected or given explicit historical clarifications. |
| Store | Draft artifacts only; identity, testing and public submission evidence absent. |

Priority: P1 should block release because display state, exits or release artifacts
can be affected. P2 affects correctness/recovery. P3 is documentation drift.
There are 8 P1, 8 P2 and 2 P3 findings. No numerical quality score is assigned
because a source audit does not measure hardware reliability or usability.

## Findings

### MM-01 [P1] Setup consent is not enforced by shared apply methods

Evidence: [src/MonitorMate/Core/Services/ComfortService.cs](../src/MonitorMate/Core/Services/ComfortService.cs), line 171.

`ApplyPresetAsync`, `ApplyAllAsync`, `ApplyStateAsync` and default `RefreshAsync` have no setup gate. Comfort schedule commands can invoke applies before completion too. Startup checks alone do not protect manual presets, sliders, resume or refresh. Put the consent policy at the shared write boundary and test every caller with fresh settings.

### MM-02 [P1] Disabled displays still receive changes

Evidence: [src/MonitorMate/Core/Services/ComfortService.cs](../src/MonitorMate/Core/Services/ComfortService.cs), line 252.

Preset selection filters Enabled, but ApplyAll iterates every state and ApplyStateCore never checks Enabled. Toggling Enabled itself triggers the card-change apply path. Enforce exclusion before gamma and hardware writes, and specify whether disabling should restore existing changes.

### MM-03 [P1] Baseline recapture can preserve the app's active tint

Evidence: [src/MonitorMate/Core/Services/GammaService.cs](../src/MonitorMate/Core/Services/GammaService.cs), line 206.

Recapture reads the current ramp and immediately replaces the stored baseline. Settings invokes this without restoring or rejecting the app's applied signature. A tinted current ramp can become the permanent restore target. Restore and verify the prior baseline or reject recapture while care is active; retain rollback data.

### MM-04 [P1] Restart releases ownership before gamma cleanup

Evidence: [src/MonitorMate/App.xaml.cs](../src/MonitorMate/App.xaml.cs), line 176.

Restart releases the mutex and starts a replacement before Shutdown triggers service disposal. Old and new instances can overlap gamma restoration, applies and writes to the same baseline JSON/temp file. This is an ordering risk established from source, not a reproduced timing failure. Complete cleanup before handing ownership to a new instance.

### MM-05 [P1] Hotkey rebuild removes focus Escape registration

Evidence: [src/MonitorMate/Core/Services/HotkeyService.cs](../src/MonitorMate/Core/Services/HotkeyService.cs), line 59.

Rebuild calls UnregisterAll, including the session Escape key, and only recreates the normal configured keys. Editing, resetting or disabling hotkeys during focus can leave the advertised Escape exit unavailable. Keep session registrations separate or re-register Escape while focus remains active.

### MM-06 [P1] Manual break can open with a stopped countdown timer

Evidence: [src/MonitorMate/Core/Services/ReminderService.cs](../src/MonitorMate/Core/Services/ReminderService.cs), line 191.

Pause and Stop stop the dispatcher timer. TakeBreakNow calls BeginBreak without restarting it, and BeginBreak does not start it. With reminders enabled but paused, stopped or not auto-started, a manual break can remain at its initial countdown until another path restarts timing. Start the timer and reset the tick reference when a break begins. Manual strict selection is converted to a skippable style, so this is not a strict-mode lockout claim.

### MM-07 [P1] Store builder can replace a good package after failed publish

Evidence: [store/Build-StorePackage.ps1](../store/Build-StorePackage.ps1), line 10.

The script deletes the previous payload and MSIX before dotnet publish, then does not check the publish exit code. PowerShell ErrorActionPreference does not by itself make every native nonzero exit terminating, especially in Windows PowerShell. Build into a fresh staging folder, check each native exit code, and replace the last good package only after successful validation.

### MM-08 [P1] Reset and match-displays commands do not update apply preferences

Evidence: [src/MonitorMate/Core/ViewModels/DisplayCardViewModel.cs](../src/MonitorMate/Core/ViewModels/DisplayCardViewModel.cs), line 212.

SetValues suppresses each setter's preference update and never copies the new values into State.Preference. Both ApplyToAllAsync and ResetToOriginal use it, then the service reapplies old preferences. Reset can report original colour restored while warmth/dim stay active; the separate hardware-brightness restore does not fix gamma. A native-call-free model probe reproduced stale TemperatureK and SoftwareDim. Copy values into preferences once, then invoke the intended apply path.

### MM-09 [P2] Rejected hardware write is cached as applied

Evidence: [src/MonitorMate/Core/Services/DisplayService.cs](../src/MonitorMate/Core/Services/DisplayService.cs), line 159.

A failed SetVCPFeature returns Supported=true, Confirmed=false. Comfort then records the requested value as AppliedBrightness/AppliedContrast, so repeating the same request can be skipped. Contrast failures with a supported-but-unconfirmed result are not surfaced consistently. Separate capability, write acceptance and readback, and update applied cache only on an accepted/verified policy.

### MM-10 [P2] Original brightness cap is redefined by each scan

Evidence: [src/MonitorMate/Core/Services/DisplayService.cs](../src/MonitorMate/Core/Services/DisplayService.cs), line 122.

OriginalBrightness is read into DisplayInfo on each scan. DisplayPreference has no persistent original-brightness field. Refresh or relaunch can redefine both the preset cap and reset target. This contradicts older documentation promising the first-ever value survives restarts. Define the intended lifetime and persist the value if that promise is retained.

### MM-11 [P2] Manual preset hold is ignored by the schedule

Evidence: [src/MonitorMate/Core/Services/ComfortService.cs](../src/MonitorMate/Core/Services/ComfortService.cs), line 415.

Manual presets set ManualHoldUntil but also set IsCustomMix=false. ShouldScheduleYield requires IsCustomMix, so only slider changes receive the hold. A forced schedule apply or boundary can immediately replace a selected preset. Evaluate hold independently from the custom/preset label and test both actions.

### MM-12 [P2] Fixed schedule accepts contradictory boundary order

Evidence: [src/MonitorMate/Core/ViewModels/ComfortViewModel.cs](../src/MonitorMate/Core/ViewModels/ComfortViewModel.cs), line 225.

The view model validates parseable times but not distinct or ordered day/evening/night boundaries. ResolvePeriod assumes an ordered day before night. Inputs such as day 20:00, evening 19:00, night 07:00 produce unintended periods. Reject invalid order or explicitly implement wraparound semantics.

### MM-13 [P2] Solar date correction can move an event further from the requested date

Evidence: [src/MonitorMate/Core/Services/SolarCalculator.cs](../src/MonitorMate/Core/Services/SolarCalculator.cs), line 26.

Any converted-date mismatch is corrected by adding one day, even if conversion is already on the following day. Resolve the astronomical event for the intended local date rather than always adding. Separately, ScheduleService.NextChange repeats today's solar times into future days instead of calculating each day's event, so its next-day prediction is approximate.

### MM-14 [P2] Skipped break counts its full configured duration

Evidence: [src/MonitorMate/Core/Services/ReminderService.cs](../src/MonitorMate/Core/Services/ReminderService.cs), line 478.

SkipBreak uses _totalSeconds and adds the rounded full duration for breaks of at least 60 seconds. Skipping a five-minute break immediately can add five minutes. Short 20-second breaks add no break minutes. Record actual elapsed seconds and aggregate consistently before displaying time totals.

### MM-15 [P2] Loaded data validation and recovery are incomplete

Evidence: [src/MonitorMate/Core/Services/UsageStore.cs](../src/MonitorMate/Core/Services/UsageStore.cs), line 157.

Usage load failure resets to empty without first preserving the corrupt file, and a later dirty flush can replace recoverable content. Null day entries or invalid dates are not validated before enumeration. Settings/gamma also normalize containers without fully validating null nested records and enum values. Validate trust-boundary data, preserve rejected input and report recovery accurately.

### MM-16 [P2] Installer force-stops the display controller

Evidence: [dist/Install-MonitorMate.ps1](../dist/Install-MonitorMate.ps1), line 17.

The upgrade path force-stops matching installed dotnet instances, bypassing normal gamma cleanup. Existing support copy also claimed an Apps uninstall entry that the script never registers. Documentation now tells users to exit normally and describes manual removal. A future installer should request graceful exit and verify cleanup before replacement.

### MM-17 [P3] Generated template conflicted with implemented design

Evidence: [design-system/monitormate/MASTER.md](../design-system/monitormate/MASTER.md), line 11.

The original master specified plant-care green, web fonts, CSS, testimonials and GSAP for a native WPF utility. Replaced with actual WPF tokens and links to source. No application UI was changed.

### MM-18 [P3] Release, safety and certification claims exceeded evidence

Evidence: [docs/SIGNING.md](../docs/SIGNING.md), line 37.

Original docs promised EV instant reputation and warning-free host launch, permanent brightness originals, universal first-run gating and ready Store declarations. Current docs distinguish historical evidence, current source and unperformed tests. Microsoft confirms EV no longer grants automatic SmartScreen bypass. Accessibility, public contacts, rating, submission and asset rights remain owner release checks.


## What changed in documentation

All existing Markdown files are listed in [the documentation index](DOCUMENTATION_INDEX.md).
The root guide, deployment guidance, glossary, Store copy, privacy, retention,
removal and accessibility statements were rewritten or corrected. Design rationale
and ADR history were preserved with prominent audit clarifications. The reference
comparison was narrowed to what saved imports/resources can establish.

New material: this audit, source architecture, a complete index and image provenance.
Exactly three promotional images use the saved Today, Comfort and Focus screenshots.
They are generated interpretations and are not new runtime captures.

## Required follow-up

Fix the eight P1 findings first, with focused checks at shared service boundaries.
Then address P2 accounting, scheduling and recovery. The
[implementation plan](PLAN.md) and [verification matrix](VERIFICATION.md) give
acceptance conditions. Retest actual monitor output, normal shutdown/restart,
hotkey edits, two monitors, mixed DPI, clean install and the final MSIX.

Current external guidance checked:
[Microsoft SmartScreen](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation),
[packaged desktop behavior](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-behind-the-scenes),
[age-rating workflow](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/pwa/age-ratings).
Local code observations are independently anchored above; these sources do not
certify MonitorMate. Live issue-tracker availability and all current Partner
Center form choices were not verified.

## Delivery checks

The machine-readable result in [audit-checks.json](audit-checks.json) covers
document inventory, local links, image count and source preservation. Build results
and full generation prompts are retained with the audit artifacts. A passing
document check does not close any code finding above.


## GitHub preparation update

The subsequent documentation pass adds repository support files and a four-visual
README. It preserves all findings above; no application defect was repaired.
The fourth visual is an authentic saved capture, not a fourth generated image.
Version 1.0.0 is a proposed GitHub preview with no timed trial. Store commercialization
and source licensing remain proposals; see [release model](RELEASE_MODEL.md),
[licensing](LICENSING.md) and [GitHub preparation](GITHUB_PREPARATION.md).
Original 22/26 document counts above describe the first audit revision, not the
current repository inventory in [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md).

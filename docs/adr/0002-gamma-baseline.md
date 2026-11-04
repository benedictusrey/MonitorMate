# ADR 0002: Gamma baseline composition

## Status

Accepted.

## Context

CareUEyes uses `SetDeviceGammaRamp` but does not read the existing ramp. That
makes it impossible to know whether a tint is composed from the display's
original calibration or from the previous tint. Repeated adjustments can stack,
and the original state is not restored.

## Decision

- Read and store a per-display baseline with `GetDeviceGammaRamp` before the
  first adjustment.
- Every colour temperature and extra-dim change is computed from that baseline,
  never from the current ramp.
- Store the baseline in `%APPDATA%\MonitorMate\gamma-baselines.json`.
- Restore the baseline on pause and normal exit.
- If the baseline cannot be read, do not apply a gamma tint; report gamma as
  unavailable and leave the display alone.

## Consequences

- Adjustments are deterministic and reversible.
- A forced kill cannot run the restore path; this is documented.
- A display whose gamma is changed by another app after the baseline is captured
  will not be reflected until the app recaptures. Recapture is an explicit reset,
  not an automatic read of the app's own output.
- The app does not claim to be an ICC calibrator.

## Alternatives considered

- Read the current ramp on every change: would compose on our own output and
  stack.
- Store no baseline: cheaper, but unsafe and not a real improvement over
  CareUEyes.


## Audit clarification, 4 November 2026

Absence of a reference binary's static import does not prove its entire runtime
behavior. The competitor conclusions above are unverified inferences, not facts
established by this audit. MonitorMate composes from stored ramps, but recapture
reads the current ramp and can save an active tint. Normal restore can be rejected
by a driver, and restart has an ordering race. See [AUDIT.md](../AUDIT.md).
The accepted baseline design does not establish these paths are correct.


## Public documentation scope

The [README](../../README.md) now labels promotional art separately from authentic
captures. Neither class proves gamma restoration. This baseline decision and its
open defects remain unchanged by the [GitHub preview preparation](../GITHUB_PREPARATION.md).

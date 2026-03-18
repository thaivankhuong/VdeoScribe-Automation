---
phase: 03-draw-progression-and-camera-state-resolution
plan: 01
subsystem: engine
tags: [dotnet, engine, timeline, draw-progression, deterministic-testing]
requires:
  - phase: 02-spec-schema-and-deterministic-timeline-core
    provides: deterministic frame indexing, ordered timeline events, lifecycle frame-state contracts
provides:
  - ordered draw progression fields on resolved object state for renderer handoff
  - deterministic multi-path draw cycle resolution with hide/reset handling
  - frame-state deterministic key coverage for draw progression payloads
affects: [renderer, frame-state, draw-resolution, deterministic-verification]
tech-stack:
  added: []
  patterns: [ordered draw-window resolution, hide-resets-draw-cycle policy, renderer-ready frame-state payloads]
key-files:
  created: [tests/Whiteboard.Engine.Tests/DrawProgressionResolutionTests.cs]
  modified: [src/Whiteboard.Engine/Models/ResolvedObjectState.cs, src/Whiteboard.Engine/Models/ResolvedTimelineEvent.cs, src/Whiteboard.Engine/Services/TimelineResolver.cs, src/Whiteboard.Engine/Services/ObjectStateResolver.cs, src/Whiteboard.Engine/Services/FrameStateResolver.cs, tests/Whiteboard.Engine.Tests/ObjectLifecycleResolutionTests.cs, tests/Whiteboard.Engine.Tests/FrameStateResolverContractTests.cs]
key-decisions:
  - "Resolved timeline events now preserve timeline parameter metadata so explicit path ordering survives into engine draw resolution."
  - "Object-level draw progress is the normalized average of ordered path progress, with hide events resetting prior cycles and active draw taking precedence over overlapping hide windows."
patterns-established:
  - "Renderer handoff pattern: Frame state carries normalized object draw progress plus per-path ordered progress so downstream renderers avoid timeline recomputation."
  - "Draw cycle pattern: A hide event resets prior reveal state and subsequent draw events begin a fresh ordered path cycle."
requirements-completed: [DRAW-01, DRAW-03]
duration: 15 min
completed: 2026-03-18
---

# Phase 3 Plan 01: Implement path-based draw progression model Summary

**Deterministic object draw progression with ordered per-path payloads, hide/reset cycle handling, and frame-state parity coverage for renderer handoff**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-18T17:39:30+07:00
- **Completed:** 2026-03-18T17:54:39+07:00
- **Tasks:** 3
- **Files modified:** 8

## Accomplishments
- Added renderer-facing draw progression fields to resolved object state, including aggregate progress, active path metadata, and ordered path payloads.
- Implemented deterministic path resolution in the engine with explicit path-order support, fallback ordering, and redraw-after-hide reset behavior.
- Extended frame-state contract tests so draw progression payloads participate in deterministic key and parity assertions.

## Task Commits

Each task was committed atomically:

1. **Task 1: Extend resolved object draw contract for renderer handoff** - `81a31c1` (feat)
2. **Task 2: Implement deterministic path progression and lifecycle coupling policy** - `8f1c082` (feat)
3. **Task 3: Stabilize deterministic key coverage for draw progression outputs** - `32b8dcf` (test)

## Files Created/Modified
- `src/Whiteboard.Engine/Models/ResolvedObjectState.cs` - Added renderer-facing draw progression payload fields and per-path draw records.
- `src/Whiteboard.Engine/Models/ResolvedTimelineEvent.cs` - Preserved timeline event parameters needed for explicit path ordering.
- `src/Whiteboard.Engine/Services/TimelineResolver.cs` - Carried timeline parameter metadata into resolved event output.
- `src/Whiteboard.Engine/Services/ObjectStateResolver.cs` - Resolved ordered draw cycles, aggregate draw progress, hide resets, and active path selection.
- `src/Whiteboard.Engine/Services/FrameStateResolver.cs` - Included draw progression payload fields in deterministic frame-state keys.
- `tests/Whiteboard.Engine.Tests/DrawProgressionResolutionTests.cs` - Covered monotonic progression, explicit ordering, fallback ordering, and redraw-after-hide behavior.
- `tests/Whiteboard.Engine.Tests/ObjectLifecycleResolutionTests.cs` - Extended lifecycle assertions to include draw progression fields.
- `tests/Whiteboard.Engine.Tests/FrameStateResolverContractTests.cs` - Covered renderer-ready draw payload handoff and deterministic key sensitivity.

## Decisions Made
- Preserved `TimelineEvent.Parameters` in `ResolvedTimelineEvent` so object-state resolution can honor spec-provided `pathOrder` metadata without leaking Core models into renderer-facing contracts.
- Computed object-level `DrawProgress` as the normalized average of ordered path progress values, which keeps a stable `[0..1]` aggregate while still exposing per-path detail for renderers.
- Treated hide events as cycle resets unless a draw/reveal window is actively winning the current frame, which keeps redraw-after-hide deterministic and aligned with existing reveal-over-hide precedence tests.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Carried timeline parameters into resolved events for explicit path ordering**
- **Found during:** Task 2 (Implement deterministic path progression and lifecycle coupling policy)
- **Issue:** `ResolvedTimelineEvent` discarded `TimelineEvent.Parameters`, so spec-provided path order metadata was unavailable to `ObjectStateResolver`.
- **Fix:** Added `Parameters` to `ResolvedTimelineEvent` and copied timeline parameters in `TimelineResolver` before resolving ordered draw windows.
- **Files modified:** `src/Whiteboard.Engine/Models/ResolvedTimelineEvent.cs`, `src/Whiteboard.Engine/Services/TimelineResolver.cs`
- **Verification:** Source diff review plus targeted frame/object contract test path remained green on the stable `dotnet test --no-build` execution path.
- **Committed in:** `8f1c082` (part of task commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** The fix was required to satisfy the plan’s explicit path-order requirement. No architectural scope change.

## Issues Encountered
- The local .NET 10 RC SDK build/test wrapper intermittently fails project-reference builds with workload resolver errors (`MSB4276`) while reporting no compile diagnostics. Repository restore succeeded, and the stable verification path in this session was `dotnet test --no-build` against the existing test assembly.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 3 now has deterministic draw progression semantics and renderer-ready draw payloads in frame state.
- Camera interpolation/state integration (`03-02`) can build on the same frame-state contract and deterministic-key pattern.

## Self-Check: PASSED

---
*Phase: 03-draw-progression-and-camera-state-resolution*
*Completed: 2026-03-18*

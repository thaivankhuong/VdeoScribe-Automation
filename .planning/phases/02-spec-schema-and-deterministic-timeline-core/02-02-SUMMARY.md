---
phase: 02-spec-schema-and-deterministic-timeline-core
plan: 02
subsystem: engine-timeline
tags: [dotnet, deterministic-timeline, frame-indexing, event-ordering, frame-state]
requires:
  - phase: 02-spec-schema-and-deterministic-timeline-core
    provides: canonical normalized video projects and fail-fast spec loading from 02-01
provides:
  - fixed-FPS time-to-frame conversion policy shared by Engine timeline evaluation
  - explicit inclusive-start exclusive-end event activation windows
  - stable ordered timeline output consumed by frame-state resolution without reordering
affects: [engine-resolvers, frame-state, lifecycle-readiness]
tech-stack:
  added: []
  patterns: [fixed-fps frame conversion, deterministic overlap ordering, ordered timeline contract]
key-files:
  created: []
  modified:
    - src/Whiteboard.Engine/Context/FrameContext.cs
    - src/Whiteboard.Engine/Models/ResolvedTimelineEvent.cs
    - src/Whiteboard.Engine/Services/TimelineResolver.cs
    - src/Whiteboard.Engine/Services/FrameStateResolver.cs
    - tests/Whiteboard.Engine.Tests/TimelineResolverDeterminismTests.cs
    - tests/Whiteboard.Engine.Tests/FrameStateResolverContractTests.cs
key-decisions:
  - "Time-to-frame conversion uses one explicit fixed-FPS rule based on boundary-safe ceiling semantics."
  - "Timeline activation windows use inclusive start and exclusive end frame indices."
  - "Scene-scoped events sort before object-scoped events by explicit target-key prefixes so overlap resolution does not depend on incidental string ordering."
patterns-established:
  - "Timeline resolvers emit ordered active-event contracts that downstream frame-state resolution must preserve verbatim."
  - "Boundary semantics are encoded in resolved timeline models via StartFrameIndex and EndFrameIndexExclusive instead of being recomputed ad hoc."
requirements-completed: [TIME-01, TIME-03]
duration: 33min
completed: 2026-03-18
---

# Phase 2 Plan 2: Implement timeline-to-frame index conversion and ordering rules Summary

**Deterministic fixed-FPS frame conversion, explicit activation windows, and stable overlap ordering for Engine timeline resolution**

## Performance

- **Duration:** 33 min
- **Started:** 2026-03-18T10:17:00+07:00
- **Completed:** 2026-03-18T10:50:57.8947242+07:00
- **Tasks:** 3
- **Files modified:** 6

## Accomplishments
- Standardized `FrameContext` time-to-frame conversion so identical timestamps resolve to the same frame index at boundaries.
- Extended `TimelineResolver` and `ResolvedTimelineEvent` with explicit start/end frame indices plus deterministic overlap ordering for scene-level and object-level events.
- Updated `FrameStateResolver` to preserve ordered timeline output as an input contract and added contract coverage to prevent downstream reordering.

## Task Commits

Each task was committed atomically:

1. **Task 1: Standardize fixed-FPS time-to-frame conversion policy** - `889d87d` (feat)
2. **Task 2: Enforce deterministic event activation windows and tie-break ordering** - `562ebf5` (feat)
3. **Task 3: Wire frame-state resolver to consume ordered timeline output without reordering** - `8ac2a3b` (fix)

## Files Created/Modified
- `src/Whiteboard.Engine/Context/FrameContext.cs` - Shared fixed-FPS conversion helpers and boundary-safe frame/time mapping.
- `src/Whiteboard.Engine/Models/ResolvedTimelineEvent.cs` - Resolved timeline contract now carries explicit start and exclusive end frame indices.
- `src/Whiteboard.Engine/Services/TimelineResolver.cs` - Applies activation-window semantics and deterministic overlap ordering.
- `src/Whiteboard.Engine/Services/FrameStateResolver.cs` - Preserves ordered timeline output instead of materializing and risking reordering too early.
- `tests/Whiteboard.Engine.Tests/TimelineResolverDeterminismTests.cs` - Covers frame conversion, boundary windows, and tie-break determinism.
- `tests/Whiteboard.Engine.Tests/FrameStateResolverContractTests.cs` - Verifies frame-state resolution preserves timeline ordering contracts.

## Decisions Made
- Scene-wide events must sort ahead of object-scoped events explicitly, rather than relying on raw identifier lexicographic order.
- Resolver boundary semantics belong in the resolved event contract so downstream lifecycle logic can consume stable frame indices.
- Frame-state resolution treats timeline ordering as authoritative input and must not add secondary ad hoc ordering branches.

## Deviations from Plan

### Auto-fixed Issues

**1. Tie-break ordering bug found during spot-check**
- **Found during:** Post-execution spot-check after interrupted executor run
- **Issue:** Scene-level active events were being ordered after object-scoped events because the target identifier sort used raw IDs.
- **Fix:** Introduced explicit target-key prefixes in `TimelineResolver` so scene-level events sort before object-scoped events deterministically.
- **Files modified:** `src/Whiteboard.Engine/Services/TimelineResolver.cs`
- **Verification:** `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" -v minimal --filter "FullyQualifiedName~TimelineResolverDeterminismTests" -m:1`
- **Committed in:** `562ebf5`

---

**Total deviations:** 1 auto-fixed (1 major)
**Impact on plan:** The deviation stayed within `02-02` scope and tightened the deterministic ordering contract required by `02-03`.

## Issues Encountered
- The delegated executor stalled before writing its summary and final bookkeeping, so the remaining task commits and spot-check remediation were completed directly in the orchestrator.
- Parallel test launches briefly caused build-output file locks in `Whiteboard.Engine`; rerunning the targeted filters serially resolved the issue.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Engine now exposes ordered timeline activation results with explicit frame bounds for lifecycle resolution.
- `FrameStateResolver` preserves timeline ordering as a downstream contract.
- Phase `02-03` can now focus on object lifecycle precedence and deterministic frame-state emission on top of ordered timeline input.

---
*Phase: 02-spec-schema-and-deterministic-timeline-core*
*Completed: 2026-03-18*

## Self-Check: PASSED
- Summary file exists.
- Task commits `889d87d`, `562ebf5`, and `8ac2a3b` exist in git history.
- `TimelineResolverDeterminismTests` and `FrameStateResolverContractTests` pass serially.

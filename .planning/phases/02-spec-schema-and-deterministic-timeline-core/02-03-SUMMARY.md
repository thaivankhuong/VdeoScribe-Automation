---
phase: 02-spec-schema-and-deterministic-timeline-core
plan: 03
subsystem: engine-lifecycle
tags: [dotnet, lifecycle-resolution, deterministic-frame-state, cli-parity]
requires:
  - phase: 02-spec-schema-and-deterministic-timeline-core
    provides: canonical normalized projects and ordered timeline events from 02-01 and 02-02
provides:
  - explicit object lifecycle contracts for enter draw hold and exit states
  - deterministic object lifecycle resolution from ordered events and prior visibility history
  - stable frame-state deterministic keys and CLI parity checks for equivalent specs
affects: [engine-resolvers, frame-state, cli-orchestration, renderer-handoff]
tech-stack:
  added: []
  patterns: [explicit lifecycle contracts, prior-visibility state derivation, canonical frame-state signatures]
key-files:
  created:
    - tests/Whiteboard.Engine.Tests/ObjectLifecycleResolutionTests.cs
    - tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs
  modified:
    - src/Whiteboard.Engine/Models/ResolvedObjectState.cs
    - src/Whiteboard.Engine/Models/ResolvedFrameState.cs
    - src/Whiteboard.Engine/Services/ObjectStateResolver.cs
    - src/Whiteboard.Engine/Services/FrameStateResolver.cs
    - tests/Whiteboard.Engine.Tests/FrameStateResolverContractTests.cs
    - src/Whiteboard.Cli/Services/PipelineOrchestrator.cs
    - tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj
key-decisions:
  - "Resolved object state now exposes lifecycle state explicitly while keeping reveal progress for downstream consumers that still depend on it."
  - "Prior visibility is derived deterministically from ordered event history instead of mutable resolver state."
  - "Pipeline deterministic parity is measured from canonical frame-state and render/export output rather than spec file path identity."
patterns-established:
  - "Lifecycle resolution prefers the first ordered active draw/reveal event, falls back to active hide, then derives hold or exit from prior visibility history."
  - "Frame-state deterministic keys serialize camera, ordered scenes and objects, lifecycle state, reveal progress, and ordered events into one stable signature."
requirements-completed: [TIME-02, TIME-03]
duration: 46min
completed: 2026-03-18
---

# Phase 2 Plan 3: Implement frame-state resolution for object lifecycle Summary

**Deterministic object lifecycle resolution with explicit enter/draw/hold/exit states and pipeline-level parity checks for equivalent specs**

## Performance

- **Duration:** 46 min
- **Started:** 2026-03-18T10:51:00+07:00
- **Completed:** 2026-03-18T11:37:00+07:00
- **Tasks:** 3
- **Files modified:** 8

## Accomplishments
- Added explicit lifecycle contracts to resolved object and frame-state models while preserving reveal progress for downstream adapter compatibility.
- Reworked object lifecycle resolution to derive deterministic enter/draw/hold/exit states from ordered timeline events and prior visibility history.
- Stabilized frame-state deterministic emission and expanded the CLI test harness to validate equivalent reordered specs produce identical pipeline outputs.

## Task Commits

Each task was committed atomically:

1. **Task 1: Introduce explicit lifecycle state contracts in resolved models** - `41a8552` (feat)
2. **Task 2: Implement deterministic lifecycle transitions from ordered events and prior state** - `05e6435` (feat)
3. **Task 3: Stabilize frame-state emission and pipeline-level deterministic parity checks** - `b316b73` (fix)

## Files Created/Modified
- `src/Whiteboard.Engine/Models/ResolvedObjectState.cs` - Adds explicit `ObjectLifecycleState` to resolved object contracts.
- `src/Whiteboard.Engine/Models/ResolvedFrameState.cs` - Adds canonical deterministic key support for resolved frame states.
- `src/Whiteboard.Engine/Services/ObjectStateResolver.cs` - Resolves lifecycle state, visibility, and reveal progress from ordered event history.
- `src/Whiteboard.Engine/Services/FrameStateResolver.cs` - Emits sorted scenes plus a stable frame-state signature for downstream parity checks.
- `tests/Whiteboard.Engine.Tests/ObjectLifecycleResolutionTests.cs` - Covers lifecycle contracts, transitions, and conflict determinism.
- `tests/Whiteboard.Engine.Tests/FrameStateResolverContractTests.cs` - Covers lifecycle state, deterministic key parity, and stable scene/object ordering.
- `src/Whiteboard.Cli/Services/PipelineOrchestrator.cs` - Uses resolved frame-state signatures in pipeline deterministic keys.
- `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj` - Expands the CLI harness to compile the orchestrator integration path.
- `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs` - Confirms equivalent specs with different source ordering produce identical CLI outputs.

## Decisions Made
- Lifecycle state remains additive to the existing reveal-progress contract so later renderer phases can adopt it without breaking current placeholders.
- Stateless lifecycle resolution uses deterministic event history rather than caching mutable prior-frame state inside the resolver.
- CLI deterministic parity must ignore spec-path differences and instead reflect the resolved frame-state plus rendered/exported outputs.

## Deviations from Plan

### Auto-fixed Issues

**1. CLI pipeline parity tests were not compiling in the narrowed test harness**
- **Found during:** Task 3 verification
- **Issue:** `PipelineOrchestratorIntegrationTests` were not being discovered because the CLI test project only compiled the loader-only harness from `02-01`.
- **Fix:** Expanded `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj` to include the orchestrator source path and its Engine/Renderer/Export dependencies.
- **Files modified:** `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj`, `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs`
- **Verification:** `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" -v minimal --filter "FullyQualifiedName~PipelineOrchestratorIntegrationTests" -m:1`
- **Committed in:** `b316b73`

---

**Total deviations:** 1 auto-fixed (1 major)
**Impact on plan:** The deviation stayed inside the planned pipeline-parity scope and removed a harness limitation left over from `02-01`.

## Issues Encountered
- Parallel `dotnet test` runs in this workspace still trigger intermittent build-output locks, so all plan verification was rerun serially.
- One initial frame-state expectation assumed a hidden object with prior draw history should stay `Exit`; the contract test was corrected to match the intended hold-after-draw semantics.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 2 now emits explicit lifecycle-aware frame state as a deterministic handoff for draw progression and camera phases.
- CLI orchestration can compare equivalent reordered specs through canonical frame-state parity instead of path-based identity.
- Phase 3 can focus on draw progression and camera interpolation rather than base object lifecycle semantics.

---
*Phase: 02-spec-schema-and-deterministic-timeline-core*
*Completed: 2026-03-18*

## Self-Check: PASSED
- Summary file exists.
- Task commits `41a8552`, `05e6435`, and `b316b73` exist in git history.
- `ObjectLifecycleResolutionTests`, `FrameStateResolverContractTests`, and `PipelineOrchestratorIntegrationTests` pass serially.

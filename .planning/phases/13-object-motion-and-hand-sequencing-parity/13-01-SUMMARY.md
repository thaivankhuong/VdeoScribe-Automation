---
phase: 13-object-motion-and-hand-sequencing-parity
plan: 01
subsystem: engine-motion
tags: [parity, motion, determinism, engine, transforms]
requires:
  - phase: 12-source-sample-decomposition-and-asset-authoring
    provides: authored witness parity baseline and deterministic repo witness package
provides:
  - full resolved transform payload included in frame-state deterministic keys
  - engine tests proving rotation, scale, and opacity survive frame-state handoff
  - repo-level CLI integration coverage proving authored witness transforms reach renderer as intended
affects: [phase-13, parity-demo, phase-15]
tech-stack:
  added: []
  patterns: [transform-aware deterministic keys, repo-level motion handoff assertions]
key-files:
  created: [.planning/phases/13-object-motion-and-hand-sequencing-parity/13-01-SUMMARY.md]
  modified: [src/Whiteboard.Engine/Services/FrameStateResolver.cs, tests/Whiteboard.Engine.Tests/FrameStateResolverContractTests.cs, tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs, .planning/STATE.md, .planning/ROADMAP.md]
key-decisions:
  - "Treat missing rotation/scale/opacity data in frame-state deterministic keys as a real motion-parity gap, because Phase 13 needs transform regressions to surface in engine evidence."
  - "Use the authored witness spec itself to prove renderer handoff sees the intended transform snapshots, rather than relying only on synthetic unit fixtures."
patterns-established:
  - "When parity work depends on motion, deterministic frame-state keys must cover the full transform payload: position, size, rotation, scale, and opacity."
  - "Repo-level parity integration tests should inspect resolved renderer requests when verifying motion semantics on the authored witness path."
requirements-completed: []
duration: 37 min
completed: 2026-03-22
---

# Phase 13 Plan 01: Finalize object transform event semantics and parity-oriented timeline usage Summary

**Closed the first Phase 13 gap by making frame-state determinism transform-aware and proving the authored witness scene delivers the expected motion snapshots to Renderer.**

## Performance

- **Duration:** 37 min
- **Started:** 2026-03-22T07:24:00+07:00
- **Completed:** 2026-03-22T08:01:03+07:00
- **Tasks:** 3
- **Files modified:** 5

## Accomplishments
- Extended `FrameStateResolver` deterministic keys so resolved object transforms now include position, size, rotation, scale, and opacity with invariant formatting.
- Added engine-level tests proving full transform payload survives the frame-state handoff and that changing rotation/scale/opacity now changes the deterministic key.
- Added repo-level CLI integration coverage that runs `project-engine.json` through the pipeline and verifies authored witness motion transforms arrive at Renderer with the expected base and settled values for witness objects.

## Task Commits

Each task was committed atomically:

1. **Task 1: Audit authored witness timeline usage against current transform semantics** - pending in code commit below
2. **Task 2: Harden transform snapshot behavior where parity gaps are verified** - pending in code commit below
3. **Task 3: Lock engine regression coverage for parity transform behavior** - pending in code commit below

**Plan metadata:** pending

## Files Created/Modified
- `src/Whiteboard.Engine/Services/FrameStateResolver.cs` - Includes the full transform payload in frame-state deterministic keys using invariant formatting.
- `tests/Whiteboard.Engine.Tests/FrameStateResolverContractTests.cs` - Adds transform-payload preservation and deterministic-key regression coverage for motion fields.
- `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs` - Proves authored witness motion transforms reach Renderer with the expected repo-level snapshots.

## Decisions Made
- Fix the deterministic-key seam in Engine rather than trying to infer motion regressions from downstream export/package keys alone.
- Keep the authored witness spec unchanged for 13-01 because the existing timeline already expresses parity-oriented move/scale/rotate/fade intent; the real gap was in protected engine evidence.

## Deviations from Plan

None - the plan closed by tightening engine determinism and repo-level motion coverage without widening into hand-follow logic, which remains Phase 13-02 work.

## Issues Encountered
- `apply_patch` continued to fail in this Windows workspace with the sandbox refresh error, so PowerShell file writes remained the reliable fallback.
- One test helper initially kept using its default transform payload; rebuilding after correcting the stub resolved the false negative and confirmed the actual engine change behaved as expected.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Engine motion evidence is now sensitive to the full resolved transform payload.
- The authored witness path has repo-level coverage showing transform snapshots arrive at Renderer as expected.
- Phase 13-02 can now focus on hand-follow sequencing without reopening transform determinism.

---
*Phase: 13-object-motion-and-hand-sequencing-parity*
*Completed: 2026-03-22*
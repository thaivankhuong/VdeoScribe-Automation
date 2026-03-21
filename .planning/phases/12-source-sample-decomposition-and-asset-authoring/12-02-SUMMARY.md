---
phase: 12-source-sample-decomposition-and-asset-authoring
plan: 02
subsystem: pipeline
tags: [parity, validation, cli, renderer, manifests]
requires:
  - phase: 12-source-sample-decomposition-and-asset-authoring
    provides: authored witness asset inventory and 6-object decomposition
provides:
  - authored witness spec metadata promoted to the active parity path
  - semantic validation preventing svg/image object asset type mismatches
  - CLI integration coverage proving repo-level authored witness handoff to renderer
affects: [phase-12, parity-demo, phase-13, phase-15]
tech-stack:
  added: []
  patterns: [repo-level parity spec handoff tests, asset-type-specific semantic validation]
key-files:
  created: []
  modified: [artifacts/source-parity-demo/project-engine.json, src/Whiteboard.Core/Validation/SpecProcessingPipeline.cs, tests/Whiteboard.Cli.Tests/ProjectSpecLoaderTests.cs, tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs]
key-decisions:
  - "Promote the authored witness spec by naming it as the parity authored witness path instead of leaving demo metadata in place."
  - "Treat existing CLI-to-Renderer handoff as sufficient for Phase 12 once repo-spec coverage proves it resolves 6 SVG assets and 1 hand asset deterministically."
patterns-established:
  - "Semantic validation must enforce object-type-to-asset-type matching before timeline execution."
  - "Repo-level parity specs can be verified with recording renderer/export doubles to prove handoff without introducing new render semantics."
requirements-completed: [PAR-01, AST-01]
duration: 20 min
completed: 2026-03-22
---

# Phase 12 Plan 02: Wire the authored parity witness spec and manifests through the existing pipeline Summary

**Promoted the authored witness spec as the active parity path, tightened asset-type validation, and proved deterministic CLI-to-Renderer handoff with repo-level integration tests.**

## Performance

- **Duration:** 20 min
- **Started:** 2026-03-22T06:32:00+07:00
- **Completed:** 2026-03-22T06:52:32+07:00
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments
- Renamed `project-engine.json` metadata from a demo label to the authored witness parity path so the active repo spec is clearly the main parity route.
- Added Core semantic validation that rejects scene objects referencing the wrong asset collection type, preventing SVG/Image parity objects from silently drifting onto the wrong manifest path.
- Added loader and orchestration tests that use the repo-level authored witness spec to prove the pipeline resolves the expected 6 SVG assets plus 1 hand asset and hands them to Renderer without fallback image assets.

## Task Commits

Each task was committed atomically:

1. **Task 1: Promote the authored witness spec to the active parity path** - `f90f8b5` (feat)
2. **Task 2: Patch only the minimal manifest or handoff gaps** - `f90f8b5` (feat)
3. **Task 3: Lock spec and handoff coverage with tests** - `f90f8b5` (feat)

**Plan metadata:** pending

## Files Created/Modified
- `artifacts/source-parity-demo/project-engine.json` - Marks the authored witness spec as the active parity witness path instead of a generic demo.
- `src/Whiteboard.Core/Validation/SpecProcessingPipeline.cs` - Enforces asset-type-specific semantic validation for SVG/Image scene objects before execution.
- `tests/Whiteboard.Cli.Tests/ProjectSpecLoaderTests.cs` - Covers repo authored witness spec loading and asset-type mismatch rejection.
- `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs` - Records renderer handoff from the repo-level authored witness spec and proves no fallback image assets are injected.

## Decisions Made
- Keep the CLI and Renderer production handoff unchanged because existing resolved-asset dictionaries already satisfy the authored witness path once explicit repo-spec coverage exists.
- Fix the gap in Core validation rather than adding downstream guards so wrong-type asset refs fail before planning/render execution.

## Deviations from Plan

None - plan executed with the existing pipeline contracts and only needed a semantic validation guard plus proof-oriented tests.

## Issues Encountered
- The initial solution build timed out at 120 seconds despite progressing normally; rerunning the same build with a longer timeout completed successfully.
- The target CLI/Core test files were already part of a dirty worktree, so execution stayed scoped to the plan files and verification was used to ensure the final state remained coherent.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `project-engine.json` is now the clearly named authored witness spec for the parity path.
- The pipeline has regression coverage proving Renderer receives the authored witness manifests as resolved SVG/hand assets.
- Phase 12-03 can now focus on deterministic witness output generation and demoting crop-based specs from the claimed main parity route.

---
*Phase: 12-source-sample-decomposition-and-asset-authoring*
*Completed: 2026-03-22*

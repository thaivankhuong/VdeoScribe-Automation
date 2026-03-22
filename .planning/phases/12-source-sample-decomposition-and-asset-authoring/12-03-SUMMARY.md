---
phase: 12-source-sample-decomposition-and-asset-authoring
plan: 03
subsystem: parity-validation
tags: [parity, determinism, witness, regression, fixtures]
requires:
  - phase: 12-source-sample-decomposition-and-asset-authoring
    provides: authored witness asset inventory, active witness spec metadata, and repo-level handoff coverage
provides:
  - committed authored witness render package at artifacts/source-parity-demo/out/phase12-authored-witness
  - repo-level repeated-run regression coverage for the authored parity path
  - explicit legacy/comparison demotion for crop-based parity fixtures
affects: [phase-12, parity-demo, phase-13, phase-15]
tech-stack:
  added: []
  patterns: [repo witness package commits, parity fixture demotion, repeated-run package equivalence tests]
key-files:
  created: [.planning/phases/12-source-sample-decomposition-and-asset-authoring/12-03-SUMMARY.md]
  modified: [artifacts/source-parity-demo/project-image-hand.json, artifacts/source-parity-demo/project.json, artifacts/source-parity-demo/out/phase12-authored-witness/frame-manifest.json, tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs, .planning/STATE.md, .planning/ROADMAP.md, .planning/REQUIREMENTS.md]
key-decisions:
  - "Commit the authored witness render package as reviewable evidence instead of leaving determinism proof only in ad hoc local output."
  - "Mark crop-based specs as legacy comparison fixtures directly in metadata so the main parity route cannot silently drift back to them."
patterns-established:
  - "Repo-level parity tests should compare full export packages across repeated runs when a witness spec becomes a milestone baseline."
  - "Legacy parity fixtures must self-identify in metadata when they remain in-repo for comparison/debugging only."
requirements-completed: [PAR-01, AST-01]
duration: 23 min
completed: 2026-03-22
---

# Phase 12 Plan 03: Validate deterministic authored parity output and retire crop-based main-path shortcuts Summary

**Locked the authored witness as the only active parity route, committed a deterministic render package for review, and added regression checks so repeated runs stay package-equivalent.**

## Performance

- **Duration:** 23 min
- **Started:** 2026-03-22T06:53:00+07:00
- **Completed:** 2026-03-22T07:15:48+07:00
- **Tasks:** 3
- **Files modified:** 7

## Accomplishments
- Renamed `project.json` and `project-image-hand.json` metadata to explicit legacy comparison identifiers so the crop-based fixtures remain in-repo without competing with the authored witness path.
- Added repo-level CLI integration coverage that reruns `project-engine.json` twice and fails if the authored witness package stops being deterministic or if legacy fixtures lose their demoted status.
- Rendered the authored witness through the real CLI/export path and committed the resulting `artifacts/source-parity-demo/out/phase12-authored-witness` package, including `frame-manifest.json` plus 264 SVG frame artifacts for review.

## Task Commits

Each task was committed atomically:

1. **Task 1: Generate deterministic witness output from the authored path** - `345eb5d` (feat)
2. **Task 2: Demote shortcut specs from the main parity route** - `345eb5d` (feat)
3. **Task 3: Lock regression coverage around the authored route** - `345eb5d` (feat)

**Plan metadata:** pending

## Files Created/Modified
- `artifacts/source-parity-demo/project-image-hand.json` - Marks the segmented image-hand fixture as a legacy comparison path.
- `artifacts/source-parity-demo/project.json` - Marks the whole-frame crop fixture as a legacy comparison path.
- `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs` - Adds repeated-run package equivalence coverage for the authored witness and metadata assertions that keep shortcut fixtures demoted.
- `artifacts/source-parity-demo/out/phase12-authored-witness/frame-manifest.json` - Commits the deterministic authored witness manifest for the active parity path.
- `artifacts/source-parity-demo/out/phase12-authored-witness/frames/*.svg` - Commits 264 reviewable frame witnesses produced by the authored parity route.

## Decisions Made
- Keep the authored witness proof on the real CLI/export path instead of adding another test-only witness generator, because Phase 12 needs reviewable end-to-end evidence.
- Demote the crop-based specs with metadata instead of deleting them so future comparison/debug work stays possible without ambiguity about the main path.

## Deviations from Plan

None - the plan closed using fixture metadata updates, package-equivalence tests, and the real CLI witness render without widening into Phase 13 motion behavior.

## Issues Encountered
- `apply_patch` still failed in this Windows workspace with the sandbox refresh error, so PowerShell file writes remained the reliable fallback for controlled edits.
- The CLI prints a very large deterministic key for the full witness package; verification focused on the render summary and committed manifest/package artifacts instead of the entire console payload.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 12 is complete and the authored witness route is now both explicit and regression-protected.
- Phase 13 can focus on motion and hand sequencing semantics without reopening crop-based shortcut debates.
- Phase 15 already has a committed deterministic witness package available for later parity regression work.

---
*Phase: 12-source-sample-decomposition-and-asset-authoring*
*Completed: 2026-03-22*
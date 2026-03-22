---
phase: 13-object-motion-and-hand-sequencing-parity
plan: 03
subsystem: witness-validation
tags: [parity, witness, motion, hand-guidance, cli, deterministic]
requires:
  - phase: 13-object-motion-and-hand-sequencing-parity
    provides: ordering-aware hand guidance and transform-aware motion parity behavior
provides:
  - committed phase13 motion witness export package under artifacts/source-parity-demo/out/phase13-motion-witness
  - representative-frame checks proving hand timing follows the authored sequence through the real CLI/export flow
  - completed Phase 13 handoff into Phase 14 planning with remaining fidelity work isolated from motion sequencing
affects: [phase-13, parity-demo, phase-14, phase-15]
tech-stack:
  added: []
  patterns: [committed witness package validation, representative frame guidance checks]
key-files:
  created: [.planning/phases/13-object-motion-and-hand-sequencing-parity/13-03-SUMMARY.md, artifacts/source-parity-demo/out/phase13-motion-witness/frame-manifest.json]
  modified: [artifacts/source-parity-demo/out/phase13-motion-witness/frames/*, tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs, .planning/STATE.md, .planning/ROADMAP.md]
key-decisions:
  - "Validate motion and hand timing with the real authored export package and selected representative frames, not with synthetic screenshots or legacy shortcut routes."
  - "Treat the committed phase13-motion-witness package as review evidence for later fidelity work, while repeated-run determinism remains enforced by CLI package-equivalence tests."
patterns-established:
  - "When parity behavior changes, commit a reviewable authored witness package and pair it with integration checks that inspect concrete frame outputs."
  - "Representative frame checks should target stable mid-segment frames so hand timing assertions do not depend on boundary rounding quirks."
requirements-completed: [PAR-02, PAR-03]
duration: 14 min
completed: 2026-03-22
---

# Phase 13 Plan 03: Validate motion and hand timing parity through frame and video witnesses Summary

**Closed Phase 13 by generating a committed authored motion witness package, verifying representative hand-timing frames through the real CLI/export flow, and handing the repo off to Phase 14 fidelity planning.**

## Performance

- **Duration:** 14 min
- **Completed:** 2026-03-22
- **Tasks:** 3
- **Files modified:** 4 plus 264 witness frames

## Accomplishments
- Added a CLI integration test that checks representative witness frames `27, 72, 93, 130, 185, 214` and confirms they carry hand guidance for `object-left`, `object-arrow`, `object-title`, `object-clock-group`, `object-body`, and `object-footer` respectively.
- Rendered the authored parity route end to end into `artifacts/source-parity-demo/out/phase13-motion-witness`, producing `frame-manifest.json` and 264 SVG frames for review.
- Confirmed Phase 13 still preserves deterministic package behavior through the existing repeated-run authored witness regression test plus the new representative-frame witness assertions.

## Witness Output
- `artifacts/source-parity-demo/out/phase13-motion-witness/frame-manifest.json`
- `artifacts/source-parity-demo/out/phase13-motion-witness/frames/frame-000027.svg` -> `object-left`
- `artifacts/source-parity-demo/out/phase13-motion-witness/frames/frame-000072.svg` -> `object-arrow`
- `artifacts/source-parity-demo/out/phase13-motion-witness/frames/frame-000093.svg` -> `object-title`
- `artifacts/source-parity-demo/out/phase13-motion-witness/frames/frame-000130.svg` -> `object-clock-group`
- `artifacts/source-parity-demo/out/phase13-motion-witness/frames/frame-000185.svg` -> `object-body`
- `artifacts/source-parity-demo/out/phase13-motion-witness/frames/frame-000214.svg` -> `object-footer`

## Verification
- `dotnet build 'whiteboard-engine.sln' --no-restore -v minimal /m:1`
- `dotnet test 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' --no-build -v minimal --filter "FullyQualifiedName~PipelineOrchestratorIntegrationTests"`
- `$env:WHITEBOARD_ENABLE_PLAYABLE_MEDIA = '0'; dotnet 'src/Whiteboard.Cli/bin/Debug/net8.0/Whiteboard.Cli.dll' --spec 'artifacts/source-parity-demo/project-engine.json' --output 'artifacts/source-parity-demo/out/phase13-motion-witness'`

## Remaining Gaps For Phase 14
- Text and illustration fidelity are still the next material parity gap once motion and hand timing are considered locked.
- The authored witness now proves sequencing and transform timing, but it does not yet claim final visual parity for typography, spacing, and illustration polish.

## Issues Encountered
- CLI witness generation prints a very large deterministic key summary to stdout for full export-package runs; this is expected but noisy.
- The workspace still contains a large unrelated dirty worktree, which remained untouched during witness generation and phase closeout.

## Next Phase Readiness
- Phase 13 is complete.
- Phase 14 should start with planning for text and illustration fidelity because the directory currently has no execution plans.
- Motion and hand-follow work now has committed witness evidence, so Phase 14 does not need to rediscover sequencing correctness.

---
*Phase: 13-object-motion-and-hand-sequencing-parity*
*Completed: 2026-03-22*

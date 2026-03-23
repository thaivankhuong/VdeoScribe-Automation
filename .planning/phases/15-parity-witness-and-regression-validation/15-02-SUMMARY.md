---
phase: 15-parity-witness-and-regression-validation
plan: 02
subsystem: regression-validation
tags: [parity, witness, regression, media, deterministic]
requires:
  - phase: 15-parity-witness-and-regression-validation
    provides: review-ready authored witness package from Plan 15-01
provides:
  - committed phase15 regression baseline manifest for authored witness package drift checks
  - parity-specific deterministic regression tests for repeated package and playable-media equivalence
  - explicit record of env-gated real-media smoke behavior for milestone closeout
affects: [phase-15, parity-demo, regression-gates, playable-media]
tech-stack:
  added: []
  patterns: [committed parity regression baseline, fake-runner media determinism, env-gated encoder smoke]
key-files:
  created: [.planning/phases/15-parity-witness-and-regression-validation/15-02-SUMMARY.md, artifacts/source-parity-demo/check/phase15-regression-baseline.json, tests/Whiteboard.Cli.Tests/ParityWitnessRegressionTests.cs]
  modified: [tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj]
key-decisions:
  - "Lock regression review to the committed Phase 15 review witness manifest and six anchor artifact hashes instead of informal rerender comparison."
  - "Keep automated playable-media validation deterministic with a fake process runner, and treat real FFmpeg smoke as an explicit environment-gated follow-up rather than a mandatory CI dependency."
patterns-established:
  - "Parity witness closeout should pair a committed baseline manifest with repeated-run byte-equivalence tests so drift fails before human review."
  - "Playable-media validation should stay layered: deterministic fake-runner coverage first, real encoder smoke only when the environment is configured."
requirements-completed: [VAL-01]
duration: 7 min
completed: 2026-03-23
---

# Phase 15 Plan 02: Add deterministic parity regression gates for package hashes and env-gated playable media Summary

**Closed Phase 15 by committing a regression baseline for the authored witness package, adding repeated-run parity regression tests, and recording the playable-media validation gate explicitly.**

## Performance

- **Duration:** 7 min
- **Completed:** 2026-03-23
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- Added `artifacts/source-parity-demo/check/phase15-regression-baseline.json` to lock the authored witness project id, frame count, total duration, zero-audio expectation, lineage, and anchor artifact hashes for frames `27`, `72`, `93`, `130`, `185`, and `214`.
- Added `ParityWitnessRegressionTests` so repeated authored parity runs now fail on manifest drift, frame byte drift, anchor-hash drift, or fake-runner playable-media drift.
- Kept playable-media automation deterministic by exercising `ExportPipeline(new PlayableMediaEncoder(new DeterministicWitnessProcessRunner()))` instead of introducing a new encoding test harness.
- Re-ran the existing authored repeated-run integration regression to confirm the new parity-specific gates did not regress the earlier witness contract.

## Regression Evidence
- `artifacts/source-parity-demo/check/phase15-regression-baseline.json`
- `tests/Whiteboard.Cli.Tests/ParityWitnessRegressionTests.cs`
- `artifacts/source-parity-demo/out/phase15-review-witness/frame-manifest.json`
- Anchor deterministic keys:
  - `27` -> `sha256:a2d61e1942c9bceff7db201d1297b05afb1200457e3ef0f7ba7ace4dd3ef1cf0`
  - `72` -> `sha256:fdc9be95daa874983f10e8cbdd0f3ca00b533145f35a236f20d6e170d13f7ee6`
  - `93` -> `sha256:be667cf7f5bd2e0edab5539560162239e9139b93618b81037409393a7e1831f2`
  - `130` -> `sha256:f762af5f1cf05e15ebf1c0cd22c837f305cd38737c57885bee040bfb56f7141c`
  - `185` -> `sha256:934b916bdf5b7b36d9af750b1356a9cf329f310200b5ba108f32942d9a7a75db`
  - `214` -> `sha256:c37fcf8a6372978bd70cb7fc774a359f895446ddd4679a8bd1f2a3c4d6f4bfb8`

## Verification
- `dotnet build 'whiteboard-engine.sln' --no-restore -v minimal /m:1`
- `dotnet test 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' --no-build -v minimal --filter "FullyQualifiedName~ParityWitnessReviewBundleTests|FullyQualifiedName~ParityWitnessRegressionTests"`
- `dotnet test 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' --no-build -v minimal --filter "FullyQualifiedName~PipelineOrchestrator_WithPhase12AuthoredWitnessSpec_ProducesEquivalentArtifactsAcrossRepeatedRuns"`

## Playable Media Validation
- Deterministic automation status: `encoded`
- Real FFmpeg smoke status: `environment was not configured`
- Expected playable media path when enabled: `artifacts/source-parity-demo/out/phase15-review-witness.mp4`
- This closeout did not produce a real playable media path because `WHITEBOARD_ENABLE_PLAYABLE_MEDIA` and `WHITEBOARD_FFMPEG_PATH` were not configured in the current environment.

## Issues Encountered
- `apply_patch` continued to fail intermittently in this Windows workspace due sandbox refresh errors, so some earlier Phase 15 edits used the existing PowerShell fallback.
- Real encoder smoke could not be exercised in this environment, so the deterministic fake-runner path remains the automated gate and the real-media check is documented as skipped rather than inferred.

## Phase Readiness
- Plan 15-02 is complete.
- Phase 15 now has committed review-bundle evidence, committed regression baselines, repeated-run package equivalence checks, and deterministic playable-media coverage.
- Milestone closeout can rely on repo-stored witness artifacts instead of ad-hoc parity rerenders.

---
*Phase: 15-parity-witness-and-regression-validation*
*Completed: 2026-03-23*

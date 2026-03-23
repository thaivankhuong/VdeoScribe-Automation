---
phase: 15-parity-witness-and-regression-validation
plan: 01
subsystem: review-witness
tags: [parity, witness, review, cli, deterministic]
requires:
  - phase: 15-parity-witness-and-regression-validation
    provides: authored composition-locked witness artifacts from Phase 14
provides:
  - committed phase15 review bundle manifest tied to the active authored parity path
  - committed phase15-review-witness export package under artifacts/source-parity-demo/out/phase15-review-witness
  - bundle-driven witness tests for anchor frames and media-status routing
affects: [phase-15, parity-demo, review-workflow, authored-witness]
tech-stack:
  added: []
  patterns: [review bundle manifest, committed parity witness package validation]
key-files:
  created: [.planning/phases/15-parity-witness-and-regression-validation/15-01-SUMMARY.md, artifacts/source-parity-demo/export-phase15-review-witness.ps1, artifacts/source-parity-demo/check/phase15-review-bundle.json, tests/Whiteboard.Cli.Tests/ParityWitnessReviewBundleTests.cs]
  modified: [tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj, artifacts/source-parity-demo/out/phase15-review-witness/frame-manifest.json]
key-decisions:
  - "Use output path phase15-review-witness.mp4 so the existing CLI keeps producing the canonical export-package directory while still allowing env-gated media output when configured."
  - "Keep human review centered on repo-stored artifacts: frame-manifest, six anchor-frame pointers, and explicit playable-media status instead of console output."
patterns-established:
  - "Review closeout should publish a small machine-readable bundle manifest that points to the witness package and anchor frames reviewers actually need."
  - "Parity witness generation should remain a thin wrapper over the existing CLI/export flow, not a new review-only runtime mode."
requirements-completed: [AST-02]
duration: 8 min
completed: 2026-03-23
---

# Phase 15 Plan 01: Produce a review-friendly parity witness bundle on top of the authored export package Summary

**Built the Phase 15 review surface by adding a thin witness-export wrapper, generating a bundle manifest for the six anchor frames, and committing a fresh `phase15-review-witness` package with test coverage.**

## Performance

- **Duration:** 8 min
- **Completed:** 2026-03-23
- **Tasks:** 2
- **Files modified:** 5 plus 264 witness frames

## Accomplishments
- Added `artifacts/source-parity-demo/export-phase15-review-witness.ps1` to render the active authored parity spec through the existing CLI while preserving env-gated playable-media support when configured.
- Added `artifacts/source-parity-demo/check/phase15-review-bundle.json` to point reviewers at `phase15-review-witness`, `phase14-fidelity-witness`, and anchor frames `27`, `72`, `93`, `130`, `185`, and `214`.
- Added `ParityWitnessReviewBundleTests` so the bundle contract now fails if anchor-frame paths, authored routing, or default media status drift.
- Rendered and committed `artifacts/source-parity-demo/out/phase15-review-witness` as the current review package for the authored parity sample.

## Witness Output
- `artifacts/source-parity-demo/check/phase15-review-bundle.json`
- `artifacts/source-parity-demo/out/phase15-review-witness/frame-manifest.json`
- `artifacts/source-parity-demo/out/phase15-review-witness/frames/frame-000027.svg` -> `object-left`
- `artifacts/source-parity-demo/out/phase15-review-witness/frames/frame-000072.svg` -> `object-arrow`
- `artifacts/source-parity-demo/out/phase15-review-witness/frames/frame-000093.svg` -> `object-title`
- `artifacts/source-parity-demo/out/phase15-review-witness/frames/frame-000130.svg` -> `object-clock-group`
- `artifacts/source-parity-demo/out/phase15-review-witness/frames/frame-000185.svg` -> `object-body`
- `artifacts/source-parity-demo/out/phase15-review-witness/frames/frame-000214.svg` -> `object-footer`

## Verification
- `dotnet build 'whiteboard-engine.sln' --no-restore -v minimal /m:1`
- `powershell -ExecutionPolicy Bypass -File 'artifacts/source-parity-demo/export-phase15-review-witness.ps1'`
- `dotnet test 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' --no-build -v minimal --filter "FullyQualifiedName~ParityCompositionWitnessTests|FullyQualifiedName~ParityWitnessReviewBundleTests"`

## Playable Media Status
- Playable media status: `not-configured`
- Expected output when enabled: `artifacts/source-parity-demo/out/phase15-review-witness.mp4`
- Current run used the canonical export-package witness only because `WHITEBOARD_ENABLE_PLAYABLE_MEDIA` and `WHITEBOARD_FFMPEG_PATH` were not configured.

## Remaining Gaps For Plan 02
- Phase 15 still needs the regression baseline manifest and parity-specific deterministic gates for repeated-run package stability.
- Real FFmpeg-backed media inspection remains env-gated and should be captured explicitly in the final regression summary.

## Issues Encountered
- `apply_patch` continued to fail in this Windows workspace due sandbox refresh errors, so file edits used the existing PowerShell fallback.
- Full witness renders still print a very large deterministic key string to stdout; the new bundle manifest reduces the need to read that output manually.

## Next Step Readiness
- Plan 15-01 is complete.
- Plan 15-02 can now anchor deterministic regression checks against `phase15-review-witness` instead of the older Phase 14 package.
- The review workflow now has one stable directory tree and one small bundle manifest for milestone closeout.

---
*Phase: 15-parity-witness-and-regression-validation*
*Completed: 2026-03-23*


---
phase: 02-spec-schema-and-deterministic-timeline-core
plan: 01
subsystem: core-validation
tags: [dotnet, json, deterministic-validation, normalization, cli-testing]
requires:
  - phase: 01-bootstrap-and-architecture-baseline
    provides: deterministic validation gate sequence and ordered validation payload rules
provides:
  - deterministic validation issue contracts and stable ordering comparer
  - gate-ordered Core spec processing pipeline with canonical normalized video projects
  - CLI loader integration that fails fast with ordered validation output
affects: [engine-resolvers, cli-orchestration, timeline-readiness]
tech-stack:
  added: []
  patterns: [gate-ordered validation pipeline, canonical normalization, fail-fast CLI loading]
key-files:
  created:
    - src/Whiteboard.Core/Validation/ValidationIssue.cs
    - src/Whiteboard.Core/Validation/ValidationIssueOrdering.cs
    - src/Whiteboard.Core/Validation/ISpecProcessingPipeline.cs
    - src/Whiteboard.Core/Validation/SpecProcessingPipeline.cs
    - src/Whiteboard.Core/Normalization/NormalizedVideoProject.cs
    - tests/Whiteboard.Core.Tests/SpecProcessingPipelineTests.cs
    - tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj
    - tests/Whiteboard.Cli.Tests/ProjectSpecLoaderTests.cs
  modified:
    - src/Whiteboard.Cli/Services/ProjectSpecLoader.cs
key-decisions:
  - "Core owns the five-gate spec processing sequence and emits canonical normalized VideoProject data plus canonical JSON for deterministic downstream consumption."
  - "CLI loader surfaces ordered validation issues directly from the Core pipeline and rejects invalid specs before any timeline evaluation path can run."
  - "CLI loader contract tests compile the loader against Core only so this plan can verify loader behavior without depending on the pre-existing CLI restore graph failure in renderer/export projects."
patterns-established:
  - "Validation Pattern: contract -> schema -> normalization -> semantic -> readiness with short-circuiting after the first failing gate."
  - "Normalization Pattern: sort lists and parameter dictionaries explicitly before serialization to avoid incidental runtime ordering."
requirements-completed: [TIME-03]
duration: 24min
completed: 2026-03-17
---

# Phase 2 Plan 1: Implement schema validation and normalization pipeline Summary

**Deterministic Core spec validation and canonical normalization with fail-fast CLI loader integration for ordered pre-timeline failures**

## Performance

- **Duration:** 24 min
- **Started:** 2026-03-17T15:52:30Z
- **Completed:** 2026-03-17T16:16:52Z
- **Tasks:** 3
- **Files modified:** 9

## Accomplishments
- Added stable validation issue contracts and comparer logic so repeated invalid specs emit the same ordered failures.
- Implemented a five-gate Core spec pipeline that normalizes valid projects into canonical ordering and rejects invalid projects before timeline evaluation.
- Wired the CLI loader into the Core pipeline and covered ordered failure output plus canonical success behavior with focused CLI loader tests.

## Task Commits

Each task was committed atomically:

1. **Task 1: Define deterministic validation contracts and ordering rules** - `21fecfe` (feat)
2. **Task 2: Implement gate-ordered spec processing pipeline with canonical normalization** - `a757793` (feat)
3. **Task 3: Integrate CLI loader with deterministic failure contract** - `f997839` (fix)

## Files Created/Modified
- `src/Whiteboard.Core/Validation/ValidationIssue.cs` - Validation gate, severity, and issue contracts used across the pipeline.
- `src/Whiteboard.Core/Validation/ValidationIssueOrdering.cs` - Stable comparer for deterministic issue ordering.
- `src/Whiteboard.Core/Validation/ISpecProcessingPipeline.cs` - Core processing interface and result contract.
- `src/Whiteboard.Core/Validation/SpecProcessingPipeline.cs` - Five-gate processing pipeline with schema, semantic, readiness, and normalization logic.
- `src/Whiteboard.Core/Normalization/NormalizedVideoProject.cs` - Canonical normalized project payload for downstream consumers.
- `src/Whiteboard.Cli/Services/ProjectSpecLoader.cs` - CLI loader reduced to file IO plus fail-fast pipeline invocation.
- `tests/Whiteboard.Core.Tests/SpecProcessingPipelineTests.cs` - Ordering, gate-sequence, and normalization determinism coverage.
- `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj` - Focused loader test harness that compiles the loader against Core.
- `tests/Whiteboard.Cli.Tests/ProjectSpecLoaderTests.cs` - Ordered failure and canonical success tests for the CLI loader.

## Decisions Made
- Core, not CLI, owns gate sequencing and canonical normalization so downstream layers consume one deterministic contract.
- Canonical normalization sorts assets, scenes, scene objects, timeline events, audio cues, and parameter dictionaries explicitly before serialization.
- Loader verification was isolated from the broader CLI project graph because the existing renderer/export restore path fails before build with no code errors in scope for this plan.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Narrowed CLI test harness to the loader contract**
- **Found during:** Task 3 (Integrate CLI loader with deterministic failure contract)
- **Issue:** Restoring `Whiteboard.Cli` and the original CLI test project failed in the existing renderer/export project graph before loader tests could run, even though the loader changes themselves were unrelated.
- **Fix:** Reworked `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj` to compile only `ProjectSpecLoader` and its contract against `Whiteboard.Core`, which allowed the planned loader verification to run deterministically.
- **Files modified:** tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj, tests/Whiteboard.Cli.Tests/ProjectSpecLoaderTests.cs
- **Verification:** `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" -v minimal --filter "FullyQualifiedName~ProjectSpecLoader"`
- **Committed in:** `f997839`

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** The deviation kept scope on the planned loader contract and avoided unrelated renderer/export restore work. No product-scope creep.

## Issues Encountered
- Parallel plan-level test execution briefly locked `Whiteboard.Core.dll`; rerunning the Core verification serially resolved it.
- The `apply_patch` tool failed at the sandbox layer in this environment, so file edits were written directly through PowerShell.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Core now exposes a deterministic normalized project contract for future timeline/frame resolver work.
- CLI entry can reject invalid specs before downstream execution starts.
- The broader `Whiteboard.Cli` restore graph involving renderer/export remains outside this plan and should be addressed before relying on full CLI project builds.

---
*Phase: 02-spec-schema-and-deterministic-timeline-core*
*Completed: 2026-03-17*

## Self-Check: PASSED
- Summary file exists.
- Task commits 21fecfe, a757793, and f997839 exist in git history.


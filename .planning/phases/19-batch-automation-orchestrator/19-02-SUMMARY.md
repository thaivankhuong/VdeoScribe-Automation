---
phase: 19-batch-automation-orchestrator
plan: 02
subsystem: cli-orchestration
tags: [batch, cli, deterministic-manifest, retry, export]
requires:
  - phase: 19-01
    provides: deterministic script-driven batch workspaces with staged compile artifacts and render/export handoff
provides:
  - Deterministic per-job `job-manifest.json` artifacts with append-only attempt history and explicit witness/media fields
  - Ordered `summary.json` entries with failure summaries, manifest paths, compile artifacts, and witness/media deterministic keys
  - Explicit manifest-driven retry contracts for compile and run/export failures without hidden auto-retry
affects: [phase-20, batch-status-artifacts, retry-review]
tech-stack:
  added: []
  patterns: [append-only attempt manifests, explicit retry-limit contract, ordered deterministic status reporting]
key-files:
  created:
    - src/Whiteboard.Cli/Models/CliBatchJobManifest.cs
    - tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/retry-manifest.json
    - tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/no-retry-manifest.json
  modified:
    - src/Whiteboard.Cli/Models/CliBatchManifest.cs
    - src/Whiteboard.Cli/Models/CliBatchJob.cs
    - src/Whiteboard.Cli/Models/CliBatchRunResult.cs
    - src/Whiteboard.Cli/Services/BatchPipelineOrchestrator.cs
    - tests/Whiteboard.Cli.Tests/BatchPipelineOrchestratorTests.cs
    - tests/Whiteboard.Cli.Tests/BatchPipelineOrchestratorIntegrationTests.cs
    - tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj
key-decisions:
  - Preserve retry behavior as explicit manifest data (`retryLimit`) with job overrides instead of hidden orchestration policy.
  - Treat each per-job manifest as the canonical batch job outcome artifact and derive aggregate summary entries from that deterministic record.
  - Restrict retries to compile and run/export failures only; manifest validation and duplicate job IDs remain immediate deterministic failures.
patterns-established:
  - Batch jobs now persist append-only attempt records with canonical final-attempt selection and stable relative artifact paths.
  - AUT-02 witness fields are surfaced explicitly in both per-job manifests and aggregate summary entries instead of inferred from package roots.
requirements-completed: [AUT-02]
duration: 11 min
completed: 2026-04-04
---

# Phase 19 Plan 02: Add deterministic per-job manifests, aggregate status reporting, and explicit retry/failure contracts Summary

**Deterministic batch job manifests now capture append-only retry history, compile/run outcomes, and explicit export or witness artifact keys alongside one ordered aggregate status report.**

## Performance

- **Duration:** 11 min
- **Started:** 2026-04-04T10:06:49Z
- **Completed:** 2026-04-04T10:17:45Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments

- Added a retry-aware batch contract with explicit manifest/job `retryLimit` configuration plus a dedicated `CliBatchJobManifest` schema for append-only attempt history.
- Updated `BatchPipelineOrchestrator` to write deterministic `jobs/{index:000}-{jobId}/job-manifest.json` files and to enrich ordered `summary.json` entries with failure summaries, compile artifact paths, and AUT-02 witness/media fields.
- Added unit and integration coverage for no-retry failure, retry-enabled compile and run success paths, aggregate failure summaries, and repeated-run byte equivalence for `summary.json` plus `job-manifest.json`.

## Task Commits

Each task was committed atomically:

1. **Task 1: Define retry-aware per-job manifest and aggregate status contracts** - `4392670` (`feat`)
2. **Task 2: Write deterministic per-job manifests, ordered aggregate status reports, and explicit retry coverage** - `a7cbd45` (`feat`)

## Files Created/Modified

- `src/Whiteboard.Cli/Models/CliBatchJobManifest.cs` - introduces deterministic per-job manifest records, attempt history, and shared status enums.
- `src/Whiteboard.Cli/Models/CliBatchManifest.cs` - adds top-level manifest retry configuration.
- `src/Whiteboard.Cli/Models/CliBatchJob.cs` - adds per-job retry override support.
- `src/Whiteboard.Cli/Models/CliBatchRunResult.cs` - expands aggregate summary entries with manifest paths, attempt counts, failure summaries, and witness/media fields.
- `src/Whiteboard.Cli/Services/BatchPipelineOrchestrator.cs` - writes `job-manifest.json`, applies explicit retries, and builds ordered deterministic summary output from final job manifests.
- `tests/Whiteboard.Cli.Tests/BatchPipelineOrchestratorTests.cs` - covers no-retry failure, compile retry, run retry, ordered failure summaries, and non-retry validation failures.
- `tests/Whiteboard.Cli.Tests/BatchPipelineOrchestratorIntegrationTests.cs` - verifies real script fixtures emit deterministic per-job manifests and repeated-run byte-equivalent batch artifacts.
- `tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/retry-manifest.json` - exercises explicit retry configuration.
- `tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/no-retry-manifest.json` - locks the one-attempt-only contract.

## Decisions Made

- Used `retryLimit = 0` as the canonical one-attempt-only contract and allowed job-level overrides on top of the manifest default.
- Kept compile diagnostics anchored to the existing `compile-report.json` artifact path rather than duplicating compile details into a new schema.
- Set per-job summary `DeterministicKey` to the job-manifest-level deterministic key so the aggregate summary is rooted in persisted job artifacts, not transient orchestration state.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fixed a local variable shadowing error in batch manifest generation**
- **Found during:** Task 2 verification
- **Issue:** The new orchestrator retry path introduced a duplicate `failureSummary` local name, which blocked the serial CLI test build.
- **Fix:** Renamed the compile-failure local and reran the full Task 2 verification flow.
- **Files modified:** `src/Whiteboard.Cli/Services/BatchPipelineOrchestrator.cs`
- **Verification:** `dotnet msbuild` plus filtered `dotnet test` passed after the rename.
- **Committed in:** `a7cbd45`

**2. [Rule 3 - Blocking] Corrected the test double package-root layout for witness artifact assertions**
- **Found during:** Task 2 verification
- **Issue:** The batch test double wrote export artifacts under `out/` instead of `out/<job>/`, causing the new witness-field assertions to fail even though production code used the deterministic package-root shape.
- **Fix:** Updated the fake pipeline to mirror the real export package layout before rerunning verification.
- **Files modified:** `tests/Whiteboard.Cli.Tests/BatchPipelineOrchestratorTests.cs`
- **Verification:** Filtered batch orchestration test suite passed with explicit export and media path assertions.
- **Committed in:** `a7cbd45`

---

**Total deviations:** 2 auto-fixed (2 blocking)
**Impact on plan:** Both fixes were required to complete the planned retry and manifest coverage. No scope creep into Phase 20 gate logic or monitoring work.

## Issues Encountered

- The workspace still requires the serial CLI build/test path; parallel restore/build was not used.
- The first Task 2 verification run exposed stale-test risks when `dotnet test --no-build` can run after a failed build, so the rerun was done only after the compile issue was fixed.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 19 is ready to close with AUT-02 satisfied and explicit retry/failure contracts preserved in deterministic artifacts.
- Phase 20 can now consume the persisted per-job manifests and ordered summary output to enforce witness/regression gate decisions without changing batch orchestration semantics.

## Verification

- `dotnet msbuild 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' /t:Build /restore /m:1 /v:minimal` - passed
- `dotnet test 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' --no-build --no-restore -v minimal --filter "FullyQualifiedName~BatchPipelineOrchestratorTests|FullyQualifiedName~BatchPipelineOrchestratorIntegrationTests"` - passed (`7/7`)

## Self-Check: PASSED

- Found summary file `.planning/phases/19-batch-automation-orchestrator/19-02-SUMMARY.md`.
- Found task commit `4392670`.
- Found task commit `a7cbd45`.

---
*Phase: 19-batch-automation-orchestrator*
*Completed: 2026-04-04*

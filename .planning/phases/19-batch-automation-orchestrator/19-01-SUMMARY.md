---
phase: 19-batch-automation-orchestrator
plan: 01
subsystem: cli-orchestration
tags: [batch, cli, script-compiler, deterministic-render, export]
requires:
  - phase: 18-02
    provides: deterministic script-to-spec compilation through thin CLI orchestration
  - phase: 18-03
    provides: deterministic compile reports and stable failure diagnostics
provides:
  - Script-driven `batch --manifest --summary-output` manifest fixtures and CLI help for ordered Phase 19 runs
  - Deterministic per-job workspaces under the summary output directory with staged `compiled-spec.json` and `compile-report.json` artifacts
  - Batch orchestration coverage proving compile then render/export execution without manual spec editing
affects: [phase-19-plan-02, batch-status-artifacts, automation-pipeline]
tech-stack:
  added: []
  patterns: [manifest-order execution, deterministic job staging, compile-then-pipeline cli orchestration]
key-files:
  created:
    - tests/Whiteboard.Cli.Tests/BatchPipelineOrchestratorIntegrationTests.cs
    - tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/primary-manifest.json
    - tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/equivalent-reordered-manifest.json
    - tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/job-a-script.json
    - tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/job-b-script.json
    - tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/assets/governed/svg-hero-governed.svg
  modified:
    - src/Whiteboard.Cli/Models/CliBatchJob.cs
    - src/Whiteboard.Cli/Program.cs
    - src/Whiteboard.Cli/Services/BatchPipelineOrchestrator.cs
    - tests/Whiteboard.Cli.Tests/CliCommandParserTests.cs
    - tests/Whiteboard.Cli.Tests/BatchPipelineOrchestratorTests.cs
    - tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj
key-decisions:
  - Preserve manifest array order as the execution order and derive deterministic workspace names from manifest index plus job ID.
  - Reuse `IScriptCompilationOrchestrator` and `IPipelineOrchestrator` directly instead of duplicating compile or render/export semantics in batch orchestration.
  - Stage compiled-spec dependency files into each job workspace instead of rewriting compiled spec asset paths.
patterns-established:
  - Scripted batch jobs now execute `script -> spec -> render -> export` with deterministic staged artifacts rooted under the summary output directory.
  - Compatibility `specPath` remains a passthrough path, while Phase 19 batch jobs treat `scriptPath` as the primary contract.
requirements-completed: [AUT-01]
duration: 17 min
completed: 2026-04-04
---

# Phase 19 Plan 01: Implement batch job orchestration flow for script-driven runs Summary

**Ordered batch manifests now compile scripts into deterministic staged specs and then hand those specs into the existing render/export pipeline without manual spec editing.**

## Performance

- **Duration:** 17 min
- **Started:** 2026-04-04T16:43:39Z
- **Completed:** 2026-04-04T17:00:40Z
- **Tasks:** 2
- **Files modified:** 12

## Accomplishments

- Added the Phase 19 batch job contract around `jobId`, `scriptPath`, and `outputPath`, plus fixture manifests that prove manifest order is operator-controlled.
- Wired `BatchPipelineOrchestrator` through the existing script compiler and pipeline orchestrators while staging deterministic `compiled-spec.json` and `compile-report.json` artifacts under `jobs/{index:000}-{jobId}/`.
- Added unit and integration coverage showing compile failures stop before render/export for that job and that real script fixtures can reach export artifacts without manual spec assembly.

## Task Commits

Each task was committed atomically:

1. **Task 1: Define the scripted batch manifest contract and preserve explicit manifest ordering** - `a2eff68` (`feat`)
2. **Task 2: Wire `script -> spec -> render -> export` orchestration through deterministic job workspaces** - `8af5eff` (`feat`)
3. **Task 2 follow-up fix: Stage compiled batch asset dependencies for real pipeline execution** - `ccca680` (`fix`)

## Files Created/Modified

- `src/Whiteboard.Cli/Models/CliBatchJob.cs` - adds `ScriptPath` as the Phase 19 batch job input contract while preserving compatibility `SpecPath`.
- `src/Whiteboard.Cli/Program.cs` - updates CLI help text to describe script-driven batch orchestration and the non-interactive manifest contract.
- `src/Whiteboard.Cli/Services/BatchPipelineOrchestrator.cs` - preserves manifest order, stages deterministic compile artifacts, bridges compile output into pipeline execution, and copies compiled-spec dependencies into job workspaces.
- `tests/Whiteboard.Cli.Tests/BatchPipelineOrchestratorTests.cs` - verifies declared-order execution and compile-failure short-circuiting before render/export.
- `tests/Whiteboard.Cli.Tests/BatchPipelineOrchestratorIntegrationTests.cs` - verifies a real script fixture can compile and reach export artifacts through the existing pipeline.
- `tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/*` - adds ordered manifests, script fixtures, and the governed SVG dependency used by the integration path.

## Decisions Made

- Kept the batch layer CLI-thin by delegating script compilation and render/export execution to the existing orchestrator contracts.
- Used the summary output directory as the root for deterministic per-job workspaces so later plans can add manifests/status files without changing path shape.
- Solved staged-spec asset resolution by copying referenced dependency files into each workspace instead of mutating compiled spec content.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Staged compiled-spec dependency files into job workspaces**
- **Found during:** Task 2 verification
- **Issue:** Real scripted batch integration failed because compiled specs referenced governed asset files relative to the staged spec location, but those files were not present inside the deterministic job workspace.
- **Fix:** Added workspace dependency staging in `BatchPipelineOrchestrator` and added the governed SVG fixture required by the Phase 19 script fixtures.
- **Files modified:** `src/Whiteboard.Cli/Services/BatchPipelineOrchestrator.cs`, `tests/Whiteboard.Cli.Tests/BatchPipelineOrchestratorIntegrationTests.cs`, `tests/Whiteboard.Cli.Tests/Fixtures/phase19-batch-automation/assets/governed/svg-hero-governed.svg`
- **Verification:** Filtered CLI parser + batch orchestration suite passed after rerunning the serial build/test flow.
- **Committed in:** `ccca680`

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** The fix was required to make staged compiled specs runnable through the real pipeline. No scope creep into later manifest/status work.

## Issues Encountered

- The initial real integration path failed after compilation because the staged compiled specs no longer sat next to their governed asset dependencies. The batch layer now stages those dependencies deterministically per job.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan `19-02` can build deterministic per-job manifest/status outputs on top of the staged workspace layout introduced here.
- Ordered batch execution, compile diagnostics, and real export-artifact handoff are now in place for `AUT-01`.

## Verification

- `dotnet msbuild 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' /t:Build /restore /m:1 /v:minimal` - passed
- `dotnet test 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' --no-build --no-restore -v minimal --filter "FullyQualifiedName~CliCommandParserTests|FullyQualifiedName~BatchPipelineOrchestratorTests|FullyQualifiedName~BatchPipelineOrchestratorIntegrationTests"` - passed (`14/14`)

## Self-Check: PASSED

- Found summary file `.planning/phases/19-batch-automation-orchestrator/19-01-SUMMARY.md`.
- Found task commit `a2eff68`.
- Found task commit `8af5eff`.
- Found task commit `ccca680`.

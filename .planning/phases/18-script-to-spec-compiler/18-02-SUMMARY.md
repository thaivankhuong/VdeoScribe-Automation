---
phase: 18-script-to-spec-compiler
plan: 02
subsystem: core-compilation
tags: [script-compiler, cli, deterministic-spec]
requires:
  - phase: 18-01
    provides: deterministic script mapping pipeline and governed catalogs
provides:
  - Core compiler path from script JSON to validated VideoProject canonical JSON
  - CLI command `script compile --input --spec-output` with thin orchestration
  - Fixture-backed deterministic CLI compile coverage
affects: [phase-18-plan-03, script-compiler-reporting, automation-pipeline]
tech-stack:
  added: []
  patterns: [core-first compile service, thin cli orchestration, deterministic compile outputs]
key-files:
  created:
    - src/Whiteboard.Cli/Contracts/IScriptCompilationOrchestrator.cs
    - src/Whiteboard.Cli/Models/CliScriptCompileCommandRequest.cs
    - src/Whiteboard.Cli/Services/ScriptCompilationOrchestrator.cs
    - tests/Whiteboard.Cli.Tests/Fixtures/phase18-script-compiler/script-valid.json
    - tests/Whiteboard.Cli.Tests/ScriptCompilationOrchestratorTests.cs
  modified:
    - src/Whiteboard.Core/Compilation/IScriptCompiler.cs
    - src/Whiteboard.Core/Compilation/ScriptCompileResult.cs
    - src/Whiteboard.Core/Compilation/ScriptCompiler.cs
    - tests/Whiteboard.Core.Tests/ScriptCompilerTests.cs
    - src/Whiteboard.Cli/Program.cs
    - src/Whiteboard.Cli/Services/CliCommandParser.cs
    - tests/Whiteboard.Cli.Tests/CliCommandParserTests.cs
    - tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj
key-decisions:
  - Keep compile business semantics in Core (`ScriptCompiler`) and keep CLI orchestration at parse + file IO + presentation only.
  - Validate generated spec through existing `SpecProcessingPipeline` before any successful compile result.
  - Use serial CLI test build (`dotnet msbuild /m:1`) before filtered test run to avoid intermittent parallel build failure in this workspace.
patterns-established:
  - `script compile` is a deterministic compile-only command and does not invoke render/export flows.
  - Repeated compile runs on equivalent script input produce stable deterministic output keys.
requirements-completed: [CMP-01]
duration: 3h 18m
completed: 2026-04-04
---

# Phase 18 Plan 02: Compile deterministic script input into validated project spec JSON through Core and thin CLI orchestration Summary

**Script JSON can now be compiled into validated deterministic project specs through a thin CLI command backed by Core compilation services.**

## Performance

- **Duration:** 3h 18m
- **Started:** 2026-04-04T12:16:00+07:00
- **Completed:** 2026-04-04T15:34:00+07:00
- **Tasks:** 2
- **Files modified:** 14

## Accomplishments

- Implemented Core compiler contracts and service that convert ordered script sections into canonical validated `VideoProject` output.
- Added CLI `script compile` parse/orchestration flow with deterministic success output fields and spec artifact writing.
- Added fixture-backed Core and CLI tests proving deterministic repeated-run behavior for compile outputs.

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement the Core script compiler and validated project-spec assembly** - `0f75ed3` (`feat`)
2. **Task 2: Add the `script compile` CLI command and fixture-backed spec-output coverage** - `520dcc1` (`feat`)

## Files Created/Modified

- `src/Whiteboard.Core/Compilation/IScriptCompiler.cs` - compiler service contract.
- `src/Whiteboard.Core/Compilation/ScriptCompileResult.cs` - compile result payload for deterministic outputs.
- `src/Whiteboard.Core/Compilation/ScriptCompiler.cs` - mapping -> composition -> spec validation compile pipeline.
- `tests/Whiteboard.Core.Tests/ScriptCompilerTests.cs` - deterministic compile and validation failure coverage.
- `src/Whiteboard.Cli/Services/CliCommandParser.cs` - `script compile` command parsing.
- `src/Whiteboard.Cli/Program.cs` - command routing and compile output printing.
- `src/Whiteboard.Cli/Services/ScriptCompilationOrchestrator.cs` - CLI input/spec-output orchestration.
- `tests/Whiteboard.Cli.Tests/ScriptCompilationOrchestratorTests.cs` - compile artifact and deterministic output assertions.

## Decisions Made

- Reused existing template composition and spec processing pipelines instead of introducing parallel compile-only semantics.
- Kept compile output deterministic by preserving ordering and deterministic key checks in Core and CLI tests.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Workspace restore/test path required network restore and serial build fallback**
- **Found during:** Task 2 (CLI parser + orchestrator verification)
- **Issue:** `dotnet test` for CLI project failed during restore/build without actionable diagnostics under default parallel path.
- **Fix:** Performed explicit restore (including renderer/export references), then used `dotnet msbuild ... /m:1` before filtered `dotnet test --no-build --no-restore`.
- **Files modified:** none (execution/verification only)
- **Verification:** CLI filtered tests pass after serial build path.
- **Committed in:** `520dcc1`

---

**Total deviations:** 1 auto-fixed (blocking)
**Impact on plan:** Verification strategy adjusted for this workspace behavior, with no scope creep and no contract changes.

## Issues Encountered

- Parallel CLI test/build path intermittently fails in this workspace. Serial build then filtered no-build test is stable.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan `18-03` can now layer deterministic report artifacts and diagnostic contracts on top of the new compile path.
- Remaining concern: keep serial verification flow for CLI suite when parallel build exhibits transient failures.

## Verification

- `dotnet test 'tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj' -v minimal --filter "FullyQualifiedName~ScriptCompilerTests"` - passed (`3/3`)
- `dotnet msbuild 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' /t:Build /restore /m:1 /v:minimal` - passed
- `dotnet test 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' --no-build --no-restore -v minimal --filter "FullyQualifiedName~ScriptCompilationOrchestratorTests|FullyQualifiedName~CliCommandParserTests"` - passed (`9/9`)

## Self-Check: PASSED

- Found summary file `.planning/phases/18-script-to-spec-compiler/18-02-SUMMARY.md`.
- Found task commit `0f75ed3`.
- Found task commit `520dcc1`.

---
*Phase: 18-script-to-spec-compiler*
*Completed: 2026-04-04*

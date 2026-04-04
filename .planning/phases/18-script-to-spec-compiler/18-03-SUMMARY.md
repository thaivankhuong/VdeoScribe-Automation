---
phase: 18-script-to-spec-compiler
plan: 03
subsystem: core-compilation
tags: [script-compiler, cli, diagnostics, deterministic-reporting]
requires:
  - phase: 18-02
    provides: deterministic script-to-spec compilation and thin CLI orchestration
provides:
  - Deterministic core compile-report and diagnostic contracts for successful and failed script compiles
  - CLI `script compile --report-output` artifact writing with stable failure diagnostics
  - Fixture-backed report coverage for repeated-run equivalence and ordered diagnostic output
affects: [phase-19, script-compiler-reporting, automation-pipeline]
tech-stack:
  added: []
  patterns: [canonical compile report artifact, stable diagnostic scoping, thin cli report persistence]
key-files:
  created:
    - src/Whiteboard.Core/Compilation/ScriptCompileDiagnostic.cs
    - src/Whiteboard.Core/Compilation/ScriptCompileReport.cs
    - src/Whiteboard.Core/Compilation/ScriptCompileReportSection.cs
    - tests/Whiteboard.Core.Tests/ScriptCompilerReportTests.cs
    - tests/Whiteboard.Cli.Tests/Fixtures/phase18-script-compiler/script-missing-required-field.json
    - tests/Whiteboard.Cli.Tests/Fixtures/phase18-script-compiler/script-unknown-governed-id.json
  modified:
    - src/Whiteboard.Core/Compilation/ScriptCompileResult.cs
    - src/Whiteboard.Core/Compilation/ScriptCompiler.cs
    - src/Whiteboard.Core/Compilation/ScriptMappingPipeline.cs
    - src/Whiteboard.Cli/Models/CliScriptCompileCommandRequest.cs
    - src/Whiteboard.Cli/Services/CliCommandParser.cs
    - src/Whiteboard.Cli/Services/ScriptCompilationOrchestrator.cs
    - src/Whiteboard.Cli/Program.cs
    - tests/Whiteboard.Cli.Tests/CliCommandParserTests.cs
    - tests/Whiteboard.Cli.Tests/ScriptCompilationOrchestratorTests.cs
key-decisions:
  - Keep compile diagnostics and report assembly in Core, and limit CLI work to parsing, file IO, and presentation.
  - Use the spec-output hash as the compile deterministic key on success and the report hash on failure so failed compiles still stay deterministic.
  - Preserve normalized script documents and partial section plans on semantic failure so reports remain auditable without fallback generation.
patterns-established:
  - Every `script compile` run writes a separate report artifact even when spec generation fails.
  - Compile diagnostics carry stable severity/code/path contracts plus section/template scope when it can be derived deterministically.
requirements-completed: [CMP-02]
duration: 31 min
completed: 2026-04-04
---

# Phase 18 Plan 03: Emit deterministic compile reports and stable diagnostic contracts for successful and failed script compiles Summary

**Deterministic script compiles now emit separate audit reports with scoped diagnostics, governed resource usage, and stable CLI report artifacts for both success and failure paths.**

## Performance

- **Duration:** 31 min
- **Started:** 2026-04-04T15:39:00+07:00
- **Completed:** 2026-04-04T16:10:14+07:00
- **Tasks:** 2
- **Files modified:** 15

## Accomplishments

- Added Core compile-report records and ordered diagnostic translation so every compile returns `Report`, `Diagnostics`, `SpecOutputJson`, and deterministic metadata.
- Extended the compiler to keep auditable section/template/governed-resource context on failures and to emit `script.spec.validation.failed` when generated specs fail Core validation.
- Added CLI `--report-output` handling plus fixture-backed tests proving report creation on success/failure and deterministic repeated-run report equivalence.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add Core compile-report records and ordered diagnostic generation** - `5c7dc9b` (`feat`)
2. **Task 2: Extend CLI compile flow to write report artifacts and deterministic failure fixtures** - `3258c2e` (`feat`)

## Files Created/Modified

- `src/Whiteboard.Core/Compilation/ScriptCompileDiagnostic.cs` - stable compile diagnostic contract with deterministic sort order by severity/code/scope/path.
- `src/Whiteboard.Core/Compilation/ScriptCompileReport.cs` - top-level report schema covering `script`, `templates`, `sections`, `governedResources`, `spec`, and `diagnostics`.
- `src/Whiteboard.Core/Compilation/ScriptCompileReportSection.cs` - per-section report payload for resolved slot bindings, governed IDs, and emitted scene/event IDs.
- `src/Whiteboard.Core/Compilation/ScriptCompiler.cs` - report assembly, diagnostic scoping, spec-validation failure reporting, and deterministic failure-key generation.
- `src/Whiteboard.Cli/Services/ScriptCompilationOrchestrator.cs` - report persistence for successful and failed compile runs.
- `src/Whiteboard.Cli/Services/CliCommandParser.cs` - explicit `--report-output` parsing with duplicate/missing flag rejection.
- `tests/Whiteboard.Core.Tests/ScriptCompilerReportTests.cs` - report shape, repeated-run equivalence, and `script.spec.validation.failed` coverage.
- `tests/Whiteboard.Cli.Tests/ScriptCompilationOrchestratorTests.cs` - success/failure report artifact assertions and deterministic failure ordering checks.

## Decisions Made

- Retained compile business semantics in Core and kept the CLI limited to deterministic parse/orchestrate/write behavior.
- Scoped report diagnostics back to script sections/templates when the compiler can derive that scope from ordered section, scene, and timeline mappings.
- Reused the workspace’s known serial CLI build fallback instead of changing project/test architecture for a repo-local build issue.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Switched CLI verification to the documented serial build path**
- **Found during:** Task 2 (Extend CLI compile flow to write report artifacts and deterministic failure fixtures)
- **Issue:** `dotnet test` for `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj` failed during the default restore/build path with only `Determining projects to restore...` surfaced.
- **Fix:** Ran `dotnet msbuild tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj /t:Build /restore /m:1 /v:minimal` and then `dotnet test ... --no-build --no-restore` with the requested test filter.
- **Files modified:** none (verification only)
- **Verification:** filtered CLI parser/orchestrator tests passed after the serial build step
- **Committed in:** `3258c2e`

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Verification flow changed for this workspace only. No scope creep or contract changes.

## Issues Encountered

- The default CLI test path in this workspace still hits an opaque restore/build failure. The serial build plus no-build test path remains reliable.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 19 can consume deterministic compile reports and stable CLI diagnostics without reopening compiler semantics.
- Compile/report artifacts now expose template selection, slot bindings, governed IDs, and validation failures in a machine-readable contract suitable for batch manifests and QA gates.

## Verification

- `dotnet test 'tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj' -v minimal --filter "FullyQualifiedName~ScriptCompilerReportTests"` - passed (`3/3`)
- `dotnet msbuild 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' /t:Build /restore /m:1 /v:minimal` - passed
- `dotnet test 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' --no-build --no-restore -v minimal --filter "FullyQualifiedName~ScriptCompilationOrchestratorTests|FullyQualifiedName~CliCommandParserTests"` - passed (`13/13`)

## Self-Check: PASSED

- Found summary file `.planning/phases/18-script-to-spec-compiler/18-03-SUMMARY.md`.
- Found task commit `5c7dc9b`.
- Found task commit `3258c2e`.

---
*Phase: 18-script-to-spec-compiler*
*Completed: 2026-04-04*

---
phase: 18-script-to-spec-compiler
plan: 01
subsystem: core-compilation
tags: [script-compiler, templates, governed-library, deterministic-validation]
requires:
  - phase: 17-template-contracts-and-scene-composition
    provides: catalog-backed template contracts and deterministic instantiation requests
provides:
  - Repo-governed script compiler mapping catalog and governed library snapshot
  - Core script compilation records and deterministic section ordering contract
  - Script-to-template mapping pipeline with stable validation codes and governed ID checks
affects: [phase-18-plan-02, phase-18-plan-03, script-compiler-cli]
tech-stack:
  added: []
  patterns: [catalog-backed section mapping, governed-id normalization, ordered compilation plans]
key-files:
  created:
    - .planning/script-compiler/template-mappings.json
    - .planning/script-compiler/governed-library.json
    - src/Whiteboard.Core/Compilation/ScriptCompilationDocument.cs
    - src/Whiteboard.Core/Compilation/ScriptSectionDefinition.cs
    - src/Whiteboard.Core/Compilation/ScriptSectionMappingRule.cs
    - src/Whiteboard.Core/Compilation/ScriptTemplateMappingCatalog.cs
    - src/Whiteboard.Core/Compilation/ScriptGovernedLibrary.cs
    - src/Whiteboard.Core/Compilation/ScriptCompilationPlan.cs
    - src/Whiteboard.Core/Compilation/IScriptMappingPipeline.cs
    - src/Whiteboard.Core/Compilation/ScriptMappingPipeline.cs
    - tests/Whiteboard.Core.Tests/ScriptMappingPipelineTests.cs
  modified: []
key-decisions:
  - Keep script compiler mappings repo-governed under `.planning/script-compiler/` and resolve template ids only through `.planning/templates/index.json`.
  - Return ordered section plans with prepared `TemplateInstantiationRequest` payloads so later compile-to-spec work can stay thin and deterministic.
patterns-established:
  - Script sections normalize by `order` then `sectionId` before semantic mapping.
  - Governed asset and effect references are validated before a section becomes an instantiation request.
requirements-completed: [CMP-01]
duration: 8min
completed: 2026-04-04
---

# Phase 18 Plan 01: Define deterministic script input contracts, governed mapping catalogs, and section-to-slot resolution rules Summary

**Deterministic script JSON now resolves ordered sections into governed template instantiation plans through committed mapping and library catalogs.**

## Performance

- **Duration:** 8 min
- **Started:** 2026-04-04T12:07:24+07:00
- **Completed:** 2026-04-04T12:14:58.8700918+07:00
- **Tasks:** 2
- **Files modified:** 12

## Accomplishments

- Added `.planning/script-compiler/template-mappings.json` and `.planning/script-compiler/governed-library.json` as the only committed Phase 18 mapping and governed-reference sources.
- Added Core compilation records for the script document, sections, governed catalogs, and the normalized compilation plan.
- Added `ScriptMappingPipeline` plus focused tests that prove deterministic reordering behavior and stable failures for duplicate sections, unresolved templates, required-field gaps, and unknown governed IDs.

## Task Commits

Each task was committed atomically:

1. **Task 1: Create the repo-governed script compiler catalogs and Core input records** - `3471201` (`feat`)
2. **Task 2: Add deterministic section-to-slot mapping validation in Core** - `c4b312a` (`feat`)

Plan metadata is recorded in the follow-up docs commit for this summary and planning state update.

## Files Created/Modified

- `.planning/script-compiler/template-mappings.json` - committed field-to-slot rules for `title-card-basic`.
- `.planning/script-compiler/governed-library.json` - deterministic snapshot `reg-main-2026-04` with governed asset/effect ids used by Phase 18.
- `src/Whiteboard.Core/Compilation/ScriptCompilationDocument.cs` - exact script input contract with `scriptId`, `projectName`, `assetRegistrySnapshotId`, `output`, and ordered `sections`.
- `src/Whiteboard.Core/Compilation/ScriptCompilationPlan.cs` - normalized plan/result contract carrying ordered section plans and validation gates.
- `src/Whiteboard.Core/Compilation/ScriptMappingPipeline.cs` - contract/schema/normalization/semantic pipeline for catalog-backed script section mapping.
- `tests/Whiteboard.Core.Tests/ScriptMappingPipelineTests.cs` - filtered repeatability and deterministic failure-path coverage.

## Decisions Made

- Reused the existing Core validation-gate pattern so script mapping failures surface through ordered `ValidationGateResult` output instead of ad hoc exceptions.
- Materialized `TemplateInstantiationRequest` payloads inside the compilation plan so the next plan can compile into project specs without recomputing mapping semantics.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Corrected repo-root discovery for `.planning` catalogs**
- **Found during:** Task 2 (Add deterministic section-to-slot mapping validation in Core)
- **Issue:** Relative catalog resolution from a script path outside `.planning/` would miss the repository root when tests or later callers passed script files from other directories.
- **Fix:** Updated `ScriptMappingPipeline` repo-root discovery to detect ancestor directories containing `.planning` before resolving default catalog paths.
- **Files modified:** `src/Whiteboard.Core/Compilation/ScriptMappingPipeline.cs`
- **Verification:** `dotnet test 'tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj' -v minimal --filter "FullyQualifiedName~ScriptMappingPipelineTests"`
- **Committed in:** `c4b312a`

**2. [Rule 1 - Bug] Fixed named-argument usage in the new test fixture record**
- **Found during:** Task 2 (Add deterministic section-to-slot mapping validation in Core)
- **Issue:** The first filtered test run failed to compile because the test used lowercase named constructor arguments against the PascalCase record parameters.
- **Fix:** Updated the failing calls to `SectionInput` to use the correct parameter names.
- **Files modified:** `tests/Whiteboard.Core.Tests/ScriptMappingPipelineTests.cs`
- **Verification:** `dotnet test 'tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj' -v minimal --filter "FullyQualifiedName~ScriptMappingPipelineTests"`
- **Committed in:** `c4b312a`

---

**Total deviations:** 2 auto-fixed (1 blocking, 1 bug)
**Impact on plan:** Both fixes were required to keep the planned pipeline deterministic and executable. No scope creep.

## Issues Encountered

- The first filtered Core test run failed on newly added test named arguments; corrected inline and reran the same suite successfully.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 18-02 can now consume ordered `ScriptSectionCompilationPlan` entries with validated slot bindings and prepared `TemplateInstantiationRequest` payloads.
- Catalog-backed resolution is locked to repo-governed ids, so the next plan can focus on compile-to-spec output and reporting instead of remapping template or governed-reference semantics.

## Verification

- `dotnet test 'tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj' -v minimal --filter "FullyQualifiedName~ScriptMappingPipelineTests"` - passed (`6/6`)

## Self-Check: PASSED

- Found summary file `.planning/phases/18-script-to-spec-compiler/18-01-SUMMARY.md`.
- Found task commit `3471201`.
- Found task commit `c4b312a`.

---
*Phase: 18-script-to-spec-compiler*
*Completed: 2026-04-04*

---
phase: 17-template-contracts-and-scene-composition
plan: 02
subsystem: core-composition
tags: [template-composer, slot-binding, deterministic-instantiation]
requires:
  - phase: 17-01
    provides: normalized template contracts and deterministic single-template validation
provides:
  - Slot-binding validation before composition
  - Deterministic template composition with namespaced IDs and explicit offsets
  - Core repeatability and collision-boundary coverage for template instantiation
affects: [phase-17-plan-03, template-cli-flow, script-compiler]
tech-stack:
  added: []
  patterns: [instance-scoped id namespacing, canonical-json hashing]
key-files:
  created:
    - src/Whiteboard.Core/Templates/TemplateInstantiationRequest.cs
    - src/Whiteboard.Core/Templates/TemplateInstantiationResult.cs
    - src/Whiteboard.Core/Templates/ComposedTemplateFragment.cs
    - src/Whiteboard.Core/Templates/ITemplateSlotBindingValidator.cs
    - src/Whiteboard.Core/Templates/TemplateSlotBindingValidationResult.cs
    - src/Whiteboard.Core/Templates/TemplateSlotBindingValidator.cs
    - src/Whiteboard.Core/Templates/ITemplateComposer.cs
    - src/Whiteboard.Core/Templates/TemplateComposer.cs
    - src/Whiteboard.Core/Templates/TemplateSlotValueResolver.cs
    - tests/Whiteboard.Core.Tests/TemplateComposerTests.cs
  modified: []
key-decisions:
  - Canonical composition output is hashed as `sha256` over deterministic JSON instead of inventing a second ad hoc key format.
  - Slot validation remains a separate Core step so CLI orchestration can reuse the same failure paths without owning composition semantics.
patterns-established:
  - Template instantiation always validates unknown and missing slot bindings before placeholder substitution.
  - Generated scene, object, and event IDs are instance-scoped and collision-checked instead of being auto-rewritten.
requirements-completed: [TMP-01, TMP-02]
duration: 11min
completed: 2026-04-04
---

# Phase 17-02 Summary

**Template contracts now instantiate into deterministic scene and timeline fragments with validated slot bindings, explicit offsets, and hashed canonical output.**

## Performance

- **Duration:** 11 min
- **Started:** 2026-04-04T10:07:25+07:00
- **Completed:** 2026-04-04T10:17:33+07:00
- **Tasks:** 2
- **Files modified:** 11

## Accomplishments

- Added Core instantiation contracts plus a template composer that materializes scenes and timeline events from validated template fragments.
- Added pre-compose slot-binding validation for unknown keys, missing required values, and legal default propagation.
- Added repeatability, offset, and collision tests that lock deterministic canonical JSON and `sha256` deterministic keys.

## Task Commits

Each task was committed atomically:

1. **Task 1: Create the template instantiation models and deterministic composer** - `ffc96ae` (`feat`)
2. **Task 2: Prove deterministic repeatability, offset handling, and collision boundaries in Core tests** - `bcaa1ec` (`test`)

Plan metadata is recorded in the follow-up docs commit for this summary and planning state update.

## Files Created/Modified

- `src/Whiteboard.Core/Templates/TemplateInstantiationRequest.cs` - explicit Core input contract for template, slot bindings, instance id, and offsets.
- `src/Whiteboard.Core/Templates/TemplateSlotBindingValidator.cs` - deterministic validation and default application for operator slot payloads.
- `src/Whiteboard.Core/Templates/TemplateComposer.cs` - slot substitution, namespaced ID generation, offset application, collision detection, and canonical JSON hashing.
- `src/Whiteboard.Core/Templates/TemplateSlotValueResolver.cs` - `{{slot:slotId}}` token resolution with deterministic missing-value failures.
- `tests/Whiteboard.Core.Tests/TemplateComposerTests.cs` - repeatability, offset, unknown-slot, required-slot, and collision coverage.

## Decisions Made

- Used `sha256` of canonical composition JSON for `DeterministicKey` so equivalent instantiations can be compared across Core and future CLI flows.
- Kept `TemplateSlotBindingValidator` separate from `TemplateComposer` to preserve a reusable pre-compose boundary for 17-03 CLI orchestration.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- The first test pass exposed a fixture-path bug in `TemplateComposerTests`; corrected the repo-root traversal and reran the same filtered suite successfully.

## User Setup Required

None.

## Next Phase Readiness

- Core now exposes the validated template instantiation path the CLI can wrap in 17-03 without reimplementing contract or composition semantics.

---
*Phase: 17-template-contracts-and-scene-composition*
*Completed: 2026-04-04*

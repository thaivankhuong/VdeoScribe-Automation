# Phase 20 Context: Deterministic QA Gates and Release Readiness

## Goal

Enforce deterministic regression/witness gates as required pass/fail checks in scripted batch automation, then lock release-readiness evidence for v1.2 closeout.

## Inputs

- Phase 19 deterministic batch orchestration and per-job manifest contracts.
- Existing parity regression baseline shape from `artifacts/source-parity-demo/check/phase15-regression-baseline.json`.
- Current v1.2 requirement `VAL-02` in `.planning/REQUIREMENTS.md`.

## Scope

- Add deterministic baseline gate enforcement in the batch execution path.
- Ensure drift blocks job success deterministically and emits reproducible evidence.
- Add integration evidence that gated runs stay deterministic across repeated executions.

## Out of Scope

- UI/editor workflows.
- Distributed workers or queue orchestration.
- Runtime AI generation or non-deterministic quality scoring.

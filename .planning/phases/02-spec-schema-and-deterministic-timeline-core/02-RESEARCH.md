# Phase 02 Research: Spec Schema Normalization + Deterministic Timeline Core

## Objective
Research implementation approach for Phase 02 to deliver deterministic, schema-driven pre-execution normalization and frame-state core behavior for `TIME-01`, `TIME-02`, and `TIME-03`.

## Scope Guardrails
- Engine-first only. No UI/editor work.
- Determinism is non-negotiable: same valid/equivalent inputs must yield same canonical spec and same frame-state outputs.
- Strict module boundaries: `Core` (contracts/normalization/validation), `Engine` (timeline/frame-state resolution), `CLI` (orchestration only).

## Current-State Assessment

### What Exists
- Phase-1 architecture policies are defined and coherent:
  - schema versioning (`major.minor`) and compatibility requirements;
  - ordered gates (`contract -> schema -> normalization -> semantic -> readiness`);
  - deterministic validation payload contract;
  - determinism invariants and verification method.
- Core contracts are present and serializable (`VideoProjectContractTests`).
- Engine has resolver skeletons with deterministic-friendly shape:
  - `FrameContext` converts frame index to time;
  - `TimelineResolver` computes active events with `[start, end)` behavior;
  - `FrameStateResolver` orchestrates timeline/object/camera resolvers.
- CLI can load/deserialize JSON spec with placeholder normalization (`ProjectSpecLoader`).

### Gaps vs Phase-2 Goals
- No explicit validation pipeline implementation for the 5 gates yet.
- CLI loader currently performs permissive fallback defaults (e.g., project metadata) without contract-level gate/error semantics.
- No canonical normalization artifact/pipeline for stable ordering/default precedence/reference resolution.
- Event ordering/tie-break policy from architecture docs is not enforced in resolver input preparation.
- Object lifecycle (`enter/draw/hold/exit`) is not modeled; `ObjectStateResolver` currently uses a coarse reveal-active rule.
- Deterministic tests exist but are contract-smoke style; missing fixture-driven parity and boundary tests mapped directly to `TIME-01/02/03`.

## Risks
1. Determinism drift from implicit collection ordering.
- Risk: relying on source insertion/order or runtime iteration may cause non-repeatable event/object resolution.
- Mitigation: canonicalize in `Core` before `Engine` and ensure resolvers consume ordered immutable lists.

2. Contract bypass through CLI fallback normalization.
- Risk: invalid specs could be "repaired" silently, violating gate semantics and making behavior environment-dependent.
- Mitigation: move gate execution to `Core` services and make CLI fail fast with ordered validation payload.

3. Boundary ambiguity in time-to-frame mapping.
- Risk: off-by-one behavior near event boundaries can destabilize `TIME-01` and lifecycle transitions.
- Mitigation: codify frame conversion and `[inclusive start, exclusive end)` activation in one policy service with boundary tests.

4. Lifecycle ambiguity under overlapping events.
- Risk: multiple concurrent actions for one object without precedence can yield non-deterministic states.
- Mitigation: define explicit lifecycle precedence and tie-break rules over normalized ordered events.

5. Test blind spots for equivalent-input parity.
- Risk: deterministic behavior appears stable for one fixture but fails for equivalent representations.
- Mitigation: introduce paired fixtures (different ordering/shape, equivalent meaning) and assert canonical+frame-state equivalence.

## Validation Architecture
Recommended architecture for Phase 02 implementation:

1. `Core` introduces a spec processing pipeline with explicit stages:
- `IContractValidator`
- `ISchemaValidator`
- `ISpecNormalizer`
- `ISemanticValidator`
- `IReadinessValidator`
- `ISpecProcessingPipeline` orchestrating fixed gate order and downstream blocking.

2. Shared deterministic artifacts in `Core`:
- `NormalizedVideoProject` (or equivalent canonical model contract);
- `ValidationIssue` payload aligned with `docs/architecture/12-validation-error-contract.md`;
- deterministic issue ordering comparer (gate, path, severity, code, occurrence).

3. `CLI` integration policy:
- `ProjectSpecLoader` loads raw JSON only;
- invokes `ISpecProcessingPipeline`;
- returns canonical model on success or deterministic ordered validation payload on failure.
- no fallback mutation in CLI after pipeline.

4. `Engine` consumption policy:
- `FrameStateResolver` and sub-resolvers consume canonical, ordered timeline/object inputs only;
- no ad hoc sorting or reference fixing inside Engine.

## Recommended Plan Decomposition (3 Plans)

### Plan 02-01: Deterministic Validation + Normalization Pipeline (Core)
Goal:
- Implement gate-ordered spec pipeline and canonical normalization contract.

Deliverables:
- Pipeline interfaces/services in `Whiteboard.Core`.
- Gate implementations for contract/schema/normalization/semantic/readiness.
- Deterministic `ValidationIssue` model + ordering.
- Canonical normalization rules for defaults, identifier/reference resolution, and collection ordering (including timeline tie-break sequence).
- Fixture inputs and canonical expected outputs for normalization.

Done signals:
- Same invalid fixture always emits identical ordered issues.
- Equivalent valid fixtures normalize to byte-equivalent canonical JSON (or equivalent deterministic representation).

### Plan 02-02: Timeline + Lifecycle Deterministic Resolution Core (Engine)
Goal:
- Enforce deterministic frame-based event activation and object lifecycle evaluation.

Deliverables:
- Explicit time-to-frame conversion policy utility shared by timeline/lifecycle resolvers.
- Timeline event preparation consumes normalized sorted events only.
- Lifecycle state model (`enter`, `draw`, `hold`, `exit`) with documented precedence and overlap handling.
- `ObjectStateResolver` updated to use lifecycle semantics, not only binary reveal-active.
- `FrameStateResolver` contract updated only as needed to carry deterministic lifecycle outputs.

Done signals:
- Boundary frames and overlap scenarios resolve identically across repeated runs.
- Equivalent normalized specs produce equivalent resolved frame-state structures.

### Plan 02-03: Determinism Verification Harness + Phase Acceptance
Goal:
- Lock deterministic behavior with targeted tests and evidence artifacts mapped to requirements.

Deliverables:
- Fixture-based tests for gate ordering, error ordering, normalization parity, timeline boundary behavior, overlap/tie-break handling, lifecycle transitions.
- Snapshot/serialization assertions for canonical normalized outputs and resolved frame states.
- Requirement trace updates for `TIME-01/02/03` evidence.

Done signals:
- Determinism-focused tests pass consistently across repeated local runs.
- Phase 02 acceptance checklist can be audited without relying on implementation intuition.

## Test Strategy Mapping to TIME-01/02/03

### TIME-01: Timeline events convert to frame indices deterministically
Test set:
- FPS boundary conversion tests (`frameIndex`, `currentTimeSeconds`, start/end inclusion rules).
- Event activation window tests using `[start, end)` at exact boundary frames.
- Equivalent-input parity tests: reordered but equivalent timeline definitions must produce same active-event set per frame.

Primary assertions:
- Active/inactive results identical across repeat runs.
- Deterministic ordering of active events when start frame ties occur.

### TIME-02: Object lifecycle states resolved per frame from timeline + prior state
Test set:
- Lifecycle transition sequence tests (`enter -> draw -> hold -> exit`) across frame progression.
- Overlap precedence tests for concurrent actions on same object.
- Prior-state dependency tests to ensure state transitions are deterministic and history-consistent.

Primary assertions:
- Lifecycle state progression is stable and reproducible.
- Equivalent normalized specs yield identical per-object lifecycle outputs for each frame.

### TIME-03: Event ordering and overlap handling stable across repeated runs
Test set:
- Tie-break tests for equal `start`, equal `duration`, category precedence, `targetId`, `eventId`.
- Multi-event overlap fixtures across scenes/objects.
- Repeat-run snapshots of resolved timeline ordering and frame-state outputs.

Primary assertions:
- Canonical event order matches documented rule stack exactly.
- No ordering drift between runs or equivalent fixture representations.

## Acceptance Signals
- Validation pipeline enforces gate order and downstream blocking exactly as architecture docs specify.
- Error payloads match contract shape and deterministic ordering keys.
- Canonical normalization exists, is deterministic, and removes representation ambiguity before Engine evaluation.
- Engine resolvers operate only on canonical normalized inputs and preserve deterministic ordering/lifecycle policies.
- `TIME-01`, `TIME-02`, `TIME-03` each have explicit, passing, fixture-based deterministic tests.
- No UI/editor artifacts introduced; no cross-module boundary violations (`CLI` does not embed domain logic, `Engine` does not own normalization/validation).

## Recommended Implementation Notes for Planning Quality
- Treat Plan 02-01 as a hard prerequisite for Plan 02-02 to avoid building Engine behavior on unstable input semantics.
- Keep policy objects explicit and small (ordering comparer, lifecycle precedence rules, frame-boundary rules) so tests can target policies directly.
- Prefer immutable or effectively immutable normalized collections to reduce accidental post-normalization mutation.
- Introduce canonical serialization helper early (even internal) to make determinism evidence cheap and repeatable.

# Phase 1 Research: Bootstrap and Architecture Baseline

## Scope and Intent
Phase 1 establishes architecture and contract baselines only. This phase must satisfy `SPEC-01`, `SPEC-02`, and `SPEC-03` through documentation, explicit boundaries, and validation design, without implementation code.

Hard constraints for all planning decisions:
- JSON spec is the single source of truth.
- Deterministic behavior is non-negotiable.
- Module boundaries (`Core`, `Engine`, `Renderer`, `Export`, `CLI`) are strict and explicit.
- UI/editor work is out of scope.

## Requirement-Driven Planning Guidance

### SPEC-01: Spec JSON as source of truth
Actionable guidance:
- Define a canonical spec contract document that owns timeline, scene, camera, assets, and output settings.
- Document field ownership by module so no module infers missing domain behavior implicitly.
- Specify which module reads raw JSON and where normalized spec becomes the internal handoff artifact.
- Establish a "no hardcoded scene logic" review rule in planning checklists.

Phase 1 planning outputs:
- Module contract matrix for JSON ingestion and handoff boundaries.
- Spec ownership map by top-level section (`meta`, `assets`, `scene`, `timeline`, `output`).

### SPEC-02: Versioning and normalization before execution
Actionable guidance:
- Define schema version policy (`major.minor` compatibility rules, deprecation approach, upgrade path).
- Define normalization stages and order (defaults, ranges, reference resolution, event ordering).
- Define fail-fast gates: no timeline/frame evaluation before schema + normalization pass succeeds.
- Define deterministic normalization rules for ambiguous cases (ordering ties, missing optional values, default injection).

Phase 1 planning outputs:
- Schema/versioning strategy document.
- Normalization contract with explicit stage sequence and deterministic tie-break rules.

### SPEC-03: Explicit validation errors for invalid/inconsistent data
Actionable guidance:
- Define validation error taxonomy (schema, semantic, cross-reference, timeline consistency, asset reference).
- Define error payload contract (code, message, path, offending value, recovery hint, severity).
- Define reporting behavior for single vs multiple errors and deterministic error ordering.
- Define boundary of responsibility: validation in `Core/Engine`, presentation/orchestration in `CLI`.

Phase 1 planning outputs:
- Validation and error-reporting spec with deterministic ordering rules.
- Acceptance criteria examples for representative invalid specs.

## Phase 1 Plan Breakdown (Execution-Oriented, Doc-First)
1. `01-01 Finalize module contracts and dependency boundaries`
- Produce module dependency diagram and allowed dependency table.
- Define input/output contracts per module and forbidden coupling examples.
- Add architecture decision record (ADR) for dependency direction and anti-leak rules.

2. `01-02 Define JSON spec schema/versioning and validation strategy`
- Produce spec schema baseline (documented shape, required/optional semantics, version rules).
- Produce normalization pipeline spec and pre-execution validation gates.
- Produce error taxonomy and deterministic reporting contract.

3. `01-03 Define deterministic evaluation and verification strategy`
- Define deterministic invariants for parse/normalize/validate outputs.
- Define deterministic test strategy for future phases (golden inputs, checksum/hash policy for intermediate artifacts, repeat-run checks).
- Define phase exit checklist mapping each invariant to a verification step.

## Risks and Mitigations
- Risk: Scope creep into implementation or UI discussion.
Mitigation: Enforce doc-only deliverables and explicit non-goal checklist in every plan review.

- Risk: Module boundary ambiguity causing future coupling.
Mitigation: Maintain a dependency allowlist/denylist and review against each planned artifact.

- Risk: Non-deterministic behavior introduced in normalization or validation ordering.
Mitigation: Specify deterministic ordering rules now (stable sort keys, tie-breakers, error emission order).

- Risk: Schema evolution breaking compatibility too early.
Mitigation: Define version compatibility rules and migration expectations before implementation begins.

- Risk: Validation contract too vague for CLI/operator use.
Mitigation: Require structured error payload shape with mandatory fields and examples.

## Required Artifacts for Phase 1
- `Phase 1 ADR set`:
  - Module boundaries and dependency direction ADR.
  - Determinism invariants ADR.
  - Spec-driven governance ADR (no hardcoded scene logic).
- `Architecture baseline docs`:
  - Module contract matrix.
  - Data-flow and handoff diagrams (spec input -> normalization -> validated normalized model).
- `Spec governance docs`:
  - Schema/versioning policy.
  - Normalization stage contract.
  - Validation/error taxonomy and payload format.
- `Verification docs`:
  - Deterministic verification strategy.
  - Phase 1 exit checklist mapped to `SPEC-01/02/03`.

## Validation Architecture
Validation architecture for Phase 1 must be defined as layered gates with deterministic outputs:

1. `Contract Gate (Pre-Parse Governance)`
- Confirms input is a project/spec JSON envelope with declared `schemaVersion`.
- Rejects missing mandatory top-level sections before deeper evaluation.

2. `Schema Gate (Structural Validity)`
- Validates required fields, type constraints, and allowable enums/ranges.
- Emits structured, ordered validation errors using canonical path notation.

3. `Normalization Gate (Deterministic Canonicalization)`
- Applies defaults, resolves references, and canonicalizes ordering.
- Produces a normalized spec artifact that is deterministic for equivalent inputs.

4. `Semantic Consistency Gate`
- Validates cross-entity consistency (target references, timeline integrity, camera references, asset references).
- Ensures no unresolved dependency proceeds to timeline execution design.

5. `Execution Readiness Gate`
- A binary gate: only normalized + semantically valid specs proceed to later timeline/frame planning.
- Captures validation summary metadata for reproducibility and auditability.

Validation architecture constraints:
- Error ordering must be deterministic.
- Gate outputs must be contract-defined and testable.
- No module outside designated validation ownership may silently coerce invalid data.

## Phase 1 Exit Criteria (Research-Derived)
- `SPEC-01`: Spec JSON ownership and module handoff contracts are documented and agreed.
- `SPEC-02`: Versioning + normalization rules are defined with deterministic behavior and pre-execution gating.
- `SPEC-03`: Validation/error model is explicit, structured, and deterministic in ordering and format.
- All artifacts above are complete and reviewed without implementation code.

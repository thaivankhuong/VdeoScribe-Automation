# Verification Strategy

## Purpose
Define how Phase 1 architecture artifacts are verified so deterministic constraints, contract gates, and requirement coverage can be audited before any engine implementation work begins.

## Verification Principles
- Verification in this phase is documentation-first and contract-first.
- Every verification activity must trace back to `SPEC-01`, `SPEC-02`, or `SPEC-03`.
- Verification must prove that deterministic behavior is enforceable, not merely stated.
- Evidence must come from architecture documents, checklists, and future canonical artifacts, not from ad hoc reviewer interpretation.

## Verification Layers

### 1. Architecture Document Presence
Confirm that the required contract documents exist and are stored at stable repository paths.

Required artifacts:
- `docs/architecture/07-module-contract-matrix.md`
- `docs/architecture/08-dependency-boundaries-adr.md`
- `docs/architecture/09-spec-ownership-map.md`
- `docs/architecture/10-spec-schema-versioning-policy.md`
- `docs/architecture/11-normalization-gates.md`
- `docs/architecture/12-validation-error-contract.md`
- `docs/architecture/13-determinism-invariants.md`
- `docs/architecture/14-verification-strategy.md`
- `.planning/phases/01-bootstrap-and-architecture-baseline/01-CHECKLIST.md`

### 2. Contract Consistency Checks
Confirm the docs describe one coherent pre-execution contract.

Required checks:
- version policy is evaluated before schema, normalization, or execution readiness work;
- normalization gates block downstream work when earlier gates fail;
- validation ordering keys in `12-validation-error-contract.md` match the invariants in `13-determinism-invariants.md`;
- repeat-run/checksum guidance refers to canonical serialization, not implementation-defined object ordering;
- evidence mappings in `01-CHECKLIST.md` reference documentation artifacts only for this phase.

### 3. Requirement Traceability
Confirm each Phase 1 requirement maps to specific evidence.

Traceability rules:
- `SPEC-01` must map to module contracts, schema/version policy, and deterministic accepted-spec invariants.
- `SPEC-02` must map to normalization gates, canonical ordering rules, and repeat-run verification evidence.
- `SPEC-03` must map to validation payload contract, deterministic ordering rules, and checklist evidence.
- Every requirement must identify at least one primary doc and one supporting doc or checklist entry.

### 4. Repeat-Run Readiness
Confirm the architecture defines how future automation will prove repeatability.

Required checks:
- accepted outputs are represented as canonical serialized artifacts;
- rejected outputs are represented as ordered validation payloads;
- checksum guidance names an algorithm and clarifies the bytes to be hashed;
- any output mismatch is explicitly treated as a contract failure, not a flaky test.

## Automated Verification Approach
These checks are expected to run as repository commands once implementation begins, but their targets are defined now.

### Phase 1 Document Checks
- Verify all expected docs exist.
- Search for required contract vocabulary:
  - `deterministic`
  - `invariant`
  - `repeat`
  - `hash`
  - `checksum`
  - `SPEC-01`
  - `SPEC-02`
  - `SPEC-03`
  - `non-goal`

### Future Contract-Gate Automation
Once execution code exists, automation should add:
- fixture-based canonical output comparisons for accepted specs;
- fixture-based ordered validation snapshot comparisons for rejected specs;
- checksum assertions over canonical serialized outputs;
- regression checks ensuring gate ordering and downstream blocking rules remain unchanged.

## Manual Verification Approach

### Reviewer Checklist
1. Confirm plans `01-01` and `01-02` artifacts exist before approving `01-03`.
2. Read `13-determinism-invariants.md` and verify parse, normalize, and validate each define measurable outputs.
3. Read `14-verification-strategy.md` and verify every layer includes auditable evidence.
4. Read `01-CHECKLIST.md` and confirm each success criterion and requirement points to concrete artifacts.
5. Reject approval if any doc implies editor UI, rendering implementation, export implementation, or business logic work in Phase 1.

### Audit Questions
- Can two processors implementing the same contract line independently derive the same accepted artifact and rejected payload expectations from these docs?
- Do the docs make checksum scope explicit enough to avoid hashing unstable internal object representations?
- Is every documented fallback deterministic, or does the contract correctly reject ambiguity?
- Does any verification step rely on reviewer intuition instead of named evidence artifacts?

## Evidence Model

| Evidence Type | Purpose | Phase 1 Source |
| --- | --- | --- |
| Contract document | Defines authoritative rule | `07` through `14` architecture docs |
| Checklist entry | Maps requirement/success criterion to proof | `01-CHECKLIST.md` |
| Future canonical artifact | Demonstrates accepted-spec repeatability | Referenced by `13-determinism-invariants.md` |
| Future ordered error payload | Demonstrates rejected-spec repeatability | Referenced by `12-validation-error-contract.md` and `13-determinism-invariants.md` |
| Future checksum record | Confirms repeat-run equality | Referenced by `13-determinism-invariants.md` |

## Exit Gates for Phase 1
Phase 1 architecture is verification-ready only when all of the following are true:
- all required architecture docs and the checklist exist;
- deterministic invariants are explicit and measurable;
- repeat-run verification method is documented for accepted and rejected artifacts;
- requirements `SPEC-01`, `SPEC-02`, and `SPEC-03` each map to evidence in the checklist;
- non-goals explicitly reject UI/editor scope, rendering/business implementation, and contract bypass behavior.

## Non-Goals
- This strategy does not add executable verification code in Phase 1.
- This strategy does not define renderer correctness, export fidelity, or runtime performance tests.
- This strategy does not allow contract compliance to be inferred from manual intuition alone.

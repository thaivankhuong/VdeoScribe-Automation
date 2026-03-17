# Phase 01 Exit Checklist

## Purpose
Provide an auditable checklist that maps Phase 1 success criteria and `SPEC-01`, `SPEC-02`, and `SPEC-03` to concrete evidence artifacts before implementation begins.

## Phase 1 Success Criteria

| Success Criterion | Evidence Artifact | Proof Expectation | Status |
| --- | --- | --- | --- |
| Engine-first architecture is explicit | `docs/architecture/07-module-contract-matrix.md` | Core, Engine, Renderer, Export, and CLI responsibilities are separated with forbidden couplings documented | Pending review |
| JSON/project spec is the only source of scene intent | `docs/architecture/09-spec-ownership-map.md` | Ownership map shows scene, timeline, output, and assets come from project/spec JSON contracts | Pending review |
| Dependency direction is clean and enforceable | `docs/architecture/08-dependency-boundaries-adr.md` | ADR forbids reverse dependencies and reinterpretation by downstream modules | Pending review |
| Pre-execution contract gates are fixed before implementation | `docs/architecture/10-spec-schema-versioning-policy.md`, `docs/architecture/11-normalization-gates.md` | Version checks and five-stage gate sequencing are documented and ordered | Pending review |
| Deterministic output expectations are measurable | `docs/architecture/13-determinism-invariants.md` | Parse, normalize, and validate stages define observable, repeatable outputs | Pending review |
| Verification is repeatable and auditable | `docs/architecture/14-verification-strategy.md` | Verification layers, evidence model, and exit gates are explicitly documented | Pending review |

## Requirement Traceability

| Requirement | Primary Evidence | Supporting Evidence | What Must Be True |
| --- | --- | --- | --- |
| `SPEC-01` | `docs/architecture/07-module-contract-matrix.md` | `docs/architecture/10-spec-schema-versioning-policy.md`, `docs/architecture/13-determinism-invariants.md` | Contract ownership, accepted version handling, and canonical accepted-spec outputs are explicit before execution work |
| `SPEC-02` | `docs/architecture/11-normalization-gates.md` | `docs/architecture/09-spec-ownership-map.md`, `docs/architecture/13-determinism-invariants.md` | Canonical ordering, default precedence, and ambiguity rejection are documented as contract behavior |
| `SPEC-03` | `docs/architecture/12-validation-error-contract.md` | `docs/architecture/13-determinism-invariants.md`, `docs/architecture/14-verification-strategy.md` | Validation payloads are structured, ordered, traceable, and repeatable across runs |

## Repeat-Run Verification Readiness

| Check | Evidence Artifact | Audit Question | Status |
| --- | --- | --- | --- |
| Accepted outputs use canonical serialization | `docs/architecture/13-determinism-invariants.md` | Does the doc require one canonical artifact for the same valid input? | Pending review |
| Rejected outputs use ordered validation payloads | `docs/architecture/12-validation-error-contract.md`, `docs/architecture/13-determinism-invariants.md` | Does the doc forbid unstable or parser-discovery-dependent error ordering? | Pending review |
| Checksum guidance is explicit | `docs/architecture/13-determinism-invariants.md`, `docs/architecture/14-verification-strategy.md` | Is checksum scope tied to serialized bytes and not internal runtime order? | Pending review |
| Repeat-run mismatches fail the contract | `docs/architecture/13-determinism-invariants.md` | Does any output mismatch block deterministic readiness? | Pending review |

## Non-Goals and Rejection Criteria

| Non-Goal | Rejection Trigger | Evidence Artifact |
| --- | --- | --- |
| No editor UI scope | Any Phase 1 doc proposes authoring/editor screens, interactions, or workflow implementation | `docs/architecture/01-project-scope.md`, `docs/architecture/14-verification-strategy.md` |
| No rendering or business-logic implementation | Any Phase 1 artifact adds renderer behavior, export commands, or runtime logic beyond contracts | `docs/architecture/02-architecture.md`, `docs/architecture/13-determinism-invariants.md` |
| No contract bypass | Any doc allows invalid specs to continue via best-effort fallback or implicit guessing | `docs/architecture/11-normalization-gates.md`, `docs/architecture/12-validation-error-contract.md`, `docs/architecture/13-determinism-invariants.md` |

## Exit Review Questions
- Do all evidence artifacts remain documentation-only in this phase?
- Can a reviewer trace each success criterion to at least one concrete file?
- Can a reviewer trace `SPEC-01`, `SPEC-02`, and `SPEC-03` without inferring unstated behavior?
- Do the docs define repeat-run evidence for both accepted and rejected inputs?
- Do the docs explicitly reject UI/editor work, rendering/business implementation, and contract bypass?

| Repeat-run evidence record fields are predefined | `docs/architecture/13-determinism-invariants.md` | Can future automation capture fixture id, artifact type, checksum, and match result without redefining the contract? | Pending review |

## Approval Blockers
- Reject Phase 1 completion if any new artifact introduces editor UI workflows, rendering logic, export orchestration, or business rules beyond contract definitions.
- Reject Phase 1 completion if any artifact permits best-effort execution after contract, schema, normalization, semantic, or readiness failures.
- Reject Phase 1 completion if requirement evidence depends on runtime code that does not exist in this phase.

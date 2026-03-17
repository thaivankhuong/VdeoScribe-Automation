# Determinism Invariants

## Purpose
Define the measurable invariants that future parsers, normalizers, and validators must preserve so the same project/spec input always yields the same canonical output or the same ordered validation result.

## Scope
- This document applies only to pre-execution contract handling.
- It governs parse, normalize, and validate stages described in `10-spec-schema-versioning-policy.md`, `11-normalization-gates.md`, and `12-validation-error-contract.md`.
- It does not introduce runtime rendering, export, editor, or business-logic behavior.

## Core Rule
For any supported `meta.schemaVersion`, identical input bytes and identical referenced contract documents must produce one of two outcomes:
- the same canonical spec representation; or
- the same validation payload with the same item ordering.

No processor may rely on platform-dependent iteration order, locale-sensitive comparison, wall-clock time, random seeds, or ambient machine state to choose between valid outcomes.

## Invariant Categories

### 1. Parse Invariants
Parse converts raw JSON bytes into the contract-recognized document model.

#### Required Invariants
- The same UTF-8 JSON document must always produce the same parsed value graph before normalization begins.
- Property names must be interpreted exactly as written; no locale or case folding may be applied unless a future schema explicitly adds that rule.
- Numeric values must preserve the exact JSON number meaning accepted by the supported contract line.
- Duplicate-key handling, if forbidden by the parser contract, must fail deterministically with the same contract or schema error code and canonical path.
- Input decoding failures must stop processing before normalization or semantic checks begin.

#### Measurable Evidence
- Parse acceptance or rejection result.
- Parsed top-level shape classification (`object`, `array`, scalar) used by the contract gate.
- Canonical error location for malformed JSON or duplicate-key rejection.

### 2. Normalization Invariants
Normalization converts a schema-valid document into the single canonical representation consumed by later phases.

#### Required Invariants
- The same schema-valid input must always produce the same canonical field values, collection ordering, and default application results.
- Defaults must be applied in the same precedence order every run.
- Equivalent authoring shapes that are allowed by the contract must normalize to the same canonical structure.
- Canonical collection order must follow the ordering rules already defined for identifiers, property names, and timeline events.
- Normalization must never invent scene intent, execution timing, or target references that were not implied by the supported contract.

#### Canonicalization Targets
- Top-level section presence after default expansion.
- Stable ordering for `scene.objects`, `timeline.events`, and any future keyed collections.
- Stable string, enum, and identifier preservation after normalization.
- Stable omission or inclusion of optional fields once defaults are resolved.

#### Measurable Evidence
- A canonical JSON artifact produced from the normalized representation.
- A repeatable checksum of that canonical artifact.
- A documented list of ordering keys and default-precedence rules used to derive the artifact.

### 3. Validation Invariants
Validation emits the contract payload when a document fails any pre-execution gate.

#### Required Invariants
- The same invalid input must always yield the same set of validation items.
- Validation items must be ordered exactly by gate, path, severity, code, and canonical occurrence.
- A downstream gate must never emit items when an upstream gate failure prevented that gate from running.
- Warning presence must be deterministic and must not suppress blocking errors.
- Error messages may be human-readable, but machine-facing fields (`code`, `path`, `severity`) are the authority for repeatability.

#### Measurable Evidence
- Stable error item count for a given invalid fixture.
- Stable serialized validation payload for a given invalid fixture.
- Stable per-item ordering keys traceable back to the contract docs.

## Outcome Matrix

| Stage | Stable Input | Stable Output | Rejection Condition |
| --- | --- | --- | --- |
| Parse | Raw JSON bytes | Parsed document model or deterministic parse failure | Malformed JSON, unsupported top-level form, forbidden duplicate key |
| Normalize | Schema-valid parsed document | Canonical spec artifact | Ambiguous defaults, ambiguous canonicalization, forbidden nondeterministic tie |
| Validate | Contract candidate or normalized spec | Ordered validation payload | Any gate failure defined by the contract |

## Repeat-Run Evaluation Method

### Document-Level Method
1. Select a reference input fixture for each gate outcome: accepted canonical spec, contract failure, schema failure, normalization failure, semantic failure, and readiness failure.
2. Process the same fixture at least two times in the same supported environment and compare outputs.
3. Serialize accepted specs into the canonical JSON artifact defined by the normalization contract.
4. Serialize rejected specs into the ordered validation payload defined by the validation contract.
5. Compare both the serialized text and a checksum captured from that text.

### Checksum Guidance
- Checksums are evidence artifacts, not execution features, in this phase.
- Future automation should hash the exact canonical serialization bytes of:
  - normalized accepted specs; and
  - ordered validation payloads for rejected specs.
- SHA-256 is the recommended checksum algorithm for future evidence capture because it is stable, common, and deterministic across platforms.
- Checksums must be generated after canonical serialization, never from in-memory object order that might vary by runtime.

### Pass Criteria
- Text output matches exactly across repeat runs.
- Checksum matches exactly across repeat runs.
- Any mismatch is a determinism contract failure and blocks execution readiness for the affected processor behavior.

## Evidence Expectations by Requirement

### SPEC-01
- Evidence that the same spec input always resolves to one canonical normalized representation.
- Evidence that contract ownership and versioning checks happen before execution behavior is considered.

### SPEC-02
- Evidence that normalization uses explicit ordering and default-precedence rules rather than implementation-defined behavior.
- Evidence that ambiguous inputs are rejected instead of guessed.

### SPEC-03
- Evidence that validation payloads are machine-readable, ordered, and repeatable across runs.
- Evidence that failure artifacts can be snapshot-tested by future CI without nondeterministic noise.

## Non-Goals
- No editor or authoring UI behavior is defined here.
- No rendering, export, or business-logic implementation is defined here.
- No processor is allowed to bypass contract gates to "best effort" execute an invalid spec.

## Review Checklist
- Does each invariant describe an observable outcome rather than an implementation detail?
- Does each accepted-spec rule lead to one canonical artifact only?
- Do rejection rules forbid hidden fallback behavior and implicit guessing?
- Are repeat-run comparisons defined in terms of canonical serialization and checksum evidence?
- Are SPEC-01, SPEC-02, and SPEC-03 covered without adding implementation code?

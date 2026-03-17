# Normalization Gates

## Purpose
Define the ordered gates a spec must pass before execution so validation, normalization, and readiness decisions happen predictably and before any timeline or rendering work begins.

## Gate Principles
- Gates run in a fixed sequence and must not be reordered.
- A later gate must not run if an earlier gate fails.
- Every accepted spec must pass all gates before timeline evaluation begins.
- Gate outputs are contract artifacts, not implementation-specific side effects.

## Gate Sequence
1. Contract gate
2. Schema gate
3. Normalization gate
4. Semantic consistency gate
5. Execution readiness gate

## 1. Contract Gate
Purpose:
- Confirm the input is a project/spec document governed by this engine contract.
- Confirm required contract metadata exists before structural parsing deepens.

Checks:
- top-level document is a JSON object;
- `meta` exists;
- `meta.schemaVersion` exists and follows the supported contract policy;
- required contract identifiers needed for traceability are present.

Reject if:
- the document is not a valid JSON object;
- required contract metadata is missing;
- the declared schema version is malformed or unsupported.

Output:
- the document is eligible for schema-level validation;
- contract-level failures are recorded without starting execution work.

## 2. Schema Gate
Purpose:
- Validate the declared document shape against the supported schema line.

Checks:
- required sections exist;
- field types match the schema contract;
- collection members match the declared object shapes;
- enums and literals are constrained to documented values.

Reject if:
- required fields are missing;
- fields have invalid types;
- object shapes or literal values fall outside the contract.

Output:
- a structurally valid document ready for canonical normalization.

## 3. Normalization Gate
Purpose:
- Convert a schema-valid document into the canonical form consumed by later engine phases.

Responsibilities:
- apply documented defaults;
- canonicalize optional shapes into one stable representation;
- normalize identifiers, references, and collection forms;
- preserve declared meaning while removing representation ambiguity.

Requirements:
- normalization must be deterministic;
- normalization must not invent new scene or timeline intent;
- normalization must complete before semantic consistency checks or execution planning.

Output:
- one canonical spec representation for the supported compatibility line.

## 4. Semantic Consistency Gate
Purpose:
- Verify the normalized spec is logically coherent and can be evaluated without ambiguity.

Checks:
- references point to existing targets;
- identifiers are unique where uniqueness is required;
- time spans, layers, and camera directives do not violate documented invariants;
- normalized sections remain internally consistent across assets, scene, timeline, and output blocks.

Reject if:
- cross-reference resolution fails;
- duplicate identities create ambiguous targeting;
- semantic contradictions would force guesswork during evaluation.

Output:
- a logically coherent normalized spec suitable for execution planning.

## 5. Execution Readiness Gate
Purpose:
- Confirm the spec is fully prepared for deterministic engine execution.

Checks:
- all earlier gates succeeded;
- required execution-facing data is present after normalization;
- no remaining placeholders, unresolved references, or contract-level warnings require author action before execution.

Reject if:
- the engine would need implicit assumptions to continue;
- required normalized values for deterministic timeline/frame evaluation are still absent.

Output:
- the spec may enter timeline-to-frame evaluation and later renderer/export phases.

## Failure Handling Rules
- A gate failure blocks all downstream gates.
- Validation output must identify the gate where a failure was detected.
- Failures are contract artifacts and must be returned using the validation error contract.
- Passing a gate does not suppress independent failures discovered in the same gate.

## Relationship to Later Phases
- Timeline evaluation begins only after execution readiness succeeds.
- Renderer and export work must consume only execution-ready specs.
- Any pre-execution upgrade path must complete inside the versioning and normalization policy, never during rendering or export.

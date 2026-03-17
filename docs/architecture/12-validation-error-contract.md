# Validation Error Contract

## Purpose
Define the machine-readable validation contract returned when a spec fails contract, schema, normalization, semantic consistency, or execution-readiness checks.

## Design Goals
- Errors must be explicit, structured, and deterministic.
- The same invalid spec must produce the same error payload ordering.
- Error output must help both humans and automated tooling identify the failing rule and affected location.
- Validation errors are contract artifacts only. They must not trigger implicit fixes during execution.

## Error Taxonomy

### Contract Errors
Use for failures that prevent the document from being recognized as a supported engine spec.

Examples:
- missing `meta` section;
- missing or malformed `meta.schemaVersion`;
- unsupported `major` compatibility line.

### Schema Errors
Use for structural violations against the declared schema line.

Examples:
- missing required field;
- wrong field type;
- unsupported enum or literal;
- malformed collection member shape.

### Normalization Errors
Use for failures encountered while producing the canonical spec representation.

Examples:
- conflicting defaults for the same field;
- ambiguous canonicalization input;
- non-deterministic representation that cannot be normalized safely.

### Semantic Consistency Errors
Use for logically invalid normalized specs.

Examples:
- unresolved reference;
- duplicate identifier where uniqueness is required;
- contradictory timeline directives;
- output settings incompatible with the documented contract.

### Execution Readiness Errors
Use when the spec passed prior gates but still cannot enter deterministic evaluation.

Examples:
- required normalized execution field remains absent;
- unresolved placeholder remains after normalization;
- execution would require an implicit assumption not allowed by contract.

## Severity Levels
- `error`: blocks the current spec from proceeding to the next gate.
- `warning`: communicates a non-blocking issue or deprecation notice that remains deterministic and does not change execution semantics.

Warnings must never be used to hide blocking failures. If a condition prevents deterministic execution, severity must be `error`.

## Payload Shape
Each validation item must use this shape:

```json
{
  "code": "SPEC_SCHEMA_REQUIRED_FIELD",
  "message": "Field is required.",
  "path": "timeline.events[0].targetId",
  "value": null,
  "hint": "Provide a targetId that matches an existing scene object id.",
  "severity": "error"
}
```

## Field Definitions
- `code`: stable machine-readable identifier for the rule that failed.
- `message`: concise human-readable explanation of the failure.
- `path`: canonical location of the failing value within the spec. Use dotted paths for object members and bracket indices for arrays.
- `value`: the offending value when it can be represented safely; otherwise `null`.
- `hint`: optional remediation guidance that does not alter the meaning of the failure.
- `severity`: `error` or `warning`.

## Code Naming Rules
- Codes must be stable across processors implementing the same contract line.
- Codes should follow the pattern `SPEC_<CATEGORY>_<RULE>`.
- Category names should align with the validation taxonomy: `CONTRACT`, `SCHEMA`, `NORMALIZATION`, `SEMANTIC`, `READINESS`.
- Codes must not embed runtime-specific class names, parser names, or exception text.

## Path Rules
- Root-level paths use top-level property names such as `meta` or `timeline.events[0]`.
- Array positions use zero-based indices.
- Paths refer to canonical contract locations, not internal implementation structures.
- If an error applies to the entire document, path may be `$`.

## Deterministic Error Ordering
Validation output must be ordered deterministically using the following keys:
1. ascending gate order: contract, schema, normalization, semantic consistency, execution readiness;
2. ascending path order using lexical comparison on canonical path strings;
3. ascending severity order with `error` before `warning`;
4. ascending code order;
5. original canonical occurrence order when all prior keys are identical.

A processor must not rely on hash-map iteration, parser-specific discovery order, or platform behavior when ordering errors.

## Response-Level Expectations
- Validation may return one or more items.
- Returning multiple errors from the same gate is allowed when they can be identified without speculative recovery.
- A downstream gate must not contribute errors if an upstream gate failure prevented that gate from running.
- Error payloads must remain stable enough for snapshot-style tests and CI assertions.

## Representative Codes
- `SPEC_CONTRACT_MISSING_SCHEMA_VERSION`
- `SPEC_CONTRACT_UNSUPPORTED_MAJOR_VERSION`
- `SPEC_SCHEMA_REQUIRED_FIELD`
- `SPEC_SCHEMA_INVALID_TYPE`
- `SPEC_NORMALIZATION_CONFLICTING_DEFAULTS`
- `SPEC_SEMANTIC_UNRESOLVED_REFERENCE`
- `SPEC_READINESS_MISSING_EXECUTION_VALUE`

## Representative Invalid-Spec Examples

### Example 1: Missing schema version
Invalid spec:
```json
{
  "meta": {
    "projectId": "demo-001"
  },
  "timeline": {
    "events": []
  }
}
```

Expected validation output:
```json
[
  {
    "code": "SPEC_CONTRACT_MISSING_SCHEMA_VERSION",
    "message": "meta.schemaVersion is required.",
    "path": "meta.schemaVersion",
    "value": null,
    "hint": "Declare a supported major.minor schema version such as 1.0.",
    "severity": "error"
  }
]
```

### Example 2: Invalid field type and unresolved reference
Invalid spec:
```json
{
  "meta": {
    "projectId": "demo-002",
    "schemaVersion": "1.0"
  },
  "scene": {
    "objects": [
      { "id": "obj-1", "type": "path", "assetRef": "svg-1", "layer": 1 }
    ]
  },
  "timeline": {
    "events": [
      { "id": "ev-1", "start": "now", "duration": 1.5, "targetId": "obj-9", "action": "draw" }
    ]
  }
}
```

Expected validation output:
```json
[
  {
    "code": "SPEC_SCHEMA_INVALID_TYPE",
    "message": "Field must be a number.",
    "path": "timeline.events[0].start",
    "value": "now",
    "hint": "Provide start as a numeric timeline position.",
    "severity": "error"
  }
]
```

Reasoning:
- schema gate fails on `timeline.events[0].start`;
- semantic reference checks do not run because downstream gates are blocked.

### Example 3: Duplicate identities after schema success
Invalid spec:
```json
{
  "meta": {
    "projectId": "demo-003",
    "schemaVersion": "1.0"
  },
  "scene": {
    "objects": [
      { "id": "obj-1", "type": "path", "assetRef": "svg-1", "layer": 1 },
      { "id": "obj-1", "type": "path", "assetRef": "svg-2", "layer": 2 }
    ]
  },
  "timeline": {
    "events": []
  }
}
```

Expected validation output:
```json
[
  {
    "code": "SPEC_SEMANTIC_DUPLICATE_IDENTIFIER",
    "message": "Identifier must be unique within scene.objects.",
    "path": "scene.objects[1].id",
    "value": "obj-1",
    "hint": "Rename the duplicate object id so references resolve to exactly one target.",
    "severity": "error"
  }
]
```

### Example 4: Deterministic multi-error ordering within one gate
Invalid spec:
```json
{
  "meta": {
    "projectId": "demo-004",
    "schemaVersion": "1.0"
  },
  "timeline": {
    "events": [
      { "id": "ev-2", "start": 2.0, "duration": "fast", "targetId": "obj-2", "action": "draw" },
      { "id": "ev-1", "start": 1.0, "duration": "slow", "targetId": "obj-1", "action": "draw" }
    ]
  }
}
```

Expected validation output:
```json
[
  {
    "code": "SPEC_SCHEMA_INVALID_TYPE",
    "message": "Field must be a number.",
    "path": "timeline.events[0].duration",
    "value": "fast",
    "hint": "Provide duration as a numeric timeline span.",
    "severity": "error"
  },
  {
    "code": "SPEC_SCHEMA_INVALID_TYPE",
    "message": "Field must be a number.",
    "path": "timeline.events[1].duration",
    "value": "slow",
    "hint": "Provide duration as a numeric timeline span.",
    "severity": "error"
  }
]
```

Reasoning:
- both errors occur in the schema gate;
- ordering follows ascending canonical path order;
- event ids do not affect error order when path ordering already distinguishes the items.

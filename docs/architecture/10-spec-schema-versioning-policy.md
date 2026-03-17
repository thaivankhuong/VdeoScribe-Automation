# Spec Schema Versioning Policy

## Purpose
Define how project/spec JSON versions are declared, interpreted, and evolved so future engine phases can validate inputs consistently without breaking deterministic execution guarantees.

## Version Identifier
- Every spec must declare `meta.schemaVersion`.
- The version format is `major.minor`.
- `major` represents a compatibility line.
- `minor` represents an additive or clarifying revision within the same compatibility line.
- Patch-style versions are not part of the runtime contract. Editorial corrections may be tracked in repository docs but must not change the declared runtime schema identifier.

## Compatibility Contract
- A processor must reject any spec whose `major` version it does not support.
- A processor may accept a newer `minor` version only when the processor explicitly declares support for that `major` line and the newer `minor` does not require behavior outside the documented contract.
- A supported `major` line guarantees:
  - field meanings remain stable;
  - deterministic normalization rules remain stable;
  - validation/error semantics remain stable enough for automation and tests.
- A `minor` version may:
  - add optional fields;
  - tighten documentation for existing behavior;
  - add new enum members or object shapes only when older processors are allowed to reject those cases deterministically.
- A `minor` version must not silently change:
  - timing semantics;
  - ordering rules;
  - default value meaning;
  - object identity/reference rules;
  - validation severity for previously valid inputs.

## Major Version Changes
A `major` version bump is required when any of the following changes occur:
- removal or rename of required fields;
- behavior changes that alter resolved frame state for previously valid specs;
- changes to normalization ordering or tie-break rules;
- changed meaning of existing values;
- validation contract changes that would alter machine-consumable error handling in a non-backward-compatible way.

## Minor Version Changes
A `minor` version bump is appropriate when changes are backward-compatible within the current compatibility line, including:
- adding optional metadata that does not affect execution unless explicitly used;
- introducing new optional sections gated by deterministic validation;
- clarifying constraints that existing valid specs already satisfy;
- documenting additional examples for the same contract behavior.

## Deprecation Policy
- Deprecation must occur before removal inside a compatibility line.
- Deprecated fields or values must remain valid for the rest of the current `major` line unless the docs explicitly declare a deterministic migration rule.
- Deprecation notices must document:
  - the deprecated field/value;
  - the recommended replacement;
  - the earliest `major` version where removal may occur;
  - whether normalization can upgrade it automatically or whether author action is required.
- Deprecation warnings are contract-level guidance. They must not mutate execution behavior by themselves.

## Processor Expectations
- A processor must validate the declared version before any semantic execution work begins.
- Version handling must occur before timeline evaluation, rendering, or export logic.
- If a processor supports multiple `minor` revisions in one `major` line, it must normalize them into the same canonical model before execution.
- If automatic upgrades are supported, the upgrade path must be deterministic and documented for each affected version pair.

## Upgrade Expectations
- Spec authors are responsible for declaring the intended contract line via `meta.schemaVersion`.
- Processors are responsible for either:
  - accepting the version and normalizing it to the canonical form for that compatibility line; or
  - rejecting it with an explicit validation error.
- Upgrades across `major` lines are not implicit. They require an explicit migration step or a dedicated upgrader contract.
- Upgrades within a supported `major` line must preserve intended meaning and produce the same normalized representation for equivalent inputs.

## Deterministic Acceptance Rules
- Version checks must produce the same outcome for the same declared version, independent of file order, platform, or runtime environment.
- A spec missing `meta.schemaVersion` must be rejected deterministically as a contract failure.
- A malformed version string must be rejected deterministically before schema validation proceeds.
- When multiple validation issues exist, version contract failures still participate in the global deterministic error ordering defined by the validation contract.

## Examples

### Accepted
```json
{
  "meta": {
    "projectId": "demo-001",
    "schemaVersion": "1.0"
  }
}
```

### Rejected: missing version
```json
{
  "meta": {
    "projectId": "demo-001"
  }
}
```

Expected outcome:
- reject before execution readiness;
- report a contract-level error at `meta.schemaVersion`.

### Rejected: unsupported compatibility line
```json
{
  "meta": {
    "projectId": "demo-001",
    "schemaVersion": "2.0"
  }
}
```

Expected outcome:
- reject deterministically;
- require an explicit migration or processor upgrade;
- do not attempt silent downgrade or best-effort execution.

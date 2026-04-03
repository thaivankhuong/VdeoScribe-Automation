# Phase 17: Template Contracts and Scene Composition - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md.

**Date:** 2026-04-03
**Phase:** 17-template-contracts-and-scene-composition
**Areas discussed:** Template package shape, Slot contract strictness, Composition semantics, Validation + authoring workflow

---

## Template package shape

| Option | Description | Selected |
|--------|-------------|----------|
| Per-template folder + index manifest | Each template in its own folder with global index catalog | ✓ |
| Single catalog file | All templates in one large JSON file | |
| Per-template file only | Standalone template files without central index | |

**User's choice:** Per-template folder + index manifest  
**Notes:** User requested to follow recommended option and prioritize correctness.

### Versioning and identity

| Option | Description | Selected |
|--------|-------------|----------|
| `templateId` immutable + `version` + `status(active|deprecated)` | Deterministic traceability and replay safety | ✓ |
| `templateId` only | Simpler but weak replay guarantees | |
| `templateId` + timestamp version | Easy generation but less controlled release semantics | |

**User's choice:** `templateId` + `version` + `status`.

### Governed references

| Option | Description | Selected |
|--------|-------------|----------|
| ID-only references | Template references only `assetId`/`effectProfileId` | ✓ |
| ID + sourcePath fallback | Flexible but less deterministic | |
| Path-first references | Fast initially but drift-prone | |

**User's choice:** ID-only references.

### Registry location

| Option | Description | Selected |
|--------|-------------|----------|
| `.planning/templates/...` | Repo-governed, no-UI/no-DB friendly | ✓ |
| `artifacts/templates/...` | Artifact-focused but less contract-centric | |
| `src/Whiteboard.Core/Templates/...` | Code-coupled storage | |

**User's choice:** `.planning/templates/...`.

---

## Slot contract strictness

| Option | Description | Selected |
|--------|-------------|----------|
| Strict slot schema + required/optional + fail-fast unknown/missing | Most deterministic and easiest to test | ✓ |
| Lenient unknown slots ignored | Flexible but hides drift and errors | |
| Auto-fill/fallback for required governance refs | Convenient but breaks controlled governance | |

**User's choice:** Delegated to assistant.  
**Assistant selection:** Strict slot schema + deterministic fail-fast validation.

---

## Composition semantics

| Option | Description | Selected |
|--------|-------------|----------|
| Deterministic namespacing + explicit offsets + collision fail-fast | Safe composition with reproducible output | ✓ |
| Auto-rewrite collisions silently | Reduces immediate failures but risks hidden drift | |
| Merge by heuristic ordering only | Harder to reason about and verify | |

**User's choice:** Delegated to assistant.  
**Assistant selection:** Deterministic namespacing + explicit offset model + fail-fast collisions.

---

## Validation + authoring workflow

| Option | Description | Selected |
|--------|-------------|----------|
| Core contract validation + CLI orchestration + file-based authoring | Aligns module boundaries and current milestone scope | ✓ |
| CLI-only ad-hoc validation | Fast but weak separation and reuse | |
| UI-driven authoring pipeline now | Outside Phase 17 scope | |

**User's choice:** Delegated to assistant.  
**Assistant selection:** Core validation + CLI orchestration + file-based authoring.

---

## the agent's Discretion

- Final schema field naming and contract split across files.
- Exact CLI command shapes for template validation/instantiation.
- Internal service decomposition while preserving Core/CLI boundaries.

## Deferred Ideas

- Database-backed template registry.
- UI for uploading/managing templates, images, and SVG assets.


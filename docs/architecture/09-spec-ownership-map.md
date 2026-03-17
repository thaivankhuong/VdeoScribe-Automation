# Spec Ownership Map

## Purpose
Map top-level project/spec JSON sections to the module contracts that own validation, interpretation, and downstream handoff responsibilities.

## Ownership Table

| Spec section | Primary owning module | Secondary consumers | Ownership rationale |
| --- | --- | --- | --- |
| `meta` | `Whiteboard.Core` | `Whiteboard.Engine`, `Whiteboard.Cli` | Schema identity, versioning, and shared project metadata are contract concerns before orchestration begins. |
| `assets` | `Whiteboard.Core` | `Whiteboard.Engine`, `Whiteboard.Renderer`, `Whiteboard.Export` | Asset descriptors and validation contracts should be stable and module-neutral; later modules consume resolved references only. |
| `scene` | `Whiteboard.Engine` | `Whiteboard.Renderer` | Scene composition semantics belong to deterministic evaluation and must not be inferred inside renderer adapters. |
| `timeline` | `Whiteboard.Engine` | `Whiteboard.Renderer`, `Whiteboard.Export` | Timeline ordering, frame mapping, and lifecycle resolution are core engine behaviors. |
| `output` | `Whiteboard.Core` | `Whiteboard.Engine`, `Whiteboard.Export`, `Whiteboard.Cli` | Output profile contracts should be shared definitions; export consumes them while Engine may validate timing-related implications. |

## Section Responsibilities

### `meta`
- Defines schema version, project identity, and other top-level contract metadata.
- Must be validated before any orchestration work starts.
- Provides compatibility context for the rest of the project/spec JSON document.

### `assets`
- Declares external resources and the contract fields needed to resolve them safely.
- Core owns the asset descriptor schema; Engine and Renderer consume explicit resolved handles rather than inventing ad hoc formats.
- Export may consume asset-related metadata only where packaging requires it.

### `scene`
- Describes what entities exist and the declarative inputs required for deterministic evaluation.
- Engine owns interpretation of scene semantics because scene behavior must come from project/spec JSON, not renderer heuristics.
- Renderer receives resolved frame-state outputs derived from `scene`, not raw scene logic to interpret independently.

### `timeline`
- Defines event ordering, timing intent, and lifecycle progression.
- Engine owns conversion from timeline intent into frame-state sequencing.
- Export may rely on derived timing metadata, but it does not own timeline meaning.

### `output`
- Defines declared render/export targets such as frame rate, dimensions, and packaging intent.
- Core owns the contract shape so validation and compatibility rules stay stable.
- Export consumes the resolved output contract without changing semantics.

## Ownership Rules
- Each top-level project/spec JSON section has one primary owner responsible for contract definition or semantic interpretation.
- Secondary consumers may read a section only through explicit contracts or derived handoffs from the primary owner.
- No module may bypass ownership by embedding hardcoded scene logic or undocumented defaults.
- When a section influences deterministic behavior, the owning module must document the rule in a stable contract before other modules rely on it.

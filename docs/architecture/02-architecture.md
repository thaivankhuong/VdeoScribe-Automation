# Architecture

## High-Level System Flow
1. Read project/spec JSON as the source of truth.
2. Validate and normalize timeline and object data.
3. Resolve frame-by-frame object and camera state.
4. Hand resolved frame state to renderer adapters.
5. Send deterministic frame outputs and timing metadata to the export pipeline.

## Module Responsibilities
- `Whiteboard.Core`: domain models, contracts, schema primitives, and shared validation concepts.
- `Whiteboard.Engine`: input normalization, timeline orchestration, state resolution, and sequencing.
- `Whiteboard.Renderer`: consume resolved frame state and produce visual frame output through rendering adapters.
- `Whiteboard.Export`: consume frame outputs and audio/timing metadata for final media packaging.
- `Whiteboard.Cli`: batch/job entry point and orchestration surface without domain logic.

## Dependency Direction
- `Core` has no dependency on upper modules.
- `Engine` depends on `Core`.
- `Renderer` and `Export` depend on contracts and resolved state from `Core`/`Engine`.
- `CLI` composes modules but must not contain domain or rendering rules.

## Why UI/Editor Is Out of Scope Now
Engine behavior must be stable and deterministic before building any authoring layer. UI work now would force premature assumptions about workflows, increase coupling risk, and slow down validation of the engine core.

## Future Extensibility Notes
- Keep input schema versioned for long-term compatibility.
- Reserve extension points for new renderers and encoders.
- Design batch-friendly orchestration for template/scenario reuse.
- Keep renderer and export integrations replaceable behind stable contracts.
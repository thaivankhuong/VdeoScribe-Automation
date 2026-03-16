# Architecture Map

## Core (`Whiteboard.Core`)
Responsibility:
- Domain and specification contracts.
- JSON/spec-friendly project model.
- Shared value objects and enums.

Includes:
- VideoProject root, assets, scene definitions, timeline definitions, output/meta contracts.

Must not include:
- Rendering behavior
- Export behavior
- CLI orchestration

## Engine (`Whiteboard.Engine`)
Responsibility:
- Resolve `VideoProject` + frame context into deterministic resolved frame state.
- Provide frame-state resolver contracts and orchestration skeleton.

Includes:
- `FrameContext`
- `ResolvedFrameState` and related resolved models
- Resolver interfaces + coordinating frame resolver

Must not include:
- Pixel drawing
- File export
- CLI workflow

## Renderer (`Whiteboard.Renderer`)
Responsibility:
- Accept resolved frame state and run renderer pipeline contracts.
- Produce structured render results for downstream export.

Includes:
- Render request/result contracts
- Frame/scene/object renderer contracts
- Placeholder render surface + skeleton services

Must not include:
- Actual SVG/path rasterization algorithms (until implementation phase)
- File writing or FFmpeg behaviors

## Export (`Whiteboard.Export`)
Responsibility:
- Convert renderer outputs into export pipeline artifacts/results.
- Define export targets and pipeline contracts.

Includes (planned/current scaffold):
- Export request/result models
- Export pipeline interfaces
- Placeholder export orchestration

Must not include:
- CLI concerns
- UI/editor concerns

## CLI (`Whiteboard.Cli`)
Responsibility:
- Entry-point orchestration for running the spec-driven pipeline.
- Wire Core -> Engine -> Renderer -> Export flow.

Includes:
- Argument handling contracts/skeleton
- Pipeline invocation orchestration

Must not include:
- Domain/model ownership
- Rendering/export implementation details

## Dependency Direction
- `Core` <- `Engine`
- `Core` <- `Renderer`
- `Core` <- `Export`
- `Core`, `Engine`, `Renderer`, `Export` <- `Cli`

Design principles:
- Deterministic, frame-based behavior
- Spec/JSON-driven inputs
- Strict module boundaries
- Contract-first progression

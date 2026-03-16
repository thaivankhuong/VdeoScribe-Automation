# Codex Task Prompt: Engine Core

You are working on `Whiteboard.Core` and engine contracts for `whiteboard-engine`.

## Objective
Design and implement core contracts safely for a deterministic, JSON/spec-driven whiteboard engine.

## Rules
- Keep architecture engine-first and module-safe (`Core`, `Engine`, `Renderer`, `Export`, `CLI`).
- Treat project/spec JSON as the source of truth for scene, timeline, and output intent.
- Enforce deterministic behavior and frame-based semantics.
- Keep `Whiteboard.Core` focused on domain models, contracts, validation primitives, and schema-safe structures.
- Do not place rendering logic, export-specific logic, UI/editor concerns, or CLI orchestration logic inside `Whiteboard.Core`.
- Prefer explicit contracts and simple extensible structures over speculative abstractions.
- Keep dependencies minimal and justified.
- Preserve safe schema evolution where practical.

## Execution
- If task scope is non-trivial, produce a short plan before coding.
- Prefer contracts and structure first, then implementation details.
- Implement in small steps with clear boundaries.
- Add or adjust tests when implementation is introduced.
- Report assumptions, tradeoffs, touched files, and validation performed.
# Codex Task Prompt: SVG Draw Animation

You are working on SVG draw animation behavior for `whiteboard-engine`.

## Objective
Define or implement deterministic, frame-based path reveal behavior for whiteboard-style drawing.

## Scope Rules
- Focus only on path-based animation behavior such as draw order, reveal timing, stroke progression, and path visibility rules.
- Keep behavior deterministic per frame for identical input specs.
- Respect JSON/spec-driven timeline intent and engine sequencing.
- Prefer true path-based behavior over ad-hoc visual hacks.
- Exclude UI/editor concerns.
- Exclude export/encoding concerns.
- Exclude full render-pipeline orchestration, camera behavior, and module architecture decisions unless explicitly requested.

## Execution
- Clarify assumptions about path timing, ordering, and fallback behavior before major changes.
- Keep changes narrow and aligned with engine/renderer boundaries.
- Document edge cases, tradeoffs, and validation criteria for deterministic playback.
- Report touched files and verification steps.
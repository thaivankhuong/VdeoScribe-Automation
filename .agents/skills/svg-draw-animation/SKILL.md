---
name: svg-draw-animation
description: Define SVG path reveal logic, draw-order behavior, handwriting-like animation constraints, and path-based rendering rules for the whiteboard engine. Use when planning SVG draw animation behavior. Do not use for module architecture decisions, full render-pipeline orchestration, generic text animation outside SVG/path handling, or final video export design.
---

## Purpose
Define deterministic, path-based SVG draw animation behavior that can be driven by project/spec data and integrated into the whiteboard rendering engine.

## When to Use
- Planning stroke reveal and progressive path drawing.
- Defining path ordering quality and layered draw-order behavior.
- Specifying handwriting-like motion constraints at path level.
- Evaluating fallback behavior for complex or imperfect SVG assets.

## Rules
- Use path-based reveal rules, not ad-hoc visual hacks.
- Keep animation timing deterministic at frame level.
- Respect engine sequencing and object visibility lifecycle.
- Keep SVG draw behavior configurable through project/spec data where possible.
- Define clear ordering, timing, and fallback rules for complex path structures.
- Keep concerns limited to SVG draw behavior, not architecture, export, or general pipeline orchestration.
- Do not implement animation code in this phase unless explicitly requested.

## Expected Output
- SVG draw animation rule set.
- Path timing, ordering, and reveal constraints.
- Edge cases and fallback behavior for path-based rendering.
- Risks and tradeoffs affecting visual realism and engine simplicity.
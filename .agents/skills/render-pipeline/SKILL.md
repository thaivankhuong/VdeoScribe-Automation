---
name: render-pipeline
description: Plan deterministic, frame-based rendering flow, timeline processing, object state resolution, camera timing integration, and render sequencing for the engine. Use when defining rendering pipeline behavior. Do not use for module architecture decisions, SVG draw-path specifics, or final video export design.
---

## Purpose
Specify a deterministic, frame-based rendering pipeline that transforms project/spec JSON into render-ready frame states and ordered visual output.

## When to Use
- Defining timeline-to-frame execution flow.
- Planning object state resolution at each frame tick.
- Designing render sequencing, layer ordering, and camera timing integration.
- Clarifying orchestration flow between timeline processing and renderer-facing inputs.

## Rules
- Treat rendering as deterministic and frame-based.
- Drive pipeline from project/spec JSON, not hardcoded scenes.
- Separate orchestration concerns from renderer adapter concerns.
- Keep timing, ordering, and repeatability rules explicit.
- Define clear handoff contracts between spec input, resolved frame state, and renderer output.
- Do not write renderer implementation code in this phase unless explicitly requested.

## Expected Output
- Pipeline stage outline (input, resolve, sequence, output).
- Determinism and timing rules for frame generation.
- Handoff contracts for frame state and renderer input/output.
- Frame/timeline validation checklist.
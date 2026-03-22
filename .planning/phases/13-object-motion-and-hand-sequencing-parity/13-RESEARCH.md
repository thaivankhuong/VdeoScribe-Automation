---
phase: 13-object-motion-and-hand-sequencing-parity
summary_type: research
completed: 2026-03-22
---

# Phase 13 Research

## Goal
Move the authored parity witness beyond static asset composition by making object transform timing and hand-follow sequencing behave more like the target VideoScribe/reference sample while preserving deterministic engine semantics.

## Current State
- Phase 12 finished with `artifacts/source-parity-demo/project-engine.json` as the only active authored parity path.
- The authored witness already renders end to end with deterministic manifests and committed frame evidence under `artifacts/source-parity-demo/out/phase12-authored-witness`.
- `ObjectStateResolver` already applies `Move`, `Scale`, `Rotate`, and `Fade` events deterministically, but coverage is still generic and not yet tuned around the parity witness scene.
- `FrameRenderer` resolves hand guidance from rendered operations, using active partial-path, text, or image operations as proxies for the drawing pose.
- The current hand-follow behavior is deterministic but still heuristic; it is not yet validated as parity-oriented sequencing across object boundaries for the authored witness path.

## Implementation Direction
1. Lock transform-event semantics around the authored witness scene.
   - Treat `Move`, `Scale`, `Rotate`, and `Fade` as the only in-scope motion controls for this phase.
   - Tighten tests and witness-scene usage so later fidelity work depends on explicit transform behavior rather than ad hoc visual tweaks.
2. Improve hand-follow sequencing on the authored route.
   - Keep hand-follow derived from resolved frame state and renderer-visible operations.
   - Prefer deterministic ordering rules driven by active object/path progression rather than renderer-only heuristics that could drift from engine state.
3. Re-validate the parity witness end to end.
   - Full-scene motion and hand sequencing should survive CLI/export execution and preserve deterministic package evidence across repeated runs.

## Scope Guardrails
- No return to crop-based shortcuts as the main parity route.
- No editor UI or interactive authoring flow.
- No Phase 14 fidelity tuning for illustration polish or text styling beyond what is required to support motion sequencing.
- No speculative animation system beyond the existing timeline action vocabulary.

## Planning-Critical Decisions

### A) Motion Semantics Policy
Recommended policy:
- Keep transform behavior centered on existing timeline actions: `Move`, `Scale`, `Rotate`, and `Fade`.
- Strengthen parity through fixture/spec usage and deterministic tests, not by inventing extra motion primitives in Phase 13.

Why:
- The engine already owns these semantics, and the roadmap explicitly says Phase 13 should prove they are sufficient for parity scenes.

### B) Hand Sequencing Policy
Recommended policy:
- Base hand-follow sequencing on active authored object/path progression from resolved frame state and rendered operations.
- Make object-to-object hand transitions deterministic and reviewable rather than purely opportunistic.

Why:
- Hand placement must follow the authored route without silently depending on shortcut imagery or renderer-local guesses.

### C) Witness Validation Policy
Recommended policy:
- Validate motion and hand timing on `project-engine.json` through full CLI/export witness runs.
- Preserve package-equivalence checks across repeated runs so Phase 13 does not weaken the deterministic baseline established in Phase 12.

### D) Boundary Policy
Recommended policy:
- Engine remains the source of transform semantics.
- Renderer remains responsible for visualizing resolved state and hand-follow overlays/assets.
- CLI stays responsible for path resolution and orchestration.

Why:
- This preserves the repo's current separation of concerns and prevents parity work from collapsing architectural boundaries.

## Risks
- It is easy to overfit motion behavior to one witness scene and accidentally create dead-end semantics.
- Hand-follow adjustments can drift away from engine state if renderer heuristics grow without matching regression coverage.
- Motion tweaks can preserve visual plausibility while still regressing deterministic ordering or package equivalence unless full witness verification stays in the loop.

## Recommended Breakdown
- `13-01`: Finalize object transform event semantics and parity-oriented timeline usage.
- `13-02`: Improve hand-follow behavior and object-to-object sequencing for parity scenes.
- `13-03`: Validate motion and hand timing parity through frame/video witnesses.
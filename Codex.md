# Codex

## Mission
Build a deterministic, spec-driven .NET whiteboard video engine that reproduces core VideoScribe-like rendering behavior.

## Primary Goal
Every implementation decision should move the engine toward VideoScribe parity in rendered output, not just toward generic whiteboard-video behavior.

## Product Direction
- The target is output parity with VideoScribe/reference samples as closely as practical within deterministic engine constraints.
- The system is engine-first, not editor-first.
- Reference samples are proof harnesses for engine behavior, not excuses to hardcode one-off demos.

## Hard Constraints
- Determinism is non-negotiable.
- The main parity path must use authored assets, explicit scene objects, and engine semantics.
- Whole-frame crops, segmented shortcut fixtures, or comparison-only assets must never become the claimed success path.
- Preserve strict module boundaries: Core -> Engine -> Renderer -> Export -> CLI.
- Renderer consumes resolved engine contracts; it must not recompute timeline or camera semantics.
- Export packages renderer output; it must not alter engine or renderer meaning.

## Current Parity Baseline
- `artifacts/source-parity-demo/project-engine.json` is the active authored witness path.
- `artifacts/source-parity-demo/project.json` and `artifacts/source-parity-demo/project-image-hand.json` are legacy comparison fixtures only.
- New parity work must strengthen the authored witness path rather than reintroduce shortcut-based success claims.

## Working Rules
- Execute only the active roadmap phase unless explicitly told otherwise.
- Keep changes small, reviewable, and verifiable.
- Add regression coverage when touching parity behavior, timeline semantics, or deterministic output.
- Prefer end-to-end witness artifacts when a phase is about parity proof, not only unit-level assertions.
- Do not revert unrelated dirty worktree changes.

## Non-Goals
- No editor UI work.
- No fake parity by screenshot matching through crop shortcuts.
- No broad refactors unrelated to the active parity objective.
- No convenience changes that weaken deterministic guarantees.

## Definition Of Done
- The change improves or protects the active parity path.
- Verification passes with deterministic results.
- Required witness artifacts or regression tests are updated.
- Planning/state docs stay aligned with the real project position.

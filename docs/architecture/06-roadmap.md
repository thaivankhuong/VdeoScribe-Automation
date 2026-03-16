# Roadmap

## Phased Plan
1. Bootstrap and architecture baseline
2. Spec schema, normalization rules, and validation contracts
3. Engine timeline and state-resolution core
4. SVG draw rendering adapter
5. Frame-to-video export pipeline
6. CLI batch orchestration and integration validation

## Recommended Build Order
- Lock spec and module contracts first.
- Define schema versioning and normalization behavior early.
- Implement deterministic timeline evaluation before renderer details.
- Add renderer/export adapters only after engine semantics are stable.
- Add batch workflow once single-project flow is reliable.

## MVP Milestones
- M1: Architecture + schema baseline approved.
- M2: Deterministic frame-state generation from spec JSON.
- M3: SVG draw progression rendered to frame outputs.
- M4: Export pipeline produces final video with synced audio.
- M5: CLI can run repeatable scenario batches.

## Intentionally Postponed
- Editor UI and interactive authoring tools.
- Any attempt to clone the VideoScribe authoring experience before engine stability.
- Advanced visual effects not required for core behavior parity.
- Broad plugin/extensibility surface beyond immediate architecture needs.

## Execution Style
Progress deliberately and architecture-first: define contracts, validate determinism, then implement adapters.
Each phase should end with a clear validation checkpoint before moving forward.
Avoid rushing features that increase coupling before core behavior is stable.
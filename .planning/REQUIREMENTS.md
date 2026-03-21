# Requirements: whiteboard-engine

**Defined:** 2026-03-21
**Core Value:** Given the same spec, assets, and settings, the engine must always produce the same frame/video output.

## v1 Requirements

### Parity Structure

- [ ] **PAR-01**: A reference scene can be represented as separate authored scene objects and assets instead of a whole-frame source crop.
- [ ] **PAR-02**: Object-level move/scale/rotate/fade timeline events drive final frame transforms deterministically for parity scenes.
- [ ] **PAR-03**: Hand draw sequencing follows the active object/path deterministically across the full scene timeline.
- [ ] **PAR-04**: A parity sample can reproduce the intended object order and final composition of the reference scene without crop-based content shortcuts.

### Text and Asset Fidelity

- [ ] **TXT-01**: Title, body, and footer content in parity samples render from authored text/vector assets rather than source-video text crops.
- [ ] **AST-01**: SVG, text, and hand assets needed for parity scenes load through spec-driven manifests and reusable asset references.
- [ ] **AST-02**: Parity sample workflows emit witness frames and final video outputs that are easy to inspect during iteration.

### Validation

- [ ] **VAL-01**: Repeated runs of the same parity sample preserve deterministic frame/video witnesses suitable for regression review.

## v2 Requirements

### Extensibility

- **EXT-04**: Reference-scene decomposition can be assisted by reusable tooling instead of manual authoring only.
- **EXT-05**: Additional parity templates and generalized source-to-spec workflows can be added beyond the current sample set.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Whole-frame image/video crops as the primary parity method | Conflicts with engine-first semantic reproduction goals |
| Interactive editor UI | Still outside the active engine milestone |
| ML-based auto-tracing or auto-vectorization | Too broad for the current focused parity milestone |
| Marketplace/template management workflows | Defer until parity workflow is proven |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| PAR-01 | Phase 12 | Pending |
| AST-01 | Phase 12 | Pending |
| PAR-02 | Phase 13 | Pending |
| PAR-03 | Phase 13 | Pending |
| PAR-04 | Phase 14 | Pending |
| TXT-01 | Phase 14 | Pending |
| AST-02 | Phase 15 | Pending |
| VAL-01 | Phase 15 | Pending |

**Coverage:**
- v1 requirements: 8 total
- Mapped to phases: 8
- Unmapped: 0

---
*Requirements defined: 2026-03-21*
*Last updated: 2026-03-21 after starting v1.1 Source Parity milestone*

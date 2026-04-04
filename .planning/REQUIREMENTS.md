# Requirements: whiteboard-engine

**Defined:** 2026-04-03
**Core Value:** Given the same spec, assets, and settings, the engine must always produce the same frame/video output.

## v1 Requirements

### Asset Registry

- [x] **REG-01**: Operator can register SVG, image, font, and hand assets with stable IDs and version metadata.
- [x] **REG-02**: A project can pin to a specific registry snapshot so renders are reproducible across runs and environments.
- [x] **REG-03**: Validation fails with explicit errors when specs reference unknown, deprecated, or type-mismatched asset IDs.

### Effect Governance

- [x] **EFX-01**: Scene objects can use only whitelisted effect profiles defined in the controlled library.
- [x] **EFX-02**: Effect profile parameters are range-validated before timeline execution to prevent undefined render behavior.

### Template System

- [x] **TMP-01**: Authors can define reusable scene templates with named slots and slot constraints in JSON.
- [x] **TMP-02**: Template instantiation produces deterministic scene objects and timeline fragments from the same inputs.

### Script Compilation

- [x] **CMP-01**: CLI can compile structured script/scenario input into a valid project spec using template and asset mapping rules.
- [x] **CMP-02**: Compilation outputs an auditable report (selected template, slot mapping, asset/effect IDs, validation warnings/errors).

### Automation and Validation

- [ ] **AUT-01**: Batch CLI can execute script -> spec -> render -> export jobs without manual editing steps.
- [ ] **AUT-02**: Each batch job emits deterministic artifact manifests with output media, witnesses, and status.
- [ ] **VAL-02**: Pipeline enforces deterministic witness/regression checks and fails jobs when drift is detected.

## v2 Requirements

### Extensibility

- **EXT-06**: Template authoring is assisted by reusable tooling and linting beyond manual JSON editing.
- **EXT-07**: Multi-language script normalization and style adaptation are supported for broader automation scenarios.

## Out of Scope

| Feature | Reason |
|---------|--------|
| AI-generated image creation in runtime pipeline | Conflicts with controlled-library and deterministic governance goals |
| Interactive editor UI | Engine-first batch automation remains the active scope |
| Unrestricted third-party effect/plugin ingestion | Violates effect control and reproducibility guarantees |
| Whole-frame crop reconstruction as the main path | Conflicts with semantic authored-object rendering model |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| REG-01 | Phase 16 | Complete |
| REG-02 | Phase 16 | Complete |
| REG-03 | Phase 16 | Complete |
| EFX-01 | Phase 16 | Complete |
| EFX-02 | Phase 16 | Complete |
| TMP-01 | Phase 17 | Complete |
| TMP-02 | Phase 17 | Complete |
| CMP-01 | Phase 18 | Complete |
| CMP-02 | Phase 18 | Complete |
| AUT-01 | Phase 19 | Pending |
| AUT-02 | Phase 19 | Pending |
| VAL-02 | Phase 20 | Pending |

**Coverage:**
- v1 requirements: 12 total
- Mapped to phases: 12
- Unmapped: 0

---
*Requirements defined: 2026-04-03*
*Last updated: 2026-04-03 after completing Phase 16 controlled registry and effect governance requirements*

# Requirements: whiteboard-engine

**Defined:** 2026-04-04
**Core Value:** Given the same spec, assets, and settings, the engine must always produce the same frame/video output.

## v1 Requirements

### Batch Throughput

- [ ] **BTHR-01**: Operator can configure deterministic batch execution profile (sequential vs bounded parallel) from manifest/config without changing job semantics.
- [ ] **BTHR-02**: Batch execution preserves deterministic per-job outputs and summary ordering regardless of throughput profile.
- [ ] **BTHR-03**: Batch run emits deterministic throughput diagnostics (profile, worker limits, queue stats) as auditable artifacts.

### Recovery and Replay

- [ ] **RPLY-01**: Operator can deterministically resume an interrupted batch from persisted per-job manifests without re-running completed successful jobs.
- [ ] **RPLY-02**: Operator can deterministically rerun only failed jobs using a stable replay manifest derived from previous batch outputs.
- [ ] **RPLY-03**: Resume/replay flows preserve compile/run/gate evidence lineage links to original batch execution.

### Reliability Hardening

- [ ] **RLBL-01**: Batch pipeline enforces deterministic preflight validation for all external dependencies required by targeted jobs before execution starts.
- [ ] **RLBL-02**: Gated batch runs produce deterministic milestone-level release witness bundle artifacts for representative scenario sets.
- [ ] **RLBL-03**: Reliability validation includes deterministic soak/regression checks that fail the run on reproducible drift or artifact inconsistency.

## v2 Requirements

### Operational UX

- **OPUX-01**: Operator dashboard for batch fleet visibility and intervention.
- **OPUX-02**: Interactive troubleshooting assistant for failed job diagnostics.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Interactive editor UI | Engine-first automation scope remains the priority |
| Non-deterministic AI runtime generation | Conflicts with deterministic reproducibility |
| Distributed multi-machine scheduler | Defer until single-host deterministic reliability is saturated |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| BTHR-01 | Phase 21 | Pending |
| BTHR-02 | Phase 21 | Pending |
| BTHR-03 | Phase 22 | Pending |
| RPLY-01 | Phase 23 | Pending |
| RPLY-02 | Phase 23 | Pending |
| RPLY-03 | Phase 23 | Pending |
| RLBL-01 | Phase 22 | Pending |
| RLBL-02 | Phase 24 | Pending |
| RLBL-03 | Phase 24 | Pending |

**Coverage:**
- v1 requirements: 9 total
- Mapped to phases: 9
- Unmapped: 0

---
*Requirements defined: 2026-04-04*
*Last updated: 2026-04-04 after initializing v1.3 requirements*

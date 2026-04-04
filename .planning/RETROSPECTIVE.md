# Project Retrospective

*A living document updated after each milestone. Lessons feed forward into future planning.*

## Milestone: v1.2 - Controlled Automation Pipeline

**Shipped:** 2026-04-04  
**Phases:** 5 | **Plans:** 13 | **Sessions:** n/a

### What Was Built

- Controlled asset registry and effect governance with deterministic validation diagnostics.
- Repo-governed template contracts and deterministic template composition pipeline.
- Script-to-spec compiler with deterministic compile reports and stable diagnostics.
- Batch orchestration with deterministic per-job manifests, retry contracts, and explicit witness/media outputs.
- Deterministic QA gate enforcement with drift blocking and reproducible gated artifacts.

### What Worked

- Phase-by-phase contract-first implementation kept module boundaries clean.
- Fixture-driven tests made deterministic behavior regressions visible early.
- Reusing existing orchestrators prevented business-logic duplication in CLI batch flow.

### What Was Inefficient

- Workspace intermittently required serial build/test fallback, slowing verification loops.
- Milestone audit was skipped before archive, creating explicit follow-up debt.

### Patterns Established

- Batch job artifact schema (`job-manifest.json`, `summary.json`, `qa-gate-report.json`) is the canonical automation evidence surface.
- Deterministic key propagation across compile/run/gate stages is now a stable review contract.

### Key Lessons

1. Keep retry semantics explicit in manifest contracts; hidden retry logic quickly breaks auditability.
2. Gate outputs must be artifactized, not only logged, to make deterministic drift review practical.

### Cost Observations

- Model mix: n/a
- Sessions: n/a
- Notable: Most effort concentrated in deterministic artifact contracts and repeatability coverage rather than new rendering semantics.

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Sessions | Phases | Key Change |
|-----------|----------|--------|------------|
| v1.1 | n/a | 4 | Shifted from core output completeness to authored source parity evidence. |
| v1.2 | n/a | 5 | Shifted from parity to controlled automation with deterministic compile, batch, and QA gate contracts. |

### Cumulative Quality

| Milestone | Tests | Coverage | Zero-Dep Additions |
|-----------|-------|----------|-------------------|
| v1.1 | Expanded parity witness suites | Determinism-focused | Yes |
| v1.2 | Expanded compile/batch/gate suites | Determinism-focused | Yes |

### Top Lessons (Verified Across Milestones)

1. Deterministic artifacts are more durable than ad hoc runtime logs for regression and release review.
2. Keeping orchestration thin and contracts explicit reduces semantic drift across phases.

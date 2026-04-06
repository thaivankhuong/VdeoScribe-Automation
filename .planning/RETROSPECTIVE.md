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

## Milestone: v1.3 - Automation Scale and Reliability

**Shipped:** 2026-04-06  
**Phases:** 4 | **Plans:** 5 | **Sessions:** n/a

### What Was Built

- Manifest-driven throughput profiles with deterministic sequential vs bounded-parallel execution behavior.
- Deterministic preflight validation and throughput diagnostics artifacts emitted for operational auditability.
- Deterministic resume/replay recovery flows that preserve evidence lineage and avoid rerunning clean work.
- Deterministic milestone-level release witness bundle output and baseline-backed reliability promotion gating.

### What Worked

- Keeping recovery and reliability work layered on existing batch contracts avoided semantic regressions.
- Focused CLI/orchestrator test expansion caught determinism drift risks before rollout.
- Manifest-first controls kept operational behavior explicit and automation-friendly.

### What Was Inefficient

- Milestone audit was not generated before archive; this remains explicit debt.
- Tooling defaults around milestone completion required manual cleanup and curation of accomplishment summaries.

### Patterns Established

- Reliability decisions should be reflected in run-level artifacts, not inferred from console output.
- Resume/replay correctness depends on explicit lineage pointers in both summary and per-job manifests.

### Key Lessons

1. Throughput scaling can stay deterministic when ordering contracts are enforced at aggregation boundaries.
2. Recovery flows are safer when resume/replay selection is fully file-driven and replay outputs are first-class artifacts.
3. Release promotion should compare deterministic witness bundles against a baseline, not ad hoc run metadata.

### Cost Observations

- Model mix: n/a
- Sessions: n/a
- Notable: Most implementation effort was spent on deterministic evidence contracts and failure gating instead of new rendering semantics.

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Sessions | Phases | Key Change |
|-----------|----------|--------|------------|
| v1.1 | n/a | 4 | Shifted from core output completeness to authored source parity evidence. |
| v1.2 | n/a | 5 | Shifted from parity to controlled automation with deterministic compile, batch, and QA gate contracts. |
| v1.3 | n/a | 4 | Shifted from controlled automation to deterministic scale/recovery/reliability promotion contracts. |

### Cumulative Quality

| Milestone | Tests | Coverage | Zero-Dep Additions |
|-----------|-------|----------|-------------------|
| v1.1 | Expanded parity witness suites | Determinism-focused | Yes |
| v1.2 | Expanded compile/batch/gate suites | Determinism-focused | Yes |
| v1.3 | Expanded throughput/preflight/recovery/reliability suites | Determinism-focused | Yes |

### Top Lessons (Verified Across Milestones)

1. Deterministic artifacts are more durable than ad hoc runtime logs for regression and release review.
2. Keeping orchestration thin and contracts explicit reduces semantic drift across phases.

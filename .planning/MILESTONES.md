# Milestones

## v1.3 Automation Scale and Reliability (Shipped: 2026-04-06)

**Phases completed:** 4 phases, 5 plans, 0 tasks

**Key accomplishments:**

- Added manifest-driven throughput profile controls with bounded parallel execution while preserving manifest-ordered deterministic outputs.
- Added deterministic preflight validation and throughput diagnostics artifacts (`preflight-report.json`, `throughput-diagnostics.json`) for both pass/fail runs.
- Added deterministic resume/replay recovery flows that skip clean jobs, replay only failed jobs, and preserve lineage to prior evidence artifacts.
- Added release witness bundle generation and deterministic reliability promotion gating with baseline comparison and drift failure reporting.
- Expanded CLI and orchestrator test coverage so repeated runs preserve equivalent witness/gate outcomes under unchanged inputs.

**Known gaps accepted at completion:**

- No `v1.3` milestone audit document was present at archive time; completion proceeded with this audit debt explicitly recorded.

---

## v1.2 Controlled Automation Pipeline (Shipped: 2026-04-04)

**Phases completed:** 5 phases, 13 plans, 22 tasks

**Key accomplishments:**

- Registry snapshot pinning is now a first-class deterministic contract in spec ingest.
- Timeline effect behavior is now governed by a deterministic whitelist with bounded parameters.
- CLI spec loading now deterministically blocks unknown/deprecated registry pins and governed effect range violations.
- Phase 17 now has a repo-versioned template catalog plus deterministic Core validation for reusable scene template packages.
- Template contracts now instantiate into deterministic scene and timeline fragments with validated slot bindings, explicit offsets, and hashed canonical output.
- Operators can now validate and instantiate repo templates through explicit CLI commands backed by the template catalog and deterministic Core composition.
- Deterministic script JSON now resolves ordered sections into governed template instantiation plans through committed mapping and library catalogs.
- Script JSON can now be compiled into validated deterministic project specs through a thin CLI command backed by Core compilation services.
- Deterministic script compiles now emit separate audit reports with scoped diagnostics, governed resource usage, and stable CLI report artifacts for both success and failure paths.
- Ordered batch manifests now compile scripts into deterministic staged specs and then hand those specs into the existing render/export pipeline without manual spec editing.
- Deterministic batch job manifests now capture append-only retry history, compile/run outcomes, and explicit export or witness artifact keys alongside one ordered aggregate status report.

**Known gaps accepted at completion:**

- No `v1.2` milestone audit document was present at archive time; completion proceeded in YOLO mode with this audit debt explicitly recorded.

---

## v1.1 Source Parity (Shipped: 2026-04-03)

**Phases completed:** 4 phases, 11 plans, 28 tasks

**Key accomplishments:**

- Generated a deterministic authored asset inventory and locked the parity witness scene to six authored objects plus a separate hand asset.
- Promoted the authored witness spec as the active parity path, tightened asset-type validation, and proved deterministic CLI-to-Renderer handoff with repo-level integration tests.
- Locked the authored witness as the only active parity route, committed a deterministic render package for review, and added regression checks so repeated runs stay package-equivalent.
- Closed the first Phase 13 gap by making frame-state determinism transform-aware and proving the authored witness scene delivers the expected motion snapshots to Renderer.
- Closed the second Phase 13 gap by making hand guidance follow authored ordering across object types and by locking the authored witness transition sequence with renderer and CLI regression coverage.
- Closed Phase 13 by generating a committed authored motion witness package, verifying representative hand-timing frames through the real CLI/export flow, and handing the repo off to Phase 14 fidelity planning.
- Authored title, body, and footer text now regenerate as deterministic multi-path vectors with CLI-backed witness checks on the active parity route.
- The authored left illustration, arrow, and clock group now regenerate with denser vector geometry and illustration-focused CLI witness checks on the active parity path.
- Closed Phase 14 by locking representative composition targets, rerunning authored hand-order regressions, and committing a reviewable `phase14-fidelity-witness` export package.
- Built the Phase 15 review surface by adding a thin witness-export wrapper, generating a bundle manifest for the six anchor frames, and committing a fresh `phase15-review-witness` package with test coverage.
- Closed Phase 15 by committing a regression baseline for the authored witness package, adding repeated-run parity regression tests, and recording the playable-media validation gate explicitly.

---

## v1.0 Engine Core (Shipped: 2026-03-21)

**Phases completed:** 11 phases, 29 plans

**Key accomplishments:**

- Delivered deterministic spec normalization, timeline resolution, draw progression, and camera state evaluation through a strict Core -> Engine -> Renderer -> Export -> CLI pipeline.
- Expanded the pipeline from single-frame evaluation into full-timeline frame artifact generation with repeated-run and equivalent-input parity coverage.
- Added playable media encoding, audio muxing, and production-style batch validation that yields finished `.mp4` outputs per job.
- Closed major VideoScribe-like fidelity gaps with traced stroke reveal, hand-guidance overlays, real hand assets, and deterministic text rendering support.
- Established archive-ready milestone history under `.planning/milestones/` and prepared the project for a new parity-focused milestone.

**Notes:**

- The original v1.0 milestone audit was created before Phases 7-11 and is archived as a stale pre-gap-closure audit witness at `.planning/milestones/v1.0-MILESTONE-AUDIT-stale.md`.

---

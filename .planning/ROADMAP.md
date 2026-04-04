# Roadmap: whiteboard-engine

## Overview

v1.0 (engine core) and v1.1 (source parity) are shipped and archived. v1.2 focuses on controlled automation: script-to-video generation through curated asset/effect libraries, template compilation, and deterministic batch quality gates.

## Milestones

- [x] **v1.0 Engine Core** - Phases 1-11 (shipped 2026-03-21)
- [x] **v1.1 Source Parity** - Phases 12-15 (shipped 2026-04-03)
- [ ] **v1.2 Controlled Automation Pipeline** - Phases 16-20 (in planning)

## Archived Milestones

- v1.0 details: `.planning/milestones/v1.0-ROADMAP.md`
- v1.0 requirements: `.planning/milestones/v1.0-REQUIREMENTS.md`
- v1.1 details: `.planning/milestones/v1.1-ROADMAP.md`
- v1.1 requirements: `.planning/milestones/v1.1-REQUIREMENTS.md`

## Phases

- [x] **Phase 16: Controlled Asset and Effect Registry** - establish versioned asset/effect governance with strict validation. (completed 2026-04-03)
- [x] **Phase 17: Template Contracts and Scene Composition** - add reusable template contracts with deterministic slot instantiation. (completed 2026-04-04)
- [ ] **Phase 18: Script-to-Spec Compiler** - compile structured script inputs into valid deterministic project specs.
- [ ] **Phase 19: Batch Automation Orchestrator** - automate script->spec->render->export workflows with auditable manifests.
- [ ] **Phase 20: Deterministic QA Gates and Release Readiness** - enforce witness/regression gates as hard pass/fail criteria for automated output.

## Phase Details

### Phase 16: Controlled Asset and Effect Registry
**Goal**: Establish a controlled library system for assets and effects that the automation pipeline can trust.
**Depends on**: Phase 15
**Requirements**: REG-01, REG-02, REG-03, EFX-01, EFX-02
**Success Criteria** (what must be TRUE):
1. Asset/effect registry entries use stable IDs and version metadata consumable by existing spec contracts.
2. Project specs can pin registry snapshots and reproduce equivalent outputs across reruns.
3. Validation blocks unknown, deprecated, mismatched asset IDs and out-of-range effect parameters before rendering.
**Plans**: 3/3 plans complete

Plans:
- [x] 16-01: Define registry schema and snapshot/pinning contracts
- [x] 16-02: Implement controlled effect profile catalog and parameter bounds validation
- [x] 16-03: Integrate registry validation into CLI/spec ingest path with deterministic error output

### Phase 17: Template Contracts and Scene Composition
**Goal**: Enable reusable template-based scene authoring without introducing UI dependencies.
**Depends on**: Phase 16
**Requirements**: TMP-01, TMP-02
**Success Criteria** (what must be TRUE):
1. Templates define named slots and constraints in JSON contracts that are versioned with the repo.
2. Template instantiation produces deterministic scene/timeline fragments from the same slot data.
3. Composition contracts integrate with existing engine modules without violating module boundaries.
**Plans**: 1/3 plans complete

Plans:
- [x] 17-01: Define template contract model (slots, constraints, defaults)
- [x] 17-02: Implement deterministic template instantiation service
- [x] 17-03: Add contract tests for repeatability and boundary validation

### Phase 18: Script-to-Spec Compiler
**Goal**: Convert structured script/scenario input into executable project specs through controlled mappings.
**Depends on**: Phase 17
**Requirements**: CMP-01, CMP-02
**Success Criteria** (what must be TRUE):
1. CLI compiler converts structured script input into valid project specs using template and registry mappings.
2. Compile output includes auditable mapping/report artifacts for template choice, slot fills, and asset/effect selections.
3. Compile failures are deterministic and actionable with clear diagnostics.
**Plans**: 1/3 plans complete

Plans:
- [x] 18-01: Define script input contract and compiler mapping rules
- [ ] 18-02: Implement compiler pipeline from script input to project spec output
- [ ] 18-03: Emit deterministic compile reports and diagnostic contracts

### Phase 19: Batch Automation Orchestrator
**Goal**: Operationalize script-driven video generation at batch scale through CLI orchestration.
**Depends on**: Phase 18
**Requirements**: AUT-01, AUT-02
**Success Criteria** (what must be TRUE):
1. Batch jobs can run script->spec->render->export end-to-end without manual authoring edits.
2. Every job publishes deterministic manifest/status artifacts suitable for review and retry logic.
3. Failure handling and retry behavior remain deterministic and auditable.
**Plans**: 0/2 plans complete

Plans:
- [ ] 19-01: Implement batch job orchestration flow for script-driven runs
- [ ] 19-02: Add deterministic job manifest/status outputs and failure/retry contracts

### Phase 20: Deterministic QA Gates and Release Readiness
**Goal**: Lock automation quality with deterministic witness/regression gates before milestone closeout.
**Depends on**: Phase 19
**Requirements**: VAL-02
**Success Criteria** (what must be TRUE):
1. Regression/witness gates are enforced as required checks for automated output jobs.
2. Drift detection blocks successful job completion and points to reproducible evidence.
3. Milestone closes with reviewable automation witnesses across representative template/script scenarios.
**Plans**: 0/2 plans complete

Plans:
- [ ] 20-01: Integrate deterministic witness/regression gate runner into batch pipeline
- [ ] 20-02: Produce release-readiness witness package and closeout validation artifacts

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1-11 | v1.0 Engine Core | 29/29 | Complete | 2026-03-21 |
| 12-15 | v1.1 Source Parity | 11/11 | Complete | 2026-04-03 |
| 16 | v1.2 Controlled Automation Pipeline | 3/3 | Complete   | 2026-04-03 |
| 17 | v1.2 Controlled Automation Pipeline | 3/3 | Complete   | 2026-04-04 |
| 18 | v1.2 Controlled Automation Pipeline | 1/3 | In Progress|  |
| 19 | v1.2 Controlled Automation Pipeline | 0/2 | Not started | - |
| 20 | v1.2 Controlled Automation Pipeline | 0/2 | Not started | - |

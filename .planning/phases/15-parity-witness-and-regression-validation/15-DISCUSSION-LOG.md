# Phase 15: Parity Witness and Regression Validation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md - this log preserves the alternatives considered.

**Date:** 2026-03-23T14:30:53.6407483+07:00
**Phase:** 15-parity-witness-and-regression-validation
**Areas discussed:** Witness deliverables, Regression evidence scope, Review workflow surface, Media encoding validation

---

## Witness deliverables

| Option | Description | Selected |
|--------|-------------|----------|
| Export-package as canonical witness | Keep `frame-manifest.json` plus committed frame artifacts as the primary review surface | x |
| Playable video as canonical witness | Treat encoded media as the main review artifact and compare packages secondarily | |
| New bespoke witness bundle format | Introduce a new review-specific package separate from export-package outputs | |

**User's choice:** Fallback default selected in non-interactive terminal mode - export-package remains canonical.
**Notes:** This matches Phases 12-14, preserves the committed parity artifact tree, and avoids replacing the current authored witness layout right before milestone closeout.

---

## Regression evidence scope

| Option | Description | Selected |
|--------|-------------|----------|
| Package equivalence first | Repeated-run manifest/file equivalence plus representative frame assertions fail before subjective review | x |
| Visual review first | Human review drives acceptance and deterministic comparisons stay secondary | |
| New broad diff system | Add a perceptual image-diff/scoring layer as the main regression mechanism | |

**User's choice:** Fallback default selected in non-interactive terminal mode - deterministic package/frame checks stay primary.
**Notes:** Existing integration tests and export deterministic keys already support this path. The six representative frames from Phases 13-14 stay the default anchor set.

---

## Review workflow surface

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse current CLI and artifact tree | Keep `--spec`/`--output` plus repo-stored witness directories as the operator flow | x |
| Add reviewer-specific CLI mode | Introduce new commands or flags focused on review packaging | |
| Externalize review to ad-hoc notes | Rely on summaries without strengthening repo-stored review artifacts | |

**User's choice:** Fallback default selected in non-interactive terminal mode - reuse current CLI/artifact flow.
**Notes:** This keeps the phase inside its boundary and avoids turning witness validation into a separate CLI-feature phase.

---

## Media encoding validation

| Option | Description | Selected |
|--------|-------------|----------|
| Two-layer validation | Deterministic fake-runner tests always, real FFmpeg-backed witness only when env is configured | x |
| Always require real media encoding | Make every Phase 15 path depend on a real FFmpeg-backed output | |
| Ignore playable media | Validate only frame/export packages and defer final media outputs | |

**User's choice:** Fallback default selected in non-interactive terminal mode - keep two validation layers.
**Notes:** This satisfies AST-02 without making environment-specific FFmpeg availability the sole gate for planning or CI-style validation.

---

## the agent's Discretion

- Exact naming of Phase 15 witness subdirectories, summary manifests, and reviewer-facing indexes.
- Whether planning splits witness generation and regression hardening into separate plans.
- Whether a thin helper doc/script is worthwhile for reviewer guidance, as long as it consumes the existing authored witness package.

## Deferred Ideas

- Automated perceptual parity scoring or image-diff dashboards.
- Multi-scene parity witness suites beyond the current authored sample.
- Reviewer-specific CLI commands or UI surfaces.

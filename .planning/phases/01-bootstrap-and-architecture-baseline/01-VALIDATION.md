---
phase: 1
slug: bootstrap-and-architecture-baseline
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-17
---

# Phase 1 - Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET test projects) |
| **Config file** | none - uses solution/project defaults |
| **Quick run command** | `dotnet test "tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj" -v minimal` |
| **Full suite command** | `dotnet test "whiteboard-engine.sln" -v minimal` |
| **Estimated runtime** | ~120 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test "tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj" -v minimal`
- **After every plan wave:** Run `dotnet test "whiteboard-engine.sln" -v minimal`
- **Before `$gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 180 seconds

---

## Plan-Anchor Verification Map

This map tracks verification anchors per plan. Detailed task-level checks remain in each PLAN.md and are executed/expanded during phase execution.

| Anchor ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|-----------|------|------|-------------|-----------|-------------------|-------------|--------|
| 01-01-A1 | 01-01 | 1 | SPEC-01 | unit/contract | `dotnet test "tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj" -v minimal` | pending | pending |
| 01-02-A1 | 01-02 | 1 | SPEC-02 | unit/contract | `dotnet test "tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj" -v minimal` | pending | pending |
| 01-03-A1 | 01-03 | 2 | SPEC-03 | integration/contract | `dotnet test "whiteboard-engine.sln" -v minimal` | pending | pending |

*Status values: pending, green, red, flaky*

---

## Wave 0 Requirements

- [x] Existing infrastructure covers phase-level automation commands.
- [ ] Expand anchor map to full task-level matrix during execute-phase if needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Architecture docs reflect strict boundaries and deterministic rules | SPEC-01,SPEC-02,SPEC-03 | Human review of architecture intent and cross-doc consistency | Review `.planning/phases/01-bootstrap-and-architecture-baseline/*` and `docs/architecture/*` to ensure required artifacts exist and are consistent |

---

## Validation Sign-Off

- [ ] All execution tasks have automated verify or explicit Wave 0 dependency
- [x] Sampling continuity target defined (quick + full suite cadence)
- [x] Wave 0 covers baseline framework/commands
- [x] No watch-mode flags
- [x] Feedback latency target < 180s documented
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending

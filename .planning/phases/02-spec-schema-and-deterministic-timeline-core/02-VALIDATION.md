---
phase: 02
type: validation
slug: spec-schema-and-deterministic-timeline-core
status: ready
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-17
---

# Phase 02 - Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET test SDK) |
| **Config file** | `tests/*/*.csproj` |
| **Quick run command** | `dotnet test "tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj" -v minimal --filter "FullyQualifiedName~SpecProcessingPipelineTests.Ordering"` |
| **Full suite command** | `dotnet test "whiteboard-engine.sln" -v minimal` |
| **Estimated runtime** | Quick checks <= 30 seconds each; full suite ~90 seconds |

---

## Sampling Rate

- **After every task commit:** Run the task-specific filtered command from the per-task map.
- **After every plan wave:** Run one smoke command for the active wave module (`Core`, then `Engine`, then `CLI` filtered suites).
- **Before `$gsd-verify-work`:** Full suite must be green.
- **Max feedback latency:** 30 seconds.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 02-01-01 | 02-01 | 1 | TIME-03 | unit | `dotnet test "tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj" -v minimal --filter "FullyQualifiedName~SpecProcessingPipelineTests.Ordering"` | ? | ? pending |
| 02-01-02 | 02-01 | 1 | TIME-03 | unit | `dotnet test "tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj" -v minimal --filter "FullyQualifiedName~SpecProcessingPipelineTests.GateOrder|FullyQualifiedName~SpecProcessingPipelineTests.Normalization"` | ? | ? pending |
| 02-01-03 | 02-01 | 1 | TIME-03 | integration | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" -v minimal --filter "FullyQualifiedName~ProjectSpecLoader"` | ? | ? pending |
| 02-02-01 | 02-02 | 2 | TIME-01, TIME-03 | unit | `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" -v minimal --filter "FullyQualifiedName~TimelineResolverDeterminismTests.TimeToFrame"` | ? | ? pending |
| 02-02-02 | 02-02 | 2 | TIME-01, TIME-03 | unit | `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" -v minimal --filter "FullyQualifiedName~TimelineResolverDeterminismTests.Window|FullyQualifiedName~TimelineResolverDeterminismTests.TieBreak"` | ? | ? pending |
| 02-02-03 | 02-02 | 2 | TIME-01, TIME-03 | unit | `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" -v minimal --filter "FullyQualifiedName~FrameStateResolverContractTests"` | ? | ? pending |
| 02-03-01 | 02-03 | 3 | TIME-02, TIME-03 | unit | `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" -v minimal --filter "FullyQualifiedName~ObjectLifecycleResolutionTests.Contracts"` | ? | ? pending |
| 02-03-02 | 02-03 | 3 | TIME-02, TIME-03 | unit | `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" -v minimal --filter "FullyQualifiedName~ObjectLifecycleResolutionTests.Transitions|FullyQualifiedName~ObjectLifecycleResolutionTests.Conflicts"` | ? | ? pending |
| 02-03-03 | 02-03 | 3 | TIME-02, TIME-03 | integration | `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" -v minimal --filter "FullyQualifiedName~FrameStateResolverContractTests"; dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" -v minimal --filter "FullyQualifiedName~PipelineOrchestrator"` | ? | ? pending |

*Status: ? pending · ? green · ? red · ?? flaky*

---

## Wave 0 Requirements

- Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

- All phase behaviors have automated verification.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency <= 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending


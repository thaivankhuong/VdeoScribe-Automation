---
phase: 05
type: validation
slug: export-pipeline-integration
status: ready
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-19
---

# Phase 05 - Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET test SDK) |
| **Config file** | `tests/*/*.csproj` |
| **Quick run command** | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~ExportPipeline|FullyQualifiedName~PipelineOrchestratorIntegrationTests"` |
| **Full suite command** | `dotnet build "whiteboard-engine.sln" --no-restore -v minimal /m:1` |
| **Estimated runtime** | Quick checks <= 30 seconds each after build; serial build ~90-120 seconds |

---

## Sampling Rate

- **After every task commit:** Run the task-specific filtered command from the per-task map.
- **After every plan wave:** Run one serial build plus one export/CLI smoke command.
- **Before `$gsd-verify-work`:** Serial solution build and targeted deterministic suites must be green.
- **Max feedback latency:** 30 seconds after a successful build.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 05-01-01 | 05-01 | 1 | PIPE-02, PIPE-03 | unit | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~ExportPipelineContractTests"` | ? | ? pending |
| 05-01-02 | 05-01 | 1 | PIPE-02 | unit | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~ExportPipelineContractTests.FramePackaging|FullyQualifiedName~ExportPipelineContractTests.AudioPackaging"` | ? | ? pending |
| 05-01-03 | 05-01 | 1 | PIPE-02, PIPE-03 | integration | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~PipelineOrchestratorIntegrationTests"` | ? | ? pending |
| 05-02-01 | 05-02 | 2 | PIPE-02, PIPE-03 | unit | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~ExportPipelineContractTests.Repeatability|FullyQualifiedName~ExportPipelineContractTests.MissingAudio"` | ? | ? pending |
| 05-02-02 | 05-02 | 2 | PIPE-03 | integration | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~PipelineOrchestratorIntegrationTests"` | ? | ? pending |
| 05-02-03 | 05-02 | 2 | PIPE-02 | unit | `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~FrameStateResolverContractTests"` | ? | ? pending |

*Status: ? pending · ? green · ? red · ?? flaky*

---

## Wave 0 Requirements

- Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

- All phase behaviors should have automated verification; manual review is limited to checking that export package metadata remains readable and inspection-friendly.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency <= 30s after build
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending

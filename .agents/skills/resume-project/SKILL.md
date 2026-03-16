---
name: resume-project
description: Resume whiteboard-engine work by reading AI dashboard files, summarizing status, identifying the active step, planning briefly, and executing the current next task.
---

# Purpose
Resume repository work consistently from the AI dashboard by reading current context, aligning to the active step, and continuing implementation in a controlled way.

# When to Use
- When starting a new session and you need to continue `whiteboard-engine`.
- When the user asks to "resume work", "continue project", or "pick up from next task".

# Steps
1. Read `docs/ai/ai-entry.md`.
2. Read `docs/ai/project-progress.md`.
3. Read `docs/ai/next-task.md`.
4. Summarize current repository/project state.
5. Identify the current active step from `project-progress.md`.
6. Propose a short execution plan for that step.
7. Execute the task described in `next-task.md` with small, reviewable edits.
8. Report what changed and any assumptions or blockers.

# Rules
- Follow `AGENTS.md`.
- Preserve engine-first architecture and deterministic design.
- Do not introduce UI/editor scope.
- Keep changes small and reviewable.
- Prefer contract-first, placeholder-safe behavior in early phases.
- Do not modify files outside task scope unless explicitly requested.

# Expected Output
- A concise state summary.
- The identified current active step.
- A short execution plan.
- Implemented changes for the current next task.
- A brief list of changed files and validation status.

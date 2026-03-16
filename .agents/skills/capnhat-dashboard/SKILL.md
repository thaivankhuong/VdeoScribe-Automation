---
name: capnhat-dashboard
description: Update AI dashboard tracking files for whiteboard-engine, including progress, next task, and prompt snapshot, without changing source code.
---

# Purpose
Maintain dashboard continuity after a completed or partially completed step by updating progress tracking and next-step handoff artifacts.

# When to Use
- When a step is finished and dashboard docs must be updated.
- When the user asks to update progress/next task without coding changes.
- When preparing a clean handoff for the next session.

# Steps
1. Read current `docs/ai/project-progress.md`.
2. Read current `docs/ai/next-task.md`.
3. Update `docs/ai/project-progress.md`:
   - Move completed work into completed history.
   - Set the correct current active step.
   - Refresh upcoming steps if needed.
4. Update `docs/ai/next-task.md` with a clear, copy/paste-ready task prompt for the next step.
5. Save the current step prompt into `docs/ai/prompts/` using the repository naming convention.
6. Validate consistency between `project-progress.md`, `next-task.md`, and the saved prompt.

# Rules
- Follow `AGENTS.md`.
- Preserve engine-first architecture and deterministic direction in wording.
- Avoid UI/editor scope in prompts and dashboard updates.
- Do not modify source code.
- Modify only:
  - `docs/ai/project-progress.md`
  - `docs/ai/next-task.md`
  - `docs/ai/prompts/`
- Keep updates minimal, explicit, and reviewable.

# Expected Output
- Updated `docs/ai/project-progress.md`.
- Updated `docs/ai/next-task.md`.
- One saved prompt file in `docs/ai/prompts/`.
- A short summary of what was updated and why.

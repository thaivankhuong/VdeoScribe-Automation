---
name: tiep-tuc-step
description: Continue the current active whiteboard-engine step by reading progress, locating the matching prompt in docs/ai/prompts, and executing safely.
---

# Purpose
Continue the currently active step using the repository prompt library, with safe scope control and deterministic architecture constraints.

# When to Use
- When `project-progress.md` already defines an active step and you need to continue it.
- When prompts are managed in `docs/ai/prompts/` and execution should follow the matching prompt.
- When the user asks to "continue current step" or "execute active step".

# Steps
1. Read `docs/ai/project-progress.md`.
2. Identify the current active step number/title.
3. Locate the corresponding prompt file in `docs/ai/prompts/`.
4. Read the prompt and extract scope, constraints, and expected outputs.
5. Execute the step in small, reviewable changes.
6. Report implemented work, assumptions, and validation results.

# Rules
- Follow `AGENTS.md`.
- Preserve engine-first architecture and deterministic behavior.
- Do not add UI/editor scope.
- Keep changes small and reviewable.
- Respect prompt-defined scope and module boundaries.
- Do not modify files outside intended step scope unless explicitly requested.

# Expected Output
- The identified active step and matching prompt path.
- A short execution plan aligned to that prompt.
- Completed in-scope edits for the step.
- A concise summary of changed files and verification status.

Use the attached project structure and notes as the source of truth.

Task:
Create the initial repository scaffold for a project named `whiteboard-engine`.

Goals:
- Create only the folder structure and starter markdown/config files.
- Do not implement application code yet.
- Do not add UI/editor features.
- Focus on Codex project setup, docs, and reusable skills.

Required structure:
- .codex/config.toml
- .agents/skills/engine-architect/SKILL.md
- .agents/skills/render-pipeline/SKILL.md
- .agents/skills/svg-draw-animation/SKILL.md
- .agents/skills/ffmpeg-export/SKILL.md
- AGENTS.md
- README.md
- docs/01-project-scope.md
- docs/02-architecture.md
- docs/03-project-rules.md
- docs/04-rendering-pipeline.md
- docs/05-video-spec.md
- docs/06-roadmap.md
- prompts/codex-bootstrap.md
- prompts/codex-task-engine-core.md
- prompts/codex-task-svg-animation.md
- src/Whiteboard.Core/
- src/Whiteboard.Engine/
- src/Whiteboard.Renderer/
- src/Whiteboard.Export/
- src/Whiteboard.Cli/
- tests/Whiteboard.Core.Tests/
- tests/Whiteboard.Engine.Tests/
- tests/Whiteboard.Renderer.Tests/
- assets/svg/
- assets/hands/
- assets/fonts/
- assets/audio/

Instructions:
1. Create the folders exactly as specified.
2. Create starter files with concise placeholder content.
3. In AGENTS.md, define the project mission, current phase, architecture rules, coding rules, and delivery rules.
4. In each SKILL.md, include valid YAML front matter with `name` and `description`.
5. Keep all markdown concise and architecture-focused.
6. Do not generate business logic or rendering code yet.
7. Keep the setup ready for a .NET-based whiteboard video engine inspired by VideoScribe core behavior.

Output:
- Show the created tree
- Then show the content of AGENTS.md
- Then show the content of the 4 SKILL.md files


This phase is repository bootstrap only. No implementation code, no NuGet packages, no rendering logic yet.
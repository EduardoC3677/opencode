---
name: init
description: Generate or update OpenCode customization files for AI coding agents
compatibility: opencode
---
The purpose of this command is to create or update OpenCode customization files:
- `AGENTS.md` for project-wide instructions
- `.opencode/agents/` for custom agents
- `.opencode/commands/` for reusable slash commands
- `.opencode/skills/` for reusable skills

If the user provides an argument, focus on that area or workflow. Only create or modify customization files; do not start implementing the requested feature itself.

Use the related skill `agent-customization` for detailed information about the supported file types.

## Workflow

1. **Discover existing conventions**
   Search: `**/{AGENTS.md,CLAUDE.md,.cursorrules,.windsurfrules,.clinerules,.cursor/rules/**,.windsurf/rules/**,.clinerules/**,README.md,CONTRIBUTING.md,ARCHITECTURE.md}`

2. **Explore the codebase**
   Find the information future agents need most:
   - build, lint, and test commands
   - architecture and boundaries
   - project-specific conventions
   - environment/setup gotchas
   - key examples worth linking instead of duplicating

3. **Generate or merge**
   - Prefer `AGENTS.md` over legacy compatibility instruction files.
   - Keep `AGENTS.md` concise and link to deeper docs when possible.
   - Put reusable workflows in `.opencode/commands/` or `.opencode/skills/`.
   - Put specialized personas/workers in `.opencode/agents/`.

4. **Iterate**
   - Ask for feedback on unclear or incomplete sections.
   - For large workspaces, suggest splitting conventions into multiple linked docs and referencing them from `opencode.json` `instructions` if appropriate.

When complete, print a compact table of added or modified customization files and why each one is useful.

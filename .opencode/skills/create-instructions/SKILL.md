---
name: create-instructions
description: Create project rules in AGENTS.md or reusable instruction sources referenced from opencode.json.
compatibility: opencode
---
Related skill: `agent-customization`. Read `./references/instructions.md` for OpenCode rule and instruction-file guidance.

Guide the user to create or update:
- `AGENTS.md` for project-wide rules, or
- reusable markdown files that are referenced via the `instructions` array in `opencode.json`.

## Extract from Conversation
Review the conversation for durable project rules such as:
- coding conventions
- required build/test commands
- architecture constraints
- repository-specific gotchas
- file-specific guidance already documented elsewhere

## Clarify if Needed
If the target scope is unclear, ask:
- Should this rule apply to the whole project or only certain files?
- Should it live directly in `AGENTS.md` or in a referenced markdown file?
- Is it a hard requirement or a preference?

## Authoring Rules
- Put universally relevant rules in `AGENTS.md`.
- Keep `AGENTS.md` concise; link to detailed docs when possible.
- Use `opencode.json` -> `instructions` for reusable markdown files or glob patterns.
- Do not invent a `.instructions.md` file format; OpenCode's documented rule surface is `AGENTS.md` plus `instructions` entries.

## Iterate
1. Draft the rule content.
2. Choose the correct destination (`AGENTS.md` or a referenced markdown file).
3. Verify the guidance is specific, durable, and not duplicated elsewhere.
4. Summarize what the rule enforces and where it was saved.

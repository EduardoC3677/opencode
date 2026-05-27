---
name: create-prompt
description: Create a reusable OpenCode command in .opencode/commands/<name>.md for a common task.
compatibility: opencode
---
This skill keeps the legacy `create-prompt` name for compatibility, but the official OpenCode equivalent of a legacy prompt file is a custom command.

Related skill: `agent-customization`. Read `./references/prompts.md` for the command template and command frontmatter rules.

Guide the user to create a reusable command file in `.opencode/commands/`.

## Extract from Conversation
Identify repeatable tasks and extract:
- the core task
- expected arguments or placeholders
- the desired output format
- whether the command should run a specific agent or the current one

## Clarify if Needed
If the pattern is unclear, ask:
- What task should the command automate?
- Does it need arguments like `$ARGUMENTS`, `$1`, `$2`?
- Should it call a specific agent or run in the current agent context?

## Authoring Rules
- Use markdown files in `.opencode/commands/`.
- Keep frontmatter limited to OpenCode command fields such as `description`, `agent`, `subtask`, and `model`.
- Put the reusable prompt template in the markdown body.
- Use file references (`@path`), shell injections (`!\`command\``), and arguments only when they materially improve the command.

## Iterate
1. Draft the command file.
2. Test the command shape mentally with a sample invocation.
3. Ensure the filename is a good slash-command name.
4. Summarize what the command does and how to invoke it.

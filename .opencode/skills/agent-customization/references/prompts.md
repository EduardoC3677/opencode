# OpenCode commands (`.opencode/commands/*.md`)

OpenCode's reusable prompt primitive is the custom command.

## Minimal template

```md
---
description: Run tests with coverage
agent: build
model: azure-foundry/gpt-5.4
---

Run the full test suite with coverage report and show failures.
```

## Supported patterns

- `$ARGUMENTS` or `$1`, `$2`, ... for arguments
- `@path/to/file` to include file contents
- `!` shell injection for command output in the prompt
- `agent` to target a specific agent
- `subtask: true` to force isolated execution

## Guidance

- Put command files in `.opencode/commands/` or `~/.config/opencode/commands/`.
- The filename becomes the slash-command name.
- Treat this as the OpenCode replacement for legacy `.prompt.md` files.
- Keep commands focused on one repeatable workflow.

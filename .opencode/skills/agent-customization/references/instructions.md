# OpenCode instructions and additional rule files

OpenCode uses two documented rule mechanisms:

1. `AGENTS.md` for the primary project rules
2. `instructions` in `opencode.json` for additional markdown files, globs, or remote URLs

## Example

```json
{
  "$schema": "https://opencode.ai/config.json",
  "instructions": [
    "CONTRIBUTING.md",
    "docs/guidelines.md",
    ".cursor/rules/*.md"
  ]
}
```

## Guidance

- Put universally relevant rules in `AGENTS.md`.
- Use `instructions` for modular rule documents.
- Referenced files can be local paths, glob patterns, or remote URLs.
- Do not create a fake `.instructions.md` file convention for OpenCode; it is not the documented primary format.

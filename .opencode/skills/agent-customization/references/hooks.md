# Hooks and enforcement in OpenCode

OpenCode does not document legacy standalone hook JSON files as a primary project customization surface.

## Recommended replacements

- `permission` rules in `opencode.json`
- custom commands in `.opencode/commands/`
- wrapper scripts invoked by commands or agents
- plugins or MCP servers for advanced automation
- explicit procedural rules in `AGENTS.md`

## Example: block edits by default

```json
{
  "$schema": "https://opencode.ai/config.json",
  "permission": {
    "edit": "deny",
    "bash": {
      "*": "ask",
      "rm -rf *": "deny"
    }
  }
}
```

## Example: require a validation workflow

Create a command like `.opencode/commands/validate.md` and instruct contributors to run it before merge.

## Guidance

If you need deterministic lifecycle automation, document the exact limitation and choose the narrowest OpenCode-native replacement instead of carrying over legacy hook syntax unchanged.

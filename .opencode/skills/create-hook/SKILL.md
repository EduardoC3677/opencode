---
name: create-hook
description: Emulate legacy hook behavior in OpenCode using permissions, commands, scripts, or plugins.
compatibility: opencode
---
OpenCode does not use legacy standalone hook JSON formats as a documented first-class project primitive.

Related skill: `agent-customization`. Read `./references/hooks.md` for the recommended OpenCode alternatives.

Guide the user to implement the closest OpenCode-native equivalent using one of these patterns:
- `permission` rules in `opencode.json`
- explicit validation commands in `.opencode/commands/`
- wrapper shell scripts invoked by commands or agents
- plugins or MCP services when real automation is required

## Extract from Conversation
Look for repeated policy needs such as:
- blocking dangerous commands
- always running validation before edits
- injecting standard context
- orchestrating repeatable setup or cleanup work

## Clarify if Needed
If the enforcement point is unclear, ask:
- Is this a safety rule, a convenience shortcut, or real automation?
- Does it need to run before a tool call, before a workflow, or as a reusable command?
- Can it be expressed as permissions instead of imperative automation?

## Iterate
1. Choose the narrowest OpenCode-native mechanism.
2. Draft the config, command, or script.
3. Document the limitation if exact hook parity is impossible.
4. Summarize how the replacement works and how to test it.

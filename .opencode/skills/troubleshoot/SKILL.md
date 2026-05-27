---
name: troubleshoot
description: Investigate OpenCode configuration, agent, command, skill, provider, model, MCP, and permission issues using direct evidence.
compatibility: opencode
---
# Troubleshoot OpenCode behavior

Use this skill when the user asks why OpenCode behaved a certain way, why a model/provider did not load, why an agent/skill/command was ignored, why an MCP server failed, or why a tool call was blocked.

## Core rules

- Base conclusions on evidence from config, logs, and reproducible commands.
- Do not rely on source-specific tools, editor-only command runners, or undocumented hidden APIs.
- Prefer official OpenCode docs and the checked-in project files.
- Never claim a fix worked until the relevant config parses and the runtime surface is verifiable.

## Investigation checklist

1. Check `opencode.json` first.
   - Validate JSON syntax.
   - Confirm `$schema` points to `https://opencode.ai/config.json`.
   - Check `model`, `provider`, `agent`, `mcp`, `permission`, and `instructions`.

2. Check project rules and runtime files.
   - `AGENTS.md`
   - `.opencode/agents/*.md`
   - `.opencode/commands/*.md`
   - `.opencode/skills/*/SKILL.md`

3. For agent/command/skill loading issues, verify naming and placement.
   - Agents must live in `.opencode/agents/`.
   - Commands must live in `.opencode/commands/`.
   - Skills must live in `.opencode/skills/<name>/SKILL.md` and the `name` field must match the folder name.

4. For provider/model issues, verify:
   - provider id matches the `provider/model` prefix
   - required environment variables are present
   - the configured base URL and model id are correct for the provider
   - model capabilities (tool calls, reasoning, limits) are defined sensibly when using a custom provider

5. For MCP issues, verify:
   - each server sits under the `mcp` key
   - `type` is `local` or `remote`
   - local servers use a valid `command` array
   - remote servers use a valid `url`
   - required credentials or headers exist
   - `enabled` matches the intended state

## Recommended verification commands

- `jq . opencode.json`
- `opencode agent list`
- `opencode mcp list`
- `opencode providers list`
- `opencode models <provider>` when the provider is available

## Documentation references

Use official OpenCode docs when format expectations are unclear:

- Rules: `https://opencode.ai/docs/rules`
- Agents: `https://opencode.ai/docs/agents`
- Commands: `https://opencode.ai/docs/commands`
- Skills: `https://opencode.ai/docs/skills`
- Providers: `https://opencode.ai/docs/providers`
- Models: `https://opencode.ai/docs/models`
- MCP servers: `https://opencode.ai/docs/mcp-servers`

## Response style

Explain:
- what happened
- the evidence
- the concrete fix
- any manual credential or environment step still required

Keep the answer user-facing and actionable. Do not dump raw internal log formats unless the user explicitly asks for them.

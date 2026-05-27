# OpenCode workspace normalization summary

This folder is aligned to the documented OpenCode project layout and normalized around an Azure AI Foundry default model.

## Final layout

Required root entrypoints:
- `opencode.json`
- `AGENTS.md`

Project-local active OpenCode assets:
- `.opencode/instructions/*.md`
- `.opencode/agents/*.md`
- `.opencode/commands/*.md`
- `.opencode/skills/<name>/SKILL.md`
- `migration-archive/` for non-runtime traceability artifacts

## Canonical mappings

- Legacy agent YAML definitions -> `.opencode/agents/*.md`
- Legacy `.prompt.md` files -> `.opencode/commands/*.md`
- Legacy instruction files -> `AGENTS.md` plus `.opencode/instructions/*.md` referenced from `opencode.json.instructions`
- Legacy `SKILL.md` packages -> `.opencode/skills/<name>/SKILL.md`
- Authoring reference docs -> `.opencode/skills/agent-customization/references/*.md`

## Normalization decisions

- The legacy configure command was renamed to `configure-opencode`.
- Prompt files were not kept under `.opencode/prompts/` because OpenCode's official reusable prompt primitive is the custom command in `.opencode/commands/`.
- Additional instruction markdown was moved under `.opencode/instructions/` and is loaded explicitly through `opencode.json.instructions`; the root `AGENTS.md` remains because OpenCode's rules entrypoint is project-root `AGENTS.md`.
- Source-traceability files were moved into `migration-archive/` and are not presented as active runtime config.
- Source-specific tool semantics such as `mcp_validate`, `mcp_reload`, background inbox flows, and session-store SQL were replaced by OpenCode-native patterns or explicit limitation notes.
- GitHub review automation was normalized to a non-interactive workflow that loads the root `opencode.json` and the `.opencode/` customization tree directly.

## Active model/provider target

- Default runtime model -> `azure-foundry/gpt-5.4`
- Custom provider id -> `azure-foundry`
- Built-in and custom agents/commands were normalized to `azure-foundry/gpt-5.4`
- MCP servers configured -> `context7` (enabled) and `github` (enabled, authenticated from `GITHUB_TOKEN`)
- Extra instructions loaded via `opencode.json.instructions`:
  - `.opencode/instructions/project-rules.md`
  - `.opencode/instructions/runtime-instructions.md`
  - `.opencode/instructions/customization-surfaces.md`

## Verification target

The checked-in runtime surface should match the current OpenCode docs:
- Config: `https://opencode.ai/config.json`
- Rules: `https://opencode.ai/docs/rules`
- Agents: `https://opencode.ai/docs/agents`
- Commands: `https://opencode.ai/docs/commands`
- Skills: `https://opencode.ai/docs/skills`
- MCP: `https://opencode.ai/docs/mcp-servers`
- Providers: `https://opencode.ai/docs/providers`

## Schema note

The repository keeps a custom provider alias named `azure-foundry` in `provider.azure-foundry`, and the active checked-in default model is `azure-foundry/gpt-5.4` because that is accepted by the current published OpenCode schema.

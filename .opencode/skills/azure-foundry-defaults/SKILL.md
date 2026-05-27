---
name: azure-foundry-defaults
description: Follow this repository's Azure AI Foundry model, provider, permission, and MCP conventions when editing OpenCode workspace config.
compatibility: opencode
---
Use this skill when modifying the OpenCode workspace in this repository.

## Defaults

- Primary model: `azure-foundry/gpt-5.4`
- Provider id: `azure-foundry`
- Required env vars: `AZURE_FOUNDRY_BASE_URL`, `AZURE_FOUNDRY_API_KEY`
- Optional GitHub MCP token: `GITHUB_TOKEN`

## Canonical file locations

- `opencode.json`
- `AGENTS.md`
- `.opencode/agents/*.md`
- `.opencode/commands/*.md`
- `.opencode/skills/<name>/SKILL.md`
- `docs/opencode/runtime-instructions.md`

## Configuration rules

1. Keep model pins aligned across config, agents, commands, and reference examples.
2. Prefer `permission` rules instead of legacy `tools` booleans.
3. Keep `context7` enabled and `github` present but disabled until credentials exist.
4. Put extra reusable instruction files in stable paths and load them through `opencode.json.instructions`.
5. Do not put secrets in repository files.

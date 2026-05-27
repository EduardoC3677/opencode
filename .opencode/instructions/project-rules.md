# OpenCode project rules

This file contains the substantive project rules for this repository. It is loaded through `opencode.json.instructions` so that most checked-in instruction markdown can live under `.opencode/` while preserving OpenCode's required project-root entrypoints.

## Effective workspace layout

Required project-root entrypoints:
- `opencode.json` — project configuration using the official schema
- `AGENTS.md` — project rules bootstrap loaded automatically by OpenCode

Active project-local OpenCode assets under `.opencode/`:
- `.opencode/instructions/*.md` — additional instruction documents loaded via `opencode.json.instructions`
- `.opencode/agents/*.md` — custom agents
- `.opencode/commands/*.md` — reusable slash commands
- `.opencode/skills/<name>/SKILL.md` — reusable skills

## Active runtime defaults

- Default model: `azure-foundry/gpt-5.4`
- Default agent: `build`
- Built-in `build`, `plan`, `general`, `explore`, and `scout` are all pinned to `azure-foundry/gpt-5.4`
- The dedicated automation agents `implementer` and `reviewer` are pinned to `azure-foundry/gpt-5.4`
- Custom agents and commands are also pinned to `azure-foundry/gpt-5.4`
- GitHub review automation must use the root `opencode.json` plus the `.opencode/` customization surfaces
- GitHub automation runs install Node.js, Bun, ripgrep, Playwright MCP/browser prerequisites, and the OpenCode CLI, then execute `opencode run` directly from the repository root

## Azure AI Foundry provider config

The checked-in provider is a custom OpenCode provider named `azure-foundry`.

Required environment variables:
- `AZURE_FOUNDRY_BASE_URL` — full OpenAI-compatible Azure Foundry endpoint
- `AZURE_FOUNDRY_API_KEY` — Azure Foundry API key

Recommended endpoint pattern:
- `https://<resource>.openai.azure.com/openai/v1`

If your deployment endpoint differs, set `AZURE_FOUNDRY_BASE_URL` to the exact compatible base URL exposed by your Azure AI Foundry deployment.

## MCP servers configured in opencode.json

- `context7` — enabled by default via `npx -y @upstash/context7-mcp`
- `github` — enabled and authenticated from `GITHUB_TOKEN` via `GITHUB_PERSONAL_ACCESS_TOKEN`
- `playwright` — enabled for browser automation and UI verification in CI-capable environments

Additional runtime instructions are loaded through `opencode.json.instructions`:
- `.opencode/instructions/project-rules.md`
- `.opencode/instructions/runtime-instructions.md`
- `.opencode/instructions/customization-surfaces.md`

## Automation rules

- GitHub Actions issue implementation runs and PR review runs are unattended. Do not ask the PR author, issue author, user, or client for clarification, approval, or permission.
- Route opened issues and `/oc` issue comments to the dedicated `implementer` agent.
- Issue implementation must work from a dedicated non-default branch, commit the resulting changes there, and open or update a pull request back to the default branch when code changes are required.
- Pull requests created from issue automation must include a concrete description with summary, validation, linked issue context, and the commits included in the branch.
- Implementation runs must behave like senior full-stack engineering work: understand the relevant code path first, then actually modify the code instead of ending at analysis.
- Implementation runs should install or refresh project dependencies when needed to implement or validate correctly.
- Validation should include a real build, compile, test, typecheck, or browser verification step for the changed surface whenever feasible.
- Use the configured GitHub and Playwright MCP servers when they materially improve repository context or browser/UI validation.
- Route opened or updated PRs to code review.
- On PR synchronize events, code review should focus first on the newly pushed commits or incremental diff before falling back to the full PR diff.
- Route `/oc` comments on PR threads or PR review comments to implementation work on the PR branch when possible, and commit the resulting changes on that branch.
- If a `/oc` implementation request targets a fork PR without write access to the branch, explain the limitation clearly and provide a concrete patch or next step instead of blocking.
- The workflow should not rely on `uses: anomalyco/opencode/github@latest`; it should prepare the toolchain and call `opencode run` directly with the routed agent and generated prompt.
- Use direct `allow` permissions for the tools required by CI automation flows.
- When ambiguity remains during automation, make the safest reasonable assumption, state it if needed, and continue.
- Use high-signal review output only: bugs, regressions, security issues, broken assumptions, concrete missing coverage, or other material risks.
- Do not produce style-only or speculative code review noise.

## Migration rules

- Treat `.opencode/agents/*.md` as the canonical replacement for legacy `.agent.yaml` / `.agent.md` style definitions.
- Treat `.opencode/commands/*.md` as the canonical replacement for legacy `.prompt.md` slash prompts.
- Treat `AGENTS.md` plus optional `instructions` entries in `opencode.json` as the canonical replacement for legacy instruction files.
- Keep skill packages in `.opencode/skills/<name>/SKILL.md` with lowercase hyphenated names.
- Prefer `permission` rules over deprecated `tools` booleans when authoring agents.
- Keep `description` values short, keyword-rich, and specific so OpenCode can discover the right agent or skill.

## Chronicle compatibility note

The original Chronicle prompts depended on source-specific session-store tooling. In OpenCode, the migrated chronicle agents and commands should work from:
- repository history
- exported chat/session logs
- local notes and standup files
- task documents supplied by the user

If that data is missing, the agent should say so explicitly instead of pretending session telemetry exists.

## Schema note

The repository keeps a custom provider alias named `azure-foundry` in `provider.azure-foundry`, and the active checked-in default model is `azure-foundry/gpt-5.4` because that is accepted by the current published OpenCode schema.

## Customization references

For the current canonical mapping between prompts, instructions, commands, agents, and skills, see `.opencode/instructions/customization-surfaces.md`.

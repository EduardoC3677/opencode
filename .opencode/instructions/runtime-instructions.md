# Runtime instructions

This repository is pinned to `azure-foundry/gpt-5.4` through the checked-in provider configuration.

## Model and provider defaults
- Keep the default model on `azure-foundry/gpt-5.4` unless the user explicitly asks for another model.
- Resolve credentials from environment variables, never from checked-in secrets.
- Use the available GitHub token to authenticate repository MCP access when present.
- Use browser MCP tooling when frontend validation or browser interaction matters.

## Repository layout
- The root rules file defines repository-wide behavior.
- The root configuration file defines providers, models, permissions, instructions, and MCP servers.
- Project-local instructions, agents, commands, and skills live in the checked-in customization directories.

## Tools and permissions
- Prefer explicit permission rules over deprecated tool booleans.
- This repository is configured for unattended automation: do not ask the user, client, or reviewer for permission during automation runs.
- Keep clarifying questions disabled for automation paths and rely on direct allow rules for the tools the repository needs.
- Keep read, search, edit, shell, repository, and MCP-backed capabilities available.
- Allow access to external directories when required to inspect or validate related project material.
- Allow github, playwright, and context7 MCP capabilities by default.

## Automation behavior
- Repository automation must load the checked-in root configuration.
- Repository automation must use the checked-in instructions, agents, commands, and skills.
- Before execution, automation must install Node.js, Bun, ripgrep, browser prerequisites, and the repository CLI toolchain required by the configured agents.
- Automation is non-interactive: never block waiting for clarification from a human.
- Issue-driven implementation should route to the dedicated implementation agent.
- Implementation must create or update a dedicated non-default branch, commit changes there, and open or update a pull request when code changes are required.
- Pull-request descriptions must include summary, validation, linked issue context, and included commits.
- Implementation must understand the relevant code path before editing, then actually implement the requested change.
- Implementation should install or refresh project dependencies when needed.
- Validation should include a real build, compile, test, typecheck, or browser-verification step whenever feasible.
- Updated pull requests should route to review.
- Incremental review should focus first on newly pushed commits or the narrowest relevant diff.
- If a branch belongs to a fork or otherwise lacks write access, explain the limitation clearly and provide a concrete patch or next step.

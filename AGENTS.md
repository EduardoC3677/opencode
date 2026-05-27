# Project bootstrap

This file is the root bootstrap for repository-wide agent behavior.

Core defaults:
- default model: `azure-foundry/gpt-5.4`
- keep credentials in environment variables only
- keep project-local agents, commands, skills, and instruction documents under the checked-in customization directories
- use the root configuration file as the single checked-in entrypoint for runtime config

Repository automation rules:
- Automation runs are non-interactive.
- Agents, prompts, instructions, and scripts must not ask the issue author, reviewer, user, or operator for clarification, approval, or permission during automation.
- Issue implementation runs must create or update a dedicated non-default branch, commit changes there, and open or update a pull request back to the default branch when code changes are required.
- Pull requests opened by automation must include a concrete description with summary, validation, linked issue context, and the commits included in the branch.
- Implementation agents must behave like senior full-stack engineers: understand the relevant code path before editing, then implement the requested change instead of stopping at analysis.
- Automation must install or refresh project dependencies when needed for implementation or validation, and should run a real build, compile, test, typecheck, or browser verification step whenever feasible.
- Automation should use the configured MCP servers when they materially improve repository context or browser or UI validation.
- Opened or updated pull requests should route to review.
- Incremental review should focus first on newly pushed commits or the narrowest relevant diff.
- If a requested branch is not writable, automation must explain the limitation clearly and provide a concrete patch or next step.
- When a decision is needed during automation, choose the safest reasonable default and continue.

# OpenCode workspace bootstrap

OpenCode requires `AGENTS.md` at the project root for automatic project-rule loading.

This repository keeps the detailed runtime guidance under `.opencode/instructions/` and loads those files through `opencode.json.instructions`.

Active instruction files:
- `.opencode/instructions/project-rules.md`
- `.opencode/instructions/runtime-instructions.md`
- `.opencode/instructions/customization-surfaces.md`

Required root entrypoints:
- `AGENTS.md`
- `opencode.json`

Core defaults:
- model: `azure-foundry/gpt-5.4`
- keep credentials in environment variables only
- keep active agents, commands, and skills under `.opencode/`
- use the repository-root `opencode.json` as the checked-in config entrypoint
- treat this file as the bootstrap and keep substantive rules in `.opencode/instructions/`

Automation rule:
- GitHub Actions issue implementation runs and PR review runs are non-interactive.
- Agents, prompts, instructions, and workflow config must not ask the PR author, issue author, or user for clarification, approval, or permission during CI runs.
- On opened issues and `/oc` comments on issues, the workflow should route to a dedicated implementation agent instead of the built-in `build` agent.
- Issue implementation runs must create or update a dedicated non-default branch, commit the resulting changes there, and open or update a pull request back to the default branch when code changes are required.
- Pull requests opened from issue automation must include a concrete PR description summarizing the implementation, validation, linked issue, and the commits included in the branch.
- The implementation agent must behave like a senior full-stack engineer: understand the relevant code path before editing, then actually implement the requested change instead of stopping at analysis.
- The automation must install or refresh project dependencies when needed for implementation or validation, and should run a real build, compile, test, or equivalent verification step for the changed surface whenever feasible.
- The automation should use the configured GitHub and Playwright MCP servers when they materially improve repository context or browser/UI validation.
- On opened or updated PRs, the workflow should route to code review.
- On PR synchronize events, review should focus first on the newly pushed commits or incremental diff before expanding to the full PR context when needed.
- On `/oc` comments in PR threads or review comments, the workflow should route to implementation work on the PR branch when possible, commit the resulting changes on that PR branch, and rely on the follow-up PR review run to review the new commits.
- If a `/oc` implementation request targets a fork PR without branch write permission, automation must explain the limitation clearly and provide a concrete patch or next step instead of blocking.
- The GitHub Actions workflow must install Node.js, Bun, ripgrep, Playwright MCP/browser prerequisites, and the OpenCode CLI before execution, then invoke `opencode run` directly instead of `uses: anomalyco/opencode/github@latest`.
- The automation prompt and agent instructions must assume this direct CLI execution path and the installed toolchain.
- When a decision is needed during automation, choose the safest reasonable default and continue.
- The workflow must load the root `opencode.json` plus the `.opencode/` agents, commands, skills, and instruction files.

If any instruction path changes, update `opencode.json.instructions` so the `.opencode/instructions/*.md` files continue to load.

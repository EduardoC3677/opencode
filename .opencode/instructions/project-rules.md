# Project rules

This file contains the substantive repository rules.

## Active defaults
- Default model: `azure-foundry/gpt-5.4`
- Default agent: `build`
- Built-in primary and subagents in this repository are pinned to `azure-foundry/gpt-5.4`
- Repository automation must use the checked-in root configuration plus the checked-in customization files

## Provider and environment
- The checked-in provider alias is `azure-foundry`.
- Required environment variables must stay outside the repository.
- Browser and repository MCP servers should be enabled when their credentials or dependencies are present.

## MCP servers
- `context7` is enabled for documentation lookup.
- `github` is enabled for repository and pull-request context.
- `playwright` is enabled for browser automation and UI verification.

## Automation rules
- Automation runs are unattended. Never ask the issue author, reviewer, user, or operator for clarification, approval, or permission.
- Route issue implementation and implementation comments to the dedicated implementation agent.
- Implementation must work from a dedicated non-default branch, commit changes there, and open or update a pull request back to the default branch when code changes are required.
- Pull requests created by automation must include summary, validation, linked issue context, and the commits included in the branch.
- Implementation must behave like senior full-stack engineering work: understand the relevant code path first, then modify the code instead of ending at analysis.
- Implementation should install or refresh project dependencies when needed to implement or validate correctly.
- Validation should include a real build, compile, test, typecheck, or browser verification step for the changed surface whenever feasible.
- Use the configured MCP servers when they materially improve repository context or browser or UI validation.
- Route opened or updated pull requests to review.
- On synchronize-style review runs, focus first on newly pushed commits or the incremental diff before expanding to the full pull request.
- If an implementation request targets a branch without write access, explain the limitation clearly and provide a concrete patch or next step.
- Use direct allow permissions for the tools required by automation flows.
- When ambiguity remains, make the safest reasonable assumption, state it if needed, and continue.
- Review output must stay high-signal: bugs, regressions, security issues, broken assumptions, concrete missing coverage, or other material risks.
- Do not produce style-only or speculative review noise.

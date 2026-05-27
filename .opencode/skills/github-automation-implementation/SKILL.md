---
name: github-automation-implementation
description: Deterministic repository automation for issue implementation, branch commits, pull-request creation, and incremental review
---
Use this skill when editing the repository's GitHub automation for issues, pull requests, and implementation comments.

## Goals
- Run repository automation by installing the required toolchain and invoking the repository CLI directly.
- Route issue implementation to a dedicated implementation agent instead of the default general-purpose agent.
- Ensure opened issues and implementation comments create or reuse a dedicated non-default branch.
- Require implementation runs to commit the resulting changes and open or update a pull request back to the default branch when code changes are required.
- Require automation-created pull requests to include a concrete description with summary, validation, linked issue context, and included commits.
- Ensure implementation comments on pull requests commit on the pull-request head branch when write access exists.
- Ensure incremental review focuses first on newly pushed commits or the narrowest relevant diff.
- If the branch is not writable, require a clear limitation message plus a concrete patch or next step.

## Recommended branch rule
- Use a deterministic issue branch name such as `automation/issue-<number>-<slug>`.
- Reuse that same branch for the original issue run and later implementation comments so automation does not fan out into multiple competing branches.

## Prompt requirements
For implementation prompts:
1. State that the run is non-interactive.
2. State that the issue title, issue body, and trigger comment are the source of truth.
3. Require branch creation or reuse, validation, commit, push, and pull-request creation or update.
4. Require a concrete pull-request body with summary, validation, linked issue context, and included commits.
5. Require the agent to understand the relevant code path before editing and to install dependencies when needed.

For review prompts:
1. Review high-signal issues only.
2. Inspect the incremental range first when a previous head SHA is available.
3. Expand to the full pull-request diff only when necessary.
4. Keep comments concrete and evidence-based.

## Files to update together
- root rules file
- root configuration file
- checked-in instruction documents
- checked-in agents
- checked-in commands
- the checked-in automation definition under `.github/`

## Runtime requirements
When editing repository automation for this repository:
1. Set up Node.js before running the repository CLI.
2. Set up Bun before running the repository CLI.
3. Install `ripgrep` in the runner image.
4. Install browser prerequisites and MCP-related dependencies required by the configured agents.
5. Install the repository CLI at the pinned version and put its bin directory on `PATH`.
6. Run `swarm setup` and `swarm doctor` before the main automation step.
7. Keep the root configuration file selected explicitly when reproducibility matters.

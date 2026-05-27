---
name: github-automation-implementation
description: Deterministic OpenCode GitHub automation for issue implementation, PR branch commits, PR creation, and incremental PR review
compatibility: opencode
---
Use this skill when editing the repository's OpenCode GitHub automation for issues, pull requests, and `/oc` comments.

## Goals
- Run GitHub Actions automation by installing Node.js, Bun, ripgrep, and the OpenCode CLI, then invoking `opencode run` directly instead of the hosted GitHub Action wrapper.
- Route issue implementation to a dedicated `implementer` agent instead of the built-in `build` agent.
- Ensure opened issues and `/oc` issue comments create or reuse a dedicated non-default branch.
- Require implementation runs to commit the resulting changes and open or update a pull request back to the default branch when code changes are required.
- Require automation-created PRs to include a concrete description with summary, validation, and linked issue context.
- Ensure `/oc` comments on PRs commit on the PR head branch when write access exists.
- Ensure PR synchronize review focuses first on the newly pushed commits or incremental diff.
- If the PR comes from a fork without write access, require a clear limitation message plus a concrete patch or next step.

## Recommended branch rule
- Use a deterministic issue branch name such as `automation/issue-<number>-<slug>`.
- Reuse that same branch for the original issue-open run and later `/oc` issue comments so automation does not fan out into multiple competing branches.

## Prompt requirements
For issue implementation prompts:
1. State that the run is non-interactive.
2. State that the issue title/body and trigger comment are the source of truth.
3. Require branch creation or reuse, validation, commit, push, and PR creation/update.
4. Require the PR to target the repository default branch, never the default branch itself as the working branch.
5. Require a concrete PR body with summary, validation, and linked issue context.

For PR `/oc` prompts:
1. State that the run is non-interactive.
2. Require work on the checked-out PR head branch.
3. Require committing the resulting changes on that branch when write access exists.
4. If write access does not exist, require an explicit limitation notice plus a patch or next step.
5. Note that the subsequent PR synchronize review should evaluate the newly pushed commits.

For PR review prompts:
1. Review high-signal issues only.
2. If a previous PR head SHA is available, inspect the incremental range first.
3. Expand to the full PR diff only when necessary.
4. Keep comments concrete and evidence-based.

## Files to update together
- `AGENTS.md`
- `opencode.json`
- `.opencode/instructions/*.md`
- `.opencode/agents/*.md`
- `.opencode/commands/*.md`
- `.github/workflows/opencode.yml`

## Workflow runtime requirements
When editing `.github/workflows/opencode.yml` for this repository:
1. Set up Node.js before running OpenCode.
2. Set up Bun before running OpenCode.
3. Install `ripgrep` in the runner image.
4. Install the OpenCode CLI in the runner and put its bin directory on `PATH`.
5. Invoke `opencode run --model ... --agent ...` directly with the generated prompt.
6. Keep `OPENCODE_CONFIG` pointed at the repository-root `opencode.json`.

# OpenCode customization surfaces

This file documents the valid OpenCode surfaces for this workspace and the schema-backed alternatives for instructions, prompts, agents, commands, and skills.

## What is valid inside `.opencode/`

According to the current OpenCode docs, the `.opencode/` directory uses plural subdirectories for project-local discovered assets such as:
- `.opencode/agents/`
- `.opencode/commands/`
- `.opencode/modes/`
- `.opencode/plugins/`
- `.opencode/skills/`
- `.opencode/tools/`
- `.opencode/themes/`

This repository actively uses:
- `.opencode/agents/*.md`
- `.opencode/commands/*.md`
- `.opencode/skills/<name>/SKILL.md`
- `.opencode/instructions/*.md`

Important: `.opencode/instructions/*.md` is a repository convention used here for organization. It is valid because `opencode.json.instructions` accepts local markdown file paths anywhere in the repo. It is not an auto-discovered built-in surface like `.opencode/agents/`, `.opencode/commands/`, or `.opencode/skills/`.

## What must remain at project root

The current OpenCode docs also say:
- per-project config lives in `opencode.json` at the project root
- project rules live in `AGENTS.md` at the project root

Those two files stay at the root because OpenCode looks for them there automatically. In this repository, they act as the required root entrypoints while most additional instruction markdown lives under `.opencode/`.

## Canonical local file locations

- `opencode.json`
  - project configuration
  - provider and model definitions
  - permissions
  - MCP server configuration
  - optional inline command and agent config
  - additional instruction sources
- `AGENTS.md`
  - primary project rules entrypoint loaded automatically
- `.opencode/agents/*.md`
  - custom agents
  - frontmatter + markdown body prompt
- `.opencode/commands/*.md`
  - reusable slash commands
  - canonical OpenCode replacement for legacy prompt files
- `.opencode/skills/<name>/SKILL.md`
  - reusable procedural skills
- `.opencode/instructions/*.md`
  - repository-local instruction docs loaded explicitly through `opencode.json.instructions`

## How legacy "prompt" concepts map into OpenCode

Use the following mapping when migrating or authoring files:
- project-wide prompt or instruction file
  - `AGENTS.md`
  - or a markdown file referenced from `opencode.json.instructions`
- reusable slash prompt
  - `.opencode/commands/<name>.md`
- agent system prompt / persona
  - markdown body of `.opencode/agents/<name>.md`
  - or inline `agent.<name>.prompt` in `opencode.json`
- reusable step-by-step know-how
  - `.opencode/skills/<name>/SKILL.md`

Do not invent new active OpenCode file types such as `.prompt.md` or `.agent.yaml` for new work in this repository.

## Schema-backed configuration alternatives

The current published schema supports additional configuration paths beyond the canonical local files.

### Additional instructions

`opencode.json.instructions` accepts:
- local markdown file paths
- glob patterns
- remote URLs

This workspace uses:
- `.opencode/instructions/project-rules.md`
- `.opencode/instructions/runtime-instructions.md`
- `.opencode/instructions/customization-surfaces.md`

### Additional skill sources

`opencode.json.skills` supports:
- `paths`: extra skill directories
- `urls`: remote skill indexes

These are optional. They are not required for the canonical local `.opencode/skills/` folder already used by this repository.

### Inline agent configuration

`opencode.json.agent.<name>` can define or override agent settings such as:
- `model`
- `prompt`
- `temperature`
- `top_p`
- `mode`
- `permission`
- `hidden`
- `steps`
- `color`
- `disable`

This workspace uses inline agent config for built-in agents and markdown files under `.opencode/agents/` for custom agents.

### Inline command configuration

`opencode.json.command.<name>` can define commands with:
- `template`
- `description`
- `agent`
- `model`
- `subtask`

This workspace keeps custom commands in `.opencode/commands/*.md`, which OpenCode loads and merges into the effective command config.

## GitHub Actions wiring

- GitHub Actions must run from the repository root so OpenCode can load `opencode.json` automatically.
- The workflow should explicitly point `OPENCODE_CONFIG` at the root `opencode.json` when reproducibility matters.
- The `.opencode/` directory must remain checked out so OpenCode can discover agents, commands, skills, and referenced instruction markdown.
- GitHub Actions automation in this repository installs Node.js, Bun, ripgrep, and the OpenCode CLI first, then invokes `opencode run` directly instead of relying on `uses: anomalyco/opencode/github@latest`.
- Non-interactive review workflows must deny `question` and rely on direct `allow` permissions for the tools they need.
- Keep a dedicated implementation agent under `.opencode/agents/` for unattended issue and `/oc` implementation runs instead of relying on the built-in `build` agent when deterministic automation behavior matters.
- Prefer a canonical issue branch naming rule such as `automation/issue-<number>-<slug>` so issue-open and issue-comment runs converge on the same working branch.
- When `/oc` targets a PR, the implementation route should work on the checked-out PR head branch, commit there when write access exists, and let the follow-up `pull_request` synchronize event review the newly pushed commits.
- When a PR comes from a fork without write access, prompts and agent rules should require a clear limitation message plus a concrete patch or next step instead of stalling.

## Authoring rules for this repository

- Keep `azure-foundry/gpt-5.4` consistent across the workspace unless the user explicitly requests another model.
- Keep secrets out of the repo; use environment variables.
- Prefer `permission` over deprecated `tools` booleans.
- Keep agent and command descriptions short and discovery-friendly.
- Keep skills in lowercase hyphenated directories whose names match the `name` frontmatter field.
- Review agents used in CI must assume unattended execution and must not ask for human approval.

## Discovery checklist

When something does not load in OpenCode, verify:
- the file is in the canonical directory for its surface
- the filename matches the intended command / agent / skill name
- markdown frontmatter is valid YAML
- `opencode.json` remains valid JSON
- any `instructions` path actually exists
- the workflow runs from the repository root or sets `OPENCODE_CONFIG` correctly
- required environment variables for providers and MCP servers are present

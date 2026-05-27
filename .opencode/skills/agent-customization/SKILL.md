---
name: agent-customization
description: Create, review, or troubleshoot OpenCode customization files such as AGENTS.md, opencode.json, .opencode/agents/*.md, .opencode/commands/*.md, and .opencode/skills/*/SKILL.md.
compatibility: opencode
---
Use this skill when creating or debugging OpenCode project customizations.

## Official OpenCode surfaces

| Surface | Canonical location | Purpose |
|---|---|---|
| Project config | `opencode.json` | models, permissions, providers, MCP, instructions |
| Project rules | `AGENTS.md` | project-wide instructions loaded automatically |
| Custom agents | `.opencode/agents/*.md` | specialized agents |
| Custom commands | `.opencode/commands/*.md` | reusable slash commands |
| Skills | `.opencode/skills/<name>/SKILL.md` | reusable procedural knowledge |

Use plural folder names. OpenCode also supports some compatibility fallbacks, but new work should target the canonical structure above.

## Decision guide

- Need project-wide behavior? -> `AGENTS.md`
- Need reusable slash prompt behavior? -> `.opencode/commands/*.md`
- Need a specialized persona with permissions? -> `.opencode/agents/*.md`
- Need reusable step-by-step know-how? -> `.opencode/skills/<name>/SKILL.md`
- Need global or external reusable docs? -> `instructions` entries in `opencode.json`

## Authoring rules

1. Prefer official documented file formats over legacy compatibility shims.
2. Prefer `permission` over deprecated `tools` booleans in agents.
3. Keep descriptions short and specific for discovery.
4. Keep `AGENTS.md` concise and link out to detailed docs where appropriate.
5. Do not invent `.prompt.md`, `.instructions.md`, or `.agent.md` file types for OpenCode; map them to commands, rules, and agent markdown files instead.

## Troubleshooting checklist

- Does the file live in the documented location?
- Does the filename match the intended command/agent/skill name?
- Is the frontmatter valid YAML?
- Are only documented frontmatter fields used?
- Is the content short enough and specific enough for discovery?
- If using `instructions` in `opencode.json`, do the referenced files actually exist?

## Reference docs

- [agent-instructions](./references/agent-instructions.md)
- [agents](./references/agents.md)
- [commands / prompts](./references/prompts.md)
- [instructions](./references/instructions.md)
- [skills](./references/skills.md)
- [hooks / replacements](./references/hooks.md)

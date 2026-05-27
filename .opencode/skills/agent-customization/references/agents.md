# OpenCode agents (`.opencode/agents/*.md`)

Custom agents are markdown files stored in `.opencode/agents/` or `~/.config/opencode/agents/`.

## Minimal template

```md
---
description: Reviews code for correctness and risk
mode: subagent
model: azure-foundry/gpt-5.4
temperature: 0.1
permission:
  edit: deny
  bash: deny
---

You are a high-signal code review agent.
Focus on correctness, security, and regressions.
Do not make edits.
```

## Common frontmatter fields

- `description` — what the agent does and when to use it
- `mode` — `primary`, `subagent`, or `all`
- `model` — `provider/model`
- `temperature`
- `top_p`
- `steps`
- `permission`
- `hidden`
- `color`
- `disable`

## Rules

- The filename becomes the agent name.
- Keep descriptions keyword-rich for discovery.
- Use `permission` to restrict edits, bash, web access, skills, and task spawning.
- Use `hidden: true` only for helper subagents that should not appear in normal menus.
- Prefer markdown agent files over legacy `.agent.yaml` or `.agent.md` conventions.

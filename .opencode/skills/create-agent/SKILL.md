---
name: create-agent
description: Create a custom OpenCode agent in .opencode/agents/<name>.md for a specific job.
compatibility: opencode
---
Related skill: `agent-customization`. Read `./references/agents.md` for the official OpenCode agent template and authoring rules.

Guide the user to create or update a markdown agent file in `.opencode/agents/`.

## Extract from Conversation
Review the conversation and generalize any repeated specialized behavior into a focused OpenCode agent. Extract:
- the role or persona
- the task boundary
- the minimum tool/permission set required
- whether the agent should be `primary` or `subagent`

## Clarify if Needed
If the role is not clear, ask:
- What should this agent do?
- When should it be selected instead of the default agent?
- Should it be read-only, editable, or shell-enabled?

## Authoring Rules
- The filename becomes the agent name.
- Use official frontmatter fields such as `description`, `mode`, `model`, `temperature`, `steps`, `permission`, `hidden`, and `color`.
- Prefer `permission` rules over deprecated `tools` booleans.
- Keep `description` keyword-rich so OpenCode can discover the agent correctly.
- Keep the body procedural and explicit about scope, constraints, and output.

## Iterate
1. Draft the agent file.
2. Tighten tool permissions to the minimum needed.
3. Validate that the file sits under `.opencode/agents/` and that the description clearly signals when to use it.
4. Summarize what the agent does and give example prompts.

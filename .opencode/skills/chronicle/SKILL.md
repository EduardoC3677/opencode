---
name: chronicle
description: Summarize recent work, produce standups, give workflow tips, or rebuild recent-work context from repository history, notes, and exported session logs.
compatibility: opencode
---
Use this skill when the user asks for standups, progress recaps, workflow tips, or recent-work reconstruction.

## Supported data sources in OpenCode
Use these in priority order:
1. exported chat/session logs the user provides
2. repository history (`git status`, `git log`, `git diff --stat`)
3. task documents, changelogs, TODO files, notes, standup docs
4. project rules and customizations (`AGENTS.md`, `.opencode/agents/`, `.opencode/commands/`, `.opencode/skills/`)

Do not claim access to undocumented session telemetry or SQL session stores unless the user explicitly provides such data as files.

## Standup workflow
When the user asks for a standup:
- group recent work into themes or branches
- prefer Yesterday / Today / Blockers format
- mention missing context explicitly
- avoid fabricating progress from thin evidence

## Tips workflow
When the user asks for workflow advice:
- inspect how work is organized in the repo and customization files
- infer repeated tasks from command, agent, and skill structure
- provide 3-5 specific recommendations
- prioritize reusable OpenCode improvements such as new commands, agents, rules, or skills

## Reindex / refresh workflow
When the user asks to reindex or refresh session context:
- explain that OpenCode has no documented session-store reindex tool
- scan available local sources instead
- summarize what sources were refreshed and what is still unavailable

## Output quality bar
- keep summaries concise
- distinguish evidence from assumptions
- cite file paths, branches, or documents when possible
- never imply cloud sync or hidden telemetry exists if it was not observed

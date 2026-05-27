# OpenCode skills (`SKILL.md`)

Skills live in `.opencode/skills/<name>/SKILL.md` or `~/.config/opencode/skills/<name>/SKILL.md`.

## Required frontmatter

```yaml
---
name: git-release
description: Create consistent releases and changelogs
license: MIT
compatibility: opencode
metadata:
  audience: maintainers
---
```

## Recognized fields

- `name` (required)
- `description` (required)
- `license` (optional)
- `compatibility` (optional)
- `metadata` (optional string-to-string map)

## Rules

- Skill names must be lowercase alphanumeric with single hyphen separators.
- The `name` field must match the containing directory.
- Keep `description` specific enough for discovery.
- Put large references in `references/`, scripts in `scripts/`, templates/assets in sibling folders.

## Discovery

OpenCode discovers skills from:
- `.opencode/skills/*/SKILL.md`
- `~/.config/opencode/skills/*/SKILL.md`
- compatibility paths such as `.claude/skills/*/SKILL.md` and `.agents/skills/*/SKILL.md`

Skills are loaded through the native `skill` tool, not by inventing custom slash-prompt formats.

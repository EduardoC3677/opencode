# OpenCode project rules (`AGENTS.md`)

OpenCode's primary project-rule file is `AGENTS.md`.

## Locations

| Path | Scope |
|---|---|
| `AGENTS.md` in project root or subdirectories | project / monorepo scope |
| `~/.config/opencode/AGENTS.md` | user-global scope |
| `CLAUDE.md` | compatibility fallback when no `AGENTS.md` is present |

## When to use

Use `AGENTS.md` for durable instructions that should apply to most tasks in the project:
- build / test / lint commands
- architecture constraints
- naming and repository conventions
- operational gotchas
- links to deeper docs

## Recommended structure

```md
# Project Rules

## Build and test
- `npm test`
- `npm run lint`

## Architecture
- API handlers live in `src/api/`
- Shared domain logic lives in `src/core/`

## Conventions
- Prefer existing service abstractions over direct SDK calls
- Update tests with code changes
```

## Good practices

- Keep it concise and high signal.
- Link to detailed docs instead of duplicating them.
- Update it when project conventions change.
- Use `opencode.json` -> `instructions` when you want to include additional markdown files or globs.

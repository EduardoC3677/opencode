---
name: project-setup-info-local
description: OpenCode-native project scaffolding guidance for full project initialization and workspace setup.
compatibility: opencode
---
# How to set up a project

Use this skill when the user wants a complete project skeleton, not a single file.
Only initialize a project in an empty directory or a directory the user explicitly wants to bootstrap.

## General rules

1. Prefer shell scaffolding commands the user can rerun outside the chat.
2. After scaffolding, update `AGENTS.md` and `opencode.json` only if the project needs OpenCode-specific guidance.
3. For MCP servers, configure them in `opencode.json` under the `mcp` key rather than creating legacy editor-specific MCP config files.
4. Document required editor extensions or external tools in `README.md` or `AGENTS.md` instead of assuming a hidden editor-command tool exists.

## vscode-extension

A VS Code / Open VSX compatible editor extension project.

Run:

```bash
npx --package yo --package generator-code -- yo code . --skipOpen
```

Notes:
- Use the generator prompts or flags the user explicitly asks for.
- Treat this as an editor extension project, not an OpenCode runtime plugin.

## next-js

Run:

```bash
npx create-next-app@latest .
```

Useful flags:
- `--ts` / `--js`
- `--tailwind`
- `--eslint`
- `--app`
- `--src-dir`
- `--turbopack`
- `--use-npm|--use-pnpm|--use-yarn|--use-bun`

## vite

Run:

```bash
npx create-vite@latest .
```

Useful flag:
- `-t, --template <name>`

## mcp-server

When creating a Model Context Protocol server:

1. Choose the requested language; default to TypeScript if unspecified.
2. Use the official MCP docs and SDK repo to pick the correct starter.
3. After scaffolding, register the server in `opencode.json`, for example:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "my-server": {
      "type": "local",
      "command": ["node", "dist/index.js"],
      "enabled": true
    }
  }
}
```

4. Add any project-specific usage notes to `AGENTS.md`.

## python-script

Prefer a simple shell-native scaffold such as:

```bash
mkdir -p . && python3 -m venv .venv && printf 'print("hello")
' > main.py
```

Then add the dependency/install/run instructions the user needs.

## python-package

Prefer a standard packaging scaffold such as:

```bash
python3 -m venv .venv
. .venv/bin/activate
python -m pip install --upgrade pip build
mkdir -p src/<package_name>
printf '__all__ = []
' > src/<package_name>/__init__.py
```

Then create `pyproject.toml`, tests, and package metadata as appropriate for the requested toolchain.

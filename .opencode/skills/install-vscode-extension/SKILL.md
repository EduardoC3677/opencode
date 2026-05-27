---
name: install-vscode-extension
description: Install or document a required editor extension for an OpenCode workflow.
compatibility: opencode
---
# Installing editor extensions for an OpenCode workflow

Use this skill when the user needs a VS Code / VSCodium / Open VSX extension to support a language, debugger, or editor-side capability while working with OpenCode.

## Procedure

1. Identify the required extension id in `publisher.extension-name` form.
2. Prefer a native editor install path:
   - VS Code / VSCodium CLI: `code --install-extension <id>`
   - Open VSX compatible environments: install the same id from the marketplace UI if the CLI is unavailable.
3. If the user explicitly asks for pre-release, install the pre-release build; otherwise prefer stable.
4. If the current environment does not expose an editor command runner, tell the user the exact extension id and the one-line install command instead of pretending OpenCode has a hidden extension-install tool.
5. For repeatable project setup, record required extensions in `AGENTS.md`, `README.md`, or project setup docs.

## Output

Return:
- the exact extension id
- whether stable or pre-release is appropriate
- the command the user can run, if CLI installation is available

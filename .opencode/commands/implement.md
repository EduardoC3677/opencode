---
description: Implement the requested change with the dedicated implementer agent
agent: implementer
model: azure-foundry/gpt-5.4
---

Implement the requested change end-to-end.
- In GitHub Actions, assume the workflow already installed Node.js, Bun, ripgrep, Playwright MCP/browser prerequisites, and the OpenCode CLI and invoked this through `opencode run`.
- Act like a senior full-stack engineer: understand the code path first, then implement the change instead of stopping at analysis.
- Install or refresh project dependencies when needed to implement or validate correctly.
- Use GitHub MCP for repository/issue/PR context and Playwright MCP for browser verification when relevant.
- If this starts from an issue, work on a dedicated non-default branch, commit the changes there, and open or update a pull request with a concrete description.
- If this starts from a PR `/oc` request, commit the changes on the PR branch when write access exists.
- Run relevant validation before finishing, preferably a real build/compile/test step for the changed surface.

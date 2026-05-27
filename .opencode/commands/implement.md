---
description: Implement the requested change with the dedicated implementer agent
agent: implementer
model: azure-foundry/gpt-5.4
---

Implement the requested change end-to-end.
- In GitHub Actions, assume the workflow already installed Node.js, Bun, ripgrep, and the OpenCode CLI and invoked this through `opencode run`.
- If this starts from an issue, work on a dedicated non-default branch, commit the changes there, and open or update a pull request with a concrete description.
- If this starts from a PR `/oc` request, commit the changes on the PR branch when write access exists.
- Run relevant validation before finishing.

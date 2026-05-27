---
description: Review the newest PR commits first with the dedicated reviewer agent
agent: reviewer
model: azure-foundry/gpt-5.4
---

Review the current pull request with high-signal feedback only.
Assume GitHub Actions prepared Node.js, Bun, ripgrep, and the OpenCode CLI and launched this review via `opencode run`.
If the prompt includes a previous PR head SHA, inspect the newly pushed commits and incremental diff first, then expand to the full PR context only when needed.

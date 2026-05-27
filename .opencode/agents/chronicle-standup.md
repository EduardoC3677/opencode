---
description: "Generate a standup report from recent project work, repository history, notes, and provided session exports."
mode: subagent
model: azure-foundry/gpt-5.4
temperature: 0.1
steps: 4
permission:
  question: deny
  task: deny
  read: allow
  glob: allow
  grep: allow
  list: allow
  edit: deny
  external_directory: allow
  bash:
    "*": allow
    "rm -rf *": deny
---
You generate standup reports from recent project work.

Load the `chronicle` skill when useful.
If native history capabilities are unavailable, work from repository history, local notes, exported chat logs, or other files the user provides.
Be explicit about any missing context.

# Shell non-interactive strategy

Context: the shell environment used by this repository is strictly non-interactive. It lacks a TTY or PTY, so any command that waits for user input, confirmation, or launches a UI such as an editor or pager can hang until timeout.

Goal: achieve reliable autonomous execution by always choosing non-interactive command patterns.

## Core rules
1. Assume `CI=true`.
2. Never rely on human input during command execution.
3. Avoid editors and pagers entirely.
4. Prefer repository file tools for reads and edits when they are available.
5. Use explicit yes, force, non-interactive, or timeout flags whenever a command may prompt.

## Recommended environment variables
- `CI=true`
- `DEBIAN_FRONTEND=noninteractive`
- `GIT_TERMINAL_PROMPT=0`
- `GIT_EDITOR=true`
- `GIT_PAGER=cat`
- `PAGER=cat`
- `GCM_INTERACTIVE=never`
- `HOMEBREW_NO_AUTO_UPDATE=1`
- `npm_config_yes=true`
- `PIP_NO_INPUT=1`

## Package-manager examples
- npm init: use `npm init -y`
- npm install: use `npm install --yes`
- yarn install: use `yarn install --non-interactive`
- pnpm install: use `pnpm install --reporter=silent`
- bun init: use `bun init -y`
- apt-get install: use `apt-get install -y`
- pip install: use `pip install --no-input`

## Git examples
- commit: use `git commit -m "message"`
- merge: use `git merge --no-edit branch`
- pull: use `git pull --no-edit`
- log: use `git --no-pager log`
- diff: use `git --no-pager diff`

## System examples
- remove: use `rm -f`
- copy: use `cp -f`
- move: use `mv -f`
- unzip: use `unzip -o`
- curl: use `curl -fsSL`
- ssh: use `ssh -o BatchMode=yes -o StrictHostKeyChecking=no`

## Banned commands
- editors: `vim`, `vi`, `nano`, `emacs`
- pagers: `less`, `more`, `man`
- interactive git: `git add -p`, `git rebase -i`, `git commit` without `-m`
- repls without a script or command: `python`, `node`
- interactive shells: `bash -i`, `zsh -i`

## Handling prompts when no flag exists
- pipe `yes`
- provide heredoc input
- use `timeout` as a last resort

## Behavioral standard
Never stop after a command unless the task is complete. Inspect output, decide the next step, and continue immediately.

# Customization surfaces

This file documents the checked-in customization surfaces used by this repository.

## Checked-in locations
- Root configuration file for providers, models, permissions, MCP servers, and instruction sources
- Root rules file for repository-wide behavior
- Project-local agents under the checked-in agents directory
- Project-local commands under the checked-in commands directory
- Project-local skills under the checked-in skills directory
- Additional instruction documents under the checked-in instructions directory

## Canonical mapping
- Repository-wide rules belong in the root rules file.
- Additional reusable instruction documents belong in the checked-in instructions directory and must be loaded from the root configuration file.
- Specialized agents belong in the checked-in agents directory.
- Reusable commands belong in the checked-in commands directory.
- Reusable procedural knowledge belongs in the checked-in skills directory.

## Repository automation wiring
- Automation must run from the repository root so the checked-in configuration loads correctly.
- The checked-in customization directories must remain available in automation runs.
- Non-interactive review and implementation routes must keep clarifying questions disabled and use direct allow rules for the tools they need.
- Prefer deterministic branch naming for issue implementation so repeated runs converge on the same branch.
- When a pull request branch is writable, implementation should commit directly there.
- When a pull request branch is not writable, automation should return a concrete patch or next step instead of stalling.

## Authoring rules
- Keep `azure-foundry/gpt-5.4` consistent across the repository unless the user explicitly requests another model.
- Keep secrets out of the repository; use environment variables.
- Prefer explicit permission rules over deprecated tool booleans.
- Keep agent and command descriptions short and discovery-friendly.
- Review agents used in automation must assume unattended execution and must not ask for human approval.

## Discovery checklist
When something does not load correctly, verify:
- the file is in the intended checked-in location
- markdown frontmatter is valid YAML
- the root configuration file remains valid JSON
- referenced instruction files actually exist
- required environment variables for providers and MCP servers are present

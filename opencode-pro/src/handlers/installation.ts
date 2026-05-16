import type { Probot } from "probot";

/**
 * Handle GitHub App installation events.
 * Logs installations and sets up initial configuration.
 */

export function setupInstallationHandler(app: Probot): void {
  // When the app is installed on an account
  app.on("installation.created", async (context) => {
    const { installation, repositories } = context.payload;

    context.log.info(
      `🎉 opencode-pro installed on ${installation.account?.login}` +
        (repositories
          ? ` with ${repositories.length} repositories`
          : " (all repositories)"),
    );

    // Post a welcome message if there's a specific repo
    if (repositories && repositories.length > 0) {
      const repo = repositories[0];
      try {
        await context.octokit.issues.create({
          owner: installation.account?.login ?? repo.full_name.split("/")[0],
          repo: repo.name,
          title: "🤖 opencode-pro is now active!",
          body: [
            "## 🎉 opencode-pro has been installed!",
            "",
            "I'm your AI-powered code assistant. Here's how to use me:",
            "",
            "### 📝 Issues & Pull Requests",
            "- **@mention me** — Just write `@opencode-pro` in any comment and I'll respond",
            "- **Assign me** — Assign me to an issue or PR and I'll work on it automatically",
            "",
            "### 🔧 Slash Commands (in PR comments)",
            "- `/review` — I'll review the code and provide feedback",
            "- `/fix` — I'll analyze and fix issues in the code",
            "- `/explain` — I'll explain what the PR does",
            "- `/test` — I'll write tests for the changes",
            "",
            "### ⚡ Auto-Review",
            "- I automatically review new PRs and updates",
            "",
            "---",
            "Powered by [OpenCode](https://github.com/anomalyco/opencode) 🚀",
          ].join("\n"),
        });
      } catch (err) {
        context.log.error(`Failed to post welcome message: ${err}`);
      }
    }
  });

  // When the app is uninstalled
  app.on("installation.deleted", async (context) => {
    context.log.info(
      `👋 opencode-pro uninstalled from ${context.payload.installation.account?.login}`,
    );
  });

  // When repositories are added to an installation
  app.on("installation_repositories.added", async (context) => {
    const { repositories_added } = context.payload;
    context.log.info(
      `➕ Repositories added: ${repositories_added.map((r) => r.full_name).join(", ")}`,
    );
  });

  // When repositories are removed from an installation
  app.on("installation_repositories.removed", async (context) => {
    const { repositories_removed } = context.payload;
    context.log.info(
      `➖ Repositories removed: ${repositories_removed.map((r) => r.full_name).join(", ")}`,
    );
  });
}
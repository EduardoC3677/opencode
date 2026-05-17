import type { Probot } from "probot";
import { setupInstallationHandler } from "./handlers/installation.js";
import { setupIssueCommentHandler } from "./handlers/issue-comment.js";
import { setupIssuesHandler } from "./handlers/issues.js";
import { setupPullRequestHandler } from "./handlers/pull-request.js";
import { setupReactionHandler } from "./handlers/reactions.js";

/**
 * Main Probot application.
 * Registers all webhook handlers.
 */
export function opencodeProApp(app: Probot): void {
  app.log.info("🤖 opencode-pro GitHub App starting...");

  try {
    setupInstallationHandler(app);
    setupIssueCommentHandler(app);
    setupIssuesHandler(app);
    setupPullRequestHandler(app);
    setupReactionHandler(app);
    app.log.info("✅ All handlers registered");
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    app.log.error(`Handler registration failed: ${message}`);
    throw error;
  }
}
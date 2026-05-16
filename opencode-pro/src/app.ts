import type { Probot } from "probot";
import { setupInstallationHandler } from "./handlers/installation.js";
import { setupIssueCommentHandler } from "./handlers/issue-comment.js";
import { setupIssuesHandler } from "./handlers/issues.js";
import { setupPullRequestHandler } from "./handlers/pull-request.js";

/**
 * Main Probot application.
 * Registers all webhook handlers.
 */
export function opencodeProApp(app: Probot): void {
  app.log.info("🤖 opencode-pro GitHub App starting...");

  // Register all event handlers
  setupInstallationHandler(app);
  setupIssueCommentHandler(app);
  setupIssuesHandler(app);
  setupPullRequestHandler(app);

  app.log.info("✅ All handlers registered");
}
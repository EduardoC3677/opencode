import type { Probot } from "probot";
import { isBotAssigned, isPullRequest } from "../github/client.js";
import { formatTaskResponse } from "../github/comments.js";
import { runOpenCodeTask } from "../opencode/runner.js";
import { loadConfig } from "../config.js";

const config = loadConfig();
const BOT_LOGIN = "opencode-pro[bot]";

/**
 * Handle issues events.
 * Detects when the bot is assigned to an issue and auto-executes.
 */
export function setupIssuesHandler(app: Probot): void {
  // When an issue is opened
  app.on("issues.opened", async (context) => {
    const { issue, repository } = context.payload;
    const owner = repository.owner.login;
    const repo = repository.name;

    // Skip PRs (handled by pull_request handler)
    if (isPullRequest(issue)) return;

    // Skip if repo is blocked
    if (config.blockedRepos.includes(`${owner}/${repo}`)) return;

    // Skip if allowed repos is set and this repo is not in it
    if (
      config.allowedRepos.length > 0 &&
      !config.allowedRepos.includes(`${owner}/${repo}`)
    ) {
      return;
    }

    // Check if bot is assigned
    if (!isBotAssigned(issue, BOT_LOGIN)) return;

    context.log.info(
      `Bot assigned to new issue ${owner}/${repo}#${issue.number}`,
    );

    const result = await runOpenCodeTask({
      githubContext: context as any,
      trigger: "assigned",
      issueNumber: issue.number,
      isPullRequest: false,
      owner,
      repo,
      model: config.model,
    });

    const responseBody = formatTaskResponse(result, "completó la tarea asignada.");

    try {
      await context.octokit.issues.createComment({
        owner,
        repo,
        issue_number: issue.number,
        body: responseBody,
      });
    } catch (err) {
      context.log.error(`Failed to post response: ${err}`);
    }
  });

  // When an issue is assigned (someone assigns the bot)
  app.on("issues.assigned", async (context) => {
    const { issue, assignee, repository } = context.payload;
    const owner = repository.owner.login;
    const repo = repository.name;

    // Skip PRs
    if (isPullRequest(issue)) return;

    // Skip if repo is blocked
    if (config.blockedRepos.includes(`${owner}/${repo}`)) return;

    // Skip if allowed repos is set and this repo is not in it
    if (
      config.allowedRepos.length > 0 &&
      !config.allowedRepos.includes(`${owner}/${repo}`)
    ) {
      return;
    }

    // Guard: assignee must exist and be our bot
    if (!assignee || assignee.login !== BOT_LOGIN) return;

    context.log.info(
      `Bot assigned to ${owner}/${repo}#${issue.number}`,
    );

    const result = await runOpenCodeTask({
      githubContext: context as any,
      trigger: "assigned",
      issueNumber: issue.number,
      isPullRequest: false,
      owner,
      repo,
      model: config.model,
    });

    const responseBody = formatTaskResponse(result, "ha sido asignado y completó la tarea.");

    try {
      await context.octokit.issues.createComment({
        owner,
        repo,
        issue_number: issue.number,
        body: responseBody,
      });
    } catch (err) {
      context.log.error(`Failed to post response: ${err}`);
    }
  });
}
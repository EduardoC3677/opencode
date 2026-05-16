import type { Probot } from "probot";
import { isBotAssigned } from "../github/client.js";
import { formatTaskResponse } from "../github/comments.js";
import { runOpenCodeTask } from "../opencode/runner.js";
import { loadConfig } from "../config.js";

const config = loadConfig();
const BOT_LOGIN = "opencode-pro[bot]";

/**
 * Handle pull_request events.
 * Auto-reviews PRs and handles assignment.
 */
export function setupPullRequestHandler(app: Probot): void {
  // Auto-review when PR is opened
  app.on("pull_request.opened", async (context) => {
    const { pull_request, repository } = context.payload;
    const owner = repository.owner.login;
    const repo = repository.name;

    // Skip if repo is blocked
    if (config.blockedRepos.includes(`${owner}/${repo}`)) return;

    // Skip if allowed repos is set and this repo is not in it
    if (
      config.allowedRepos.length > 0 &&
      !config.allowedRepos.includes(`${owner}/${repo}`)
    ) {
      return;
    }

    context.log.info(
      `Auto-reviewing PR ${owner}/${repo}#${pull_request.number}`,
    );

    // Add eyes reaction to show we're reviewing
    try {
      await context.octokit.reactions.createForIssue({
        owner,
        repo,
        issue_number: pull_request.number,
        content: "eyes",
      });
    } catch {
      // Ignore
    }

    const result = await runOpenCodeTask({
      githubContext: context as any,
      trigger: "pr_opened",
      issueNumber: pull_request.number,
      isPullRequest: true,
      owner,
      repo,
      model: config.model,
    });

    const responseBody = formatTaskResponse(result, "completó la revisión automática.");

    try {
      await context.octokit.issues.createComment({
        owner,
        repo,
        issue_number: pull_request.number,
        body: responseBody,
      });
    } catch (err) {
      context.log.error(`Failed to post review: ${err}`);
    }
  });

  // When PR is synchronized (new commits pushed)
  app.on("pull_request.synchronize", async (context) => {
    const { pull_request, repository } = context.payload;
    const owner = repository.owner.login;
    const repo = repository.name;

    if (config.blockedRepos.includes(`${owner}/${repo}`)) return;
    if (
      config.allowedRepos.length > 0 &&
      !config.allowedRepos.includes(`${owner}/${repo}`)
    ) {
      return;
    }

    context.log.info(
      `Re-reviewing updated PR ${owner}/${repo}#${pull_request.number}`,
    );

    const result = await runOpenCodeTask({
      githubContext: context as any,
      trigger: "pr_synchronize",
      issueNumber: pull_request.number,
      isPullRequest: true,
      owner,
      repo,
      model: config.model,
    });

    const responseBody = formatTaskResponse(result, "completó la re-revisión.");

    try {
      await context.octokit.issues.createComment({
        owner,
        repo,
        issue_number: pull_request.number,
        body: responseBody,
      });
    } catch (err) {
      context.log.error(`Failed to post re-review: ${err}`);
    }
  });

  // When bot is assigned to a PR
  app.on("pull_request.assigned", async (context) => {
    const { pull_request, assignee, repository } = context.payload;
    const owner = repository.owner.login;
    const repo = repository.name;

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
      `Bot assigned to PR ${owner}/${repo}#${pull_request.number}`,
    );

    const result = await runOpenCodeTask({
      githubContext: context as any,
      trigger: "assigned",
      issueNumber: pull_request.number,
      isPullRequest: true,
      owner,
      repo,
      model: config.model,
    });

    const responseBody = formatTaskResponse(result, "ha sido asignado y completó la revisión.");

    try {
      await context.octokit.issues.createComment({
        owner,
        repo,
        issue_number: pull_request.number,
        body: responseBody,
      });
    } catch (err) {
      context.log.error(`Failed to post response: ${err}`);
    }
  });
}
import type { Probot } from "probot";
import { isPullRequest } from "../github/client.js";
import { formatTaskResponse } from "../github/comments.js";
import { runOpenCodeTask } from "../opencode/runner.js";
import { loadConfig } from "../config.js";
import type { SlashCommand, TriggerKind } from "../types.js";
import { COMMAND_AGENT_MAP } from "../types.js";

const config = loadConfig();
const BOT_NAME = "opencode-pro";

const SLASH_COMMANDS: SlashCommand[] = [
  "/review", "/fix", "/explain", "/test",
  "/plan", "/build", "/refactor", "/docs",
  "/optimize", "/security", "/summarize", "/suggest",
];

const OC_PREFIX_PATTERN = /^\/(oc|opencode)\b/i;

function extractSlashCommandToken(body: string): SlashCommand | undefined {
  const firstContentLine = body
    .split("\n")
    .map((line) => line.trim())
    .find((line) => line.length > 0);

  if (!firstContentLine) {
    return undefined;
  }

  const firstToken = firstContentLine.split(/\s+/)[0];
  if (!firstToken) {
    return undefined;
  }

  return (SLASH_COMMANDS as string[]).includes(firstToken)
    ? (firstToken as SlashCommand)
    : undefined;
}

function extractPrefixedCommand(
  body: string,
): { command?: SlashCommand; taskText: string } | null {
  const lines = body.split("\n");
  const firstContentLineIndex = lines.findIndex((line) => line.trim().length > 0);
  if (firstContentLineIndex < 0) {
    return null;
  }

  const firstContentLine = lines[firstContentLineIndex]?.trim() ?? "";

  if (!firstContentLine || !OC_PREFIX_PATTERN.test(firstContentLine)) {
    return null;
  }

  const remainder = firstContentLine.replace(OC_PREFIX_PATTERN, "").trim();
  if (!remainder) {
    const trailingText = lines.slice(firstContentLineIndex + 1).join("\n").trim();
    return { taskText: trailingText };
  }

  const [firstToken, ...restTokens] = remainder.split(/\s+/);
  const normalizedToken = firstToken.startsWith("/") ? firstToken.toLowerCase() : `/${firstToken.toLowerCase()}`;
  const trailingText = lines.slice(firstContentLineIndex + 1).join("\n").trim();

  if ((SLASH_COMMANDS as string[]).includes(normalizedToken)) {
    const commandTask = [restTokens.join(" ").trim(), trailingText].filter(Boolean).join("\n\n");
    return {
      command: normalizedToken as SlashCommand,
      taskText: commandTask,
    };
  }

  const customTask = [remainder, trailingText].filter(Boolean).join("\n\n");
  return { taskText: customTask };
}

/**
 * Handle issue_comment events.
 * Detects @opencode-pro mentions and slash commands.
 */
export function setupIssueCommentHandler(app: Probot): void {
  app.on("issue_comment.created", async (context) => {
    const { comment, issue, repository } = context.payload;
    const body = comment.body;
    const owner = repository.owner.login;
    const repo = repository.name;
    const issueNumber = issue.number;
    const isPR = isPullRequest(issue);

    // Skip bot's own comments
    if (comment.user?.type === "Bot") return;

    // Skip if repo is blocked
    if (config.blockedRepos.includes(`${owner}/${repo}`)) return;

    // Skip if allowed repos is set and this repo is not in it
    if (
      config.allowedRepos.length > 0 &&
      !config.allowedRepos.includes(`${owner}/${repo}`)
    ) {
      return;
    }

    let trigger: TriggerKind | null = null;
    let command: SlashCommand | undefined;
    let commentBodyForPrompt = body;

    // Check for @opencode-pro mention
    const mentionPattern = new RegExp(`@${BOT_NAME}\\b`, "i");
    if (mentionPattern.test(body)) {
      trigger = "mention";
    }

    // Check for slash commands (only in PRs)
    if (isPR) {
      const slashCommand = extractSlashCommandToken(body);
      if (slashCommand) {
        trigger = "slash_command";
        command = slashCommand;
      }
    }

    // Check for /oc and /opencode prefixed commands/tasks
    const prefixedCommand = extractPrefixedCommand(body);
    if (prefixedCommand) {
      trigger = "slash_command";
      command = prefixedCommand.command;
      commentBodyForPrompt = prefixedCommand.taskText;

      if (!prefixedCommand.command && !prefixedCommand.taskText) {
        context.log.info(
          `Ignoring empty /oc command on ${owner}/${repo}#${issueNumber}`,
        );
        return;
      }
    }

    if (!trigger) return;

    // Look up the best agent for slash commands
    const agent = command ? COMMAND_AGENT_MAP[command] : undefined;

    // Add a reaction to show we're working
    try {
      await context.octokit.reactions.createForIssueComment({
        owner,
        repo,
        comment_id: comment.id,
        content: "eyes",
      });
    } catch (reactionErr) {
      context.log.warn(
        `Failed to add eyes reaction on ${owner}/${repo}#${issueNumber}: ${String(reactionErr)}`,
      );
    }

    context.log.info(
      `Processing ${trigger}${command ? ` ${command}` : ""} on ${owner}/${repo}#${issueNumber}`,
    );

    // Run the OpenCode task
    let result;
    try {
      result = await runOpenCodeTask({
        githubContext: context as any,
        trigger,
        command,
        commentBody: commentBodyForPrompt,
        issueNumber,
        isPullRequest: isPR,
        owner,
        repo,
        model: config.model,
        agent,
      });
    } catch (err) {
      context.log.error(`OpenCode task threw synchronously: ${err}`);
      try {
        await context.octokit.issues.createComment({
          owner,
          repo,
          issue_number: issueNumber,
          body: `🤖 **opencode-pro** encontró un error inesperado.\n\n\`\`\`\n${String(err).slice(0, 1000)}\n\`\`\``,
        });
      } catch (notifyErr) {
        context.log.error(`Failed to post unexpected-error comment: ${String(notifyErr)}`);
      }
      return;
    }

    if (!result.success) {
      // Report the failure
      const responseBody = formatTaskResponse(result, "encontró un error.");
      try {
        await context.octokit.issues.createComment({
          owner,
          repo,
          issue_number: issueNumber,
          body: responseBody,
        });
      } catch (postErr) {
        context.log.error(`Failed to post error comment: ${postErr}`);
      }
      return;
    }

    // Success path
    const responseBody = formatTaskResponse(result, "completó la tarea.");

    try {
      await context.octokit.issues.createComment({
        owner,
        repo,
        issue_number: issueNumber,
        body: responseBody,
      });
    } catch (postErr) {
      context.log.error(`Failed to post success comment: ${postErr}`);
    }
  });
}

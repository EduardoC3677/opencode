import type { Probot } from "probot";

/**
 * Handle reaction_added events to enable interaction shortcuts.
 * Users can react to bot comments to approve, dismiss, or retry.
 */
export function setupReactionHandler(app: Probot): void {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  app.on("reaction.created" as any, async (context: any) => {
    const { reaction, comment, repository } = context.payload;
    const owner = repository.owner.login;
    const repo = repository.name;

    // Only react to reactions on the bot's own comments
    const commentUser = comment?.user;
    if (!commentUser || commentUser.type !== "Bot") return;
    // Check if the comment is from our bot
    if (!commentUser.login?.includes("opencode-pro")) return;

    const reactionUser = reaction.user?.login;
    if (!reactionUser) return;

    // Skip if the reactor is the bot itself
    if (reactionUser.includes("opencode-pro")) return;

    const reactionName = reaction.content;

    context.log.info(
      `Reaction ${reactionName} from ${reactionUser} on ${owner}/${repo}#${comment?.issue_url}`,
    );

    switch (reactionName) {
      case "rocket":
        // User approves the suggestion
        try {
          await context.octokit.reactions.createForIssueComment({
            owner,
            repo,
            comment_id: comment.id,
            content: "heart",
          });
        } catch {
          // Best effort
        }
        break;

      case "-1":
        // User dismisses the suggestion
        try {
          await context.octokit.reactions.createForIssueComment({
            owner,
            repo,
            comment_id: comment.id,
            content: "confused",
          });
        } catch {
          // Best effort
        }
        break;

      default:
        break;
    }
  });
}
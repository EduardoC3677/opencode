import type { Context, ProbotOctokit } from "probot";

/**
 * GitHub API utilities for the bot
 */

/** Post a comment on an issue or PR */
export async function postComment(
  octokit: InstanceType<typeof ProbotOctokit>,
  owner: string,
  repo: string,
  issueNumber: number,
  body: string,
): Promise<void> {
  await octokit.issues.createComment({
    owner,
    repo,
    issue_number: issueNumber,
    body,
  });
}

/** Add a reaction to a comment */
export async function addReaction(
  octokit: InstanceType<typeof ProbotOctokit>,
  owner: string,
  repo: string,
  commentId: number,
  reaction: "+1" | "-1" | "rocket" | "eyes" | "heart",
): Promise<void> {
  await octokit.reactions.createForIssueComment({
    owner,
    repo,
    comment_id: commentId,
    content: reaction,
  });
}

/** Get issue or PR details */
export async function getIssue(
  octokit: InstanceType<typeof ProbotOctokit>,
  owner: string,
  repo: string,
  issueNumber: number,
) {
  const { data } = await octokit.issues.get({
    owner,
    repo,
    issue_number: issueNumber,
  });
  return data;
}

/** Check if an issue is a pull request */
export function isPullRequest(issue: { pull_request?: unknown }): boolean {
  return issue.pull_request !== undefined && issue.pull_request !== null;
}

/** Get the PR diff via raw request with diff accept header */
export async function getPullRequestDiff(
  octokit: InstanceType<typeof ProbotOctokit>,
  owner: string,
  repo: string,
  pullNumber: number,
): Promise<string> {
  const response = await octokit.request(
    "GET /repos/{owner}/{repo}/pulls/{pull_number}",
    {
      owner,
      repo,
      pull_number: pullNumber,
      headers: { accept: "application/vnd.github.v3.diff" },
    },
  );
  return typeof response.data === "string" ? response.data : "";
}

/** Get PR files changed */
export async function getPullRequestFiles(
  octokit: InstanceType<typeof ProbotOctokit>,
  owner: string,
  repo: string,
  pullNumber: number,
) {
  const { data } = await octokit.pulls.listFiles({
    owner,
    repo,
    pull_number: pullNumber,
  });
  return data;
}

/** Get issue/PR comments */
export async function getComments(
  octokit: InstanceType<typeof ProbotOctokit>,
  owner: string,
  repo: string,
  issueNumber: number,
) {
  const { data } = await octokit.issues.listComments({
    owner,
    repo,
    issue_number: issueNumber,
  });
  return data;
}

/** Check if the bot is assigned to an issue */
export function isBotAssigned(
  issue: { assignees?: Array<{ login: string }> | null },
  botLogin: string,
): boolean {
  return issue.assignees?.some((a) => a.login === botLogin) ?? false;
}
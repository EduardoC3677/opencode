import type { ProbotOctokit } from "probot";

interface GitHubError extends Error {
  status?: number;
}

/**
 * Retry helper for transient errors (429 rate limit, 5xx server errors).
 */
async function withRetry<T>(fn: () => Promise<T>, retries = 2): Promise<T> {
  for (let attempt = 0; attempt <= retries; attempt++) {
    try {
      return await fn();
    } catch (err) {
      const githubErr = err as GitHubError;
      // Retry on rate limits (429), server errors (5xx), or network errors (no status)
      const isRetryable =
        githubErr.status === 429 ||
        (githubErr.status !== undefined && githubErr.status >= 500) ||
        githubErr.status === undefined;
      
      if (attempt === retries || !isRetryable) throw err;
      await new Promise((r) => setTimeout(r, 1000 * (attempt + 1)));
    }
  }
  // Unreachable — satisfies TypeScript
  throw new Error("withRetry: unexpected fallthrough");
}

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
  try {
    await withRetry(() =>
      octokit.issues.createComment({
        owner,
        repo,
        issue_number: issueNumber,
        body,
      }),
    );
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    throw new Error(`Failed to post comment on ${owner}/${repo}#${issueNumber}: ${message}`);
  }
}

/** Add a reaction to a comment */
export async function addReaction(
  octokit: InstanceType<typeof ProbotOctokit>,
  owner: string,
  repo: string,
  commentId: number,
  reaction: "+1" | "-1" | "rocket" | "eyes" | "heart",
): Promise<void> {
  try {
    await octokit.reactions.createForIssueComment({
      owner,
      repo,
      comment_id: commentId,
      content: reaction,
    });
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    throw new Error(`Failed to add reaction to comment ${commentId}: ${message}`);
  }
}

/** Get issue or PR details */
export async function getIssue(
  octokit: InstanceType<typeof ProbotOctokit>,
  owner: string,
  repo: string,
  issueNumber: number,
) {
  try {
    const { data } = await withRetry(() =>
      octokit.issues.get({
        owner,
        repo,
        issue_number: issueNumber,
      }),
    );
    return data;
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    throw new Error(`Failed to get issue ${owner}/${repo}#${issueNumber}: ${message}`);
  }
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
  try {
    const response = await withRetry(() =>
      octokit.request("GET /repos/{owner}/{repo}/pulls/{pull_number}", {
        owner,
        repo,
        pull_number: pullNumber,
        headers: { accept: "application/vnd.github.v3.diff" },
      }),
    );
    if (typeof response.data !== "string") {
      throw new Error(`Unexpected response type for PR diff on ${owner}/${repo}#${pullNumber}`);
    }
    return response.data;
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    throw new Error(`Failed to get PR diff for ${owner}/${repo}#${pullNumber}: ${message}`);
  }
}

/** Get PR files changed */
export async function getPullRequestFiles(
  octokit: InstanceType<typeof ProbotOctokit>,
  owner: string,
  repo: string,
  pullNumber: number,
) {
  try {
    const { data } = await withRetry(() =>
      octokit.pulls.listFiles({
        owner,
        repo,
        pull_number: pullNumber,
      }),
    );
    return data;
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    throw new Error(`Failed to list PR files for ${owner}/${repo}#${pullNumber}: ${message}`);
  }
}

/** Get issue/PR comments */
export async function getComments(
  octokit: InstanceType<typeof ProbotOctokit>,
  owner: string,
  repo: string,
  issueNumber: number,
) {
  try {
    const { data } = await withRetry(() =>
      octokit.issues.listComments({
        owner,
        repo,
        issue_number: issueNumber,
      }),
    );
    return data;
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    throw new Error(`Failed to list comments on ${owner}/${repo}#${issueNumber}: ${message}`);
  }
}

/** Check if the bot is assigned to an issue */
export function isBotAssigned(
  issue: { assignees?: Array<{ login: string }> | null },
  botLogin: string,
): boolean {
  return issue.assignees?.some((a) => a.login === botLogin) ?? false;
}
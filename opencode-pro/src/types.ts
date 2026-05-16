import type { Context } from "probot";

/** Supported trigger types for the bot */
export type TriggerKind =
  | "mention"           // @opencode-pro mentioned in a comment
  | "assigned"          // Bot was assigned to an issue or PR
  | "slash_command"     // Slash command like /review, /fix, etc.
  | "pr_opened"         // PR was opened (auto-review)
  | "pr_synchronize";   // PR was updated with new commits

/** Supported slash commands */
export type SlashCommand = "/review" | "/fix" | "/explain" | "/test";

/** Context passed to the OpenCode runner */
export interface TaskContext {
  /** The Probot context for GitHub API access */
  githubContext: Context;
  /** What triggered this task */
  trigger: TriggerKind;
  /** The slash command if trigger is slash_command */
  command?: SlashCommand;
  /** The comment body that triggered this (if any) */
  commentBody?: string;
  /** The issue or PR number */
  issueNumber: number;
  /** Whether this is a PR (true) or issue (false) */
  isPullRequest: boolean;
  /** Repository owner */
  owner: string;
  /** Repository name */
  repo: string;
  /** The model to use for this task */
  model: string;
  /** PR diff content (for code review) */
  prDiff?: string;
  /** PR changed files summary */
  prFiles?: string;
  /** Issue/PR title */
  title?: string;
  /** Issue/PR body */
  body?: string;
}

/** Result from running an OpenCode task */
export interface TaskResult {
  success: boolean;
  output?: string;
  error?: string;
  durationMs: number;
}

/** Configuration loaded from environment */
export interface BotConfig {
  appId: number;
  privateKey: string;
  webhookSecret: string;
  clientId: string;
  clientSecret: string;
  opencodeConfigDir: string;
  azureOpenaiApiKey: string;
  model: string;
  port: number;
  logLevel: string;
  taskTimeout: number;
  allowedRepos: string[];
  blockedRepos: string[];
}
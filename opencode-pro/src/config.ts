import { config } from "dotenv";
import type { BotConfig } from "./types.js";

config();

function parseReposList(value: string | undefined): string[] {
  if (!value) return [];
  return value
    .split(",")
    .map((r) => r.trim())
    .filter(Boolean);
}

export function loadConfig(): BotConfig {
  const privateKey = process.env.PRIVATE_KEY?.replace(/\\n/g, "\n") ?? "";

  if (!process.env.APP_ID) {
    throw new Error("Missing required env var: APP_ID");
  }
  if (!privateKey) {
    throw new Error("Missing required env var: PRIVATE_KEY");
  }
  if (!process.env.WEBHOOK_SECRET) {
    throw new Error("Missing required env var: WEBHOOK_SECRET");
  }

  const appId = Number(process.env.APP_ID);
  if (Number.isNaN(appId)) {
    throw new Error("APP_ID must be a valid number");
  }

  const port = process.env.PORT !== undefined ? Number(process.env.PORT) : 3000;
  if (Number.isNaN(port)) {
    throw new Error("PORT must be a valid number");
  }

  const taskTimeout = process.env.TASK_TIMEOUT !== undefined ? Number(process.env.TASK_TIMEOUT) : 900;
  if (Number.isNaN(taskTimeout)) {
    throw new Error("TASK_TIMEOUT must be a valid number");
  }

  return {
    appId,
    privateKey,
    webhookSecret: process.env.WEBHOOK_SECRET,
    clientId: process.env.CLIENT_ID ?? "",
    clientSecret: process.env.CLIENT_SECRET ?? "",
    opencodeConfigDir: process.env.OPENCODE_CONFIG_DIR ?? ".opencode",
    azureOpenaiApiKey: process.env.AZURE_OPENAI_API_KEY ?? "",
    model: process.env.OPENCODE_MODEL ?? "deepseek/deepseek-v4-pro",
    port,
    logLevel: process.env.LOG_LEVEL ?? "info",
    taskTimeout,
    allowedRepos: parseReposList(process.env.ALLOWED_REPOS),
    blockedRepos: parseReposList(process.env.BLOCKED_REPOS),
  };
}
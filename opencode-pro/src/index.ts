import { run } from "probot";
import { opencodeProApp } from "./app.js";
import { loadConfig } from "./config.js";

const config = loadConfig();

/**
 * Entry point for the opencode-pro GitHub App.
 * Uses Probot's run() which reads APP_ID, PRIVATE_KEY, WEBHOOK_SECRET, and PORT
 * from environment variables automatically.
 */
console.log("🚀 Starting opencode-pro GitHub App...");
console.log(`   App ID: ${config.appId}`);
console.log(`   Port: ${config.port}`);
console.log(`   Model: ${config.model}`);
console.log(`   Config Dir: ${config.opencodeConfigDir}`);

run(opencodeProApp).catch((err) => {
  console.error("❌ Failed to start opencode-pro:", err);
  process.exit(1);
});
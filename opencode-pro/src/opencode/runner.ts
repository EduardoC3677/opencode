import { spawn } from "node:child_process";
import { writeFile, mkdir, unlink } from "node:fs/promises";
import { join } from "node:path";
import { tmpdir } from "node:os";
import type { TaskContext, TaskResult } from "../types.js";
import { loadConfig } from "../config.js";
import { composePrompt } from "./prompt.js";

/**
 * Run an OpenCode task by spawning the opencode CLI as a child process.
 * This is the core integration between the GitHub App and the OpenCode AI engine.
 */

const config = loadConfig();

export async function runOpenCodeTask(ctx: TaskContext): Promise<TaskResult> {
  const startTime = Date.now();
  const prompt = composePrompt(ctx);

  // Create a temporary file for the prompt
  const tmpDir = join(tmpdir(), "opencode-pro");
  await mkdir(tmpDir, { recursive: true });
  const promptFile = join(tmpDir, `prompt-${ctx.issueNumber}-${Date.now()}.txt`);
  await writeFile(promptFile, prompt, "utf-8");

  return new Promise<TaskResult>((resolve) => {
    let stdout = "";
    let stderr = "";
    let settled = false;

    const cleanup = async () => {
      try {
        await unlink(promptFile);
      } catch {
        // Best-effort cleanup — file may already be gone
      }
    };

    const child = spawn(
      "opencode",
      [
        "run",
        "--prompt-file", promptFile,
        "--model", ctx.model,
        "--config-dir", config.opencodeConfigDir,
        "--timeout", String(config.taskTimeout),
      ],
      {
        env: {
          ...process.env,
          AZURE_OPENAI_API_KEY: config.azureOpenaiApiKey,
          OPENCODE_CONFIG_DIR: config.opencodeConfigDir,
        },
        stdio: ["ignore", "pipe", "pipe"],
        timeout: config.taskTimeout * 1000,
      },
    );

    child.stdout?.on("data", (data: Buffer) => {
      stdout += data.toString();
    });

    child.stderr?.on("data", (data: Buffer) => {
      stderr += data.toString();
    });

    child.on("exit", (code, signal) => {
      if (signal === "SIGTERM") {
        stderr = stderr || `Task timed out after ${config.taskTimeout}s`;
      }
    });

    child.on("close", (code) => {
      if (settled) return;
      settled = true;

      const durationMs = Date.now() - startTime;

      if (code === 0) {
        resolve({
          success: true,
          output: stdout.trim() || "Task completed successfully.",
          durationMs,
        });
      } else {
        resolve({
          success: false,
          output: stdout.trim(),
          error: stderr.trim() || `Process exited with code ${code}`,
          durationMs,
        });
      }

      // Fire-and-forget but don't block resolution
      cleanup().catch(() => {});
    });

    child.on("error", (err) => {
      if (settled) return;
      settled = true;

      const durationMs = Date.now() - startTime;
      resolve({
        success: false,
        error: `Failed to spawn opencode process: ${err.message}`,
        durationMs,
      });

      cleanup().catch(() => {});
    });
  });
}
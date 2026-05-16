import type { TaskResult } from "../types.js";

/**
 * Format a task result into a comment body for posting to GitHub.
 */
export function formatTaskResponse(
  result: TaskResult,
  context: string,
): string {
  if (result.success) {
    return [
      `🤖 **opencode-pro** ${context}`,
      "",
      result.output
        ? `\`\`\`\n${result.output.slice(0, 60000)}\n\`\`\``
        : "✅ Tarea completada.",
      "",
      `⏱️ Duración: ${(result.durationMs / 1000).toFixed(1)}s`,
    ].join("\n");
  }

  return [
    `🤖 **opencode-pro** encontró un error.`,
    "",
    result.error
      ? `\`\`\`\n${result.error.slice(0, 2000)}\n\`\`\``
      : "❌ Error desconocido.",
    "",
    `⏱️ Duración: ${(result.durationMs / 1000).toFixed(1)}s`,
  ].join("\n");
}
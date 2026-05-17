import type { TaskResult } from "../types.js";

/**
 * Safely convert a value to a string with truncation.
 * Guards against non-string output values.
 */
function safeSlice(val: unknown, max: number): string {
  const str = typeof val === "string" ? val : String(val ?? "");
  if (str.length > max) return str.slice(0, max) + "\n...(truncated)";
  return str;
}

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
        ? `\`\`\`\n${safeSlice(result.output, 60000)}\n\`\`\``
        : "✅ Tarea completada.",
      "",
      `⏱️ Duración: ${(result.durationMs / 1000).toFixed(1)}s`,
    ].join("\n");
  }

  return [
    `🤖 **opencode-pro** encontró un error.`,
    "",
    result.error
      ? `\`\`\`\n${safeSlice(result.error, 2000)}\n\`\`\``
      : "❌ Error desconocido.",
    "",
    `⏱️ Duración: ${(result.durationMs / 1000).toFixed(1)}s`,
  ].join("\n");
}
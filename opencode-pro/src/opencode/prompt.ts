import type { TaskContext, SlashCommand } from "../types.js";

/**
 * Compose the prompt that will be sent to OpenCode.
 * This mirrors the prompt composition from the existing opencode.yml workflow.
 */

const SLASH_COMMAND_PROMPTS: Record<SlashCommand, string> = {
  "/review":
    "Realiza una revisión de código exhaustiva de este Pull Request. " +
    "Analiza la calidad del código, seguridad, rendimiento, estilo y buenas prácticas. " +
    "Proporciona sugerencias específicas y accionables. " +
    "NO implementes cambios, solo revisa y comenta.",

  "/fix":
    "Analiza este Pull Request y CORRIGE los problemas encontrados. " +
    "Implementa las correcciones necesarias en el código. " +
    "Asegúrate de que los cambios sean mínimos y enfocados en resolver los problemas identificados.",

  "/explain":
    "Explica de manera clara y detallada qué hace este Pull Request. " +
    "Describe los cambios, la arquitectura, las decisiones de diseño y cualquier consideración importante. " +
    "NO implementes cambios, solo explica.",

  "/test":
    "Analiza este Pull Request y escribe pruebas unitarias y de integración para los cambios. " +
    "Asegúrate de cubrir casos edge, errores y flujos principales. " +
    "Implementa las pruebas en los archivos apropiados.",
};

export function composePrompt(ctx: TaskContext): string {
  const parts: string[] = [];

  // Header with context
  parts.push("CONTEXTO:");
  parts.push(`event_name=${ctx.trigger}`);
  parts.push(`repository=${ctx.owner}/${ctx.repo}`);
  parts.push(`issue_number=${ctx.issueNumber}`);
  parts.push(`is_pull_request=${ctx.isPullRequest}`);
  parts.push("");

  // Issue/PR title and body
  if (ctx.title) {
    parts.push("=== TÍTULO ===");
    parts.push(ctx.title);
    parts.push("");
  }

  if (ctx.body) {
    parts.push("=== DESCRIPCIÓN ===");
    parts.push(ctx.body);
    parts.push("");
  }

  // PR diff and files for code review
  if (ctx.prFiles) {
    parts.push("=== ARCHIVOS MODIFICADOS ===");
    parts.push(ctx.prFiles);
    parts.push("");
  }

  if (ctx.prDiff) {
    parts.push("=== DIFF DEL PR ===");
    parts.push(ctx.prDiff);
    parts.push("");
  }

  // Slash command takes priority
  if (ctx.trigger === "slash_command" && ctx.command) {
    parts.push("=== COMANDO ===");
    parts.push(ctx.command);
    parts.push("");
    parts.push(SLASH_COMMAND_PROMPTS[ctx.command]);
    parts.push("");
  }

  // Comment body
  if (ctx.commentBody) {
    parts.push("=== COMENTARIO ===");
    parts.push(ctx.commentBody);
    parts.push("");
  }

  // For assigned tasks, add instruction
  if (ctx.trigger === "assigned") {
    parts.push("=== ASIGNACIÓN ===");
    parts.push(
      "Has sido asignado a este " +
        (ctx.isPullRequest ? "Pull Request" : "Issue") +
        ". Analiza el contenido y realiza la tarea solicitada.",
    );
    parts.push("");
  }

  // For mentions, add instruction
  if (ctx.trigger === "mention") {
    parts.push("=== MENCIÓN ===");
    parts.push(
      "Has sido mencionado con @opencode-pro. Lee el comentario y el contexto del " +
        (ctx.isPullRequest ? "Pull Request" : "Issue") +
        " y responde apropiadamente.",
    );
    parts.push("");
  }

  // For PR events, add auto-review instruction
  if (ctx.trigger === "pr_opened" || ctx.trigger === "pr_synchronize") {
    parts.push("=== AUTO-REVIEW ===");
    parts.push(
      "Realiza una revisión automática de este Pull Request. " +
        "Analiza el código, proporciona feedback constructivo y sugiere mejoras.",
    );
    parts.push("");
  }

  return parts.join("\n");
}
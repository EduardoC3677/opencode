import type { TaskContext, SlashCommand, AgentName } from "../types.js";

/**
 * Compose the prompt that will be sent to OpenCode.
 * This mirrors the prompt composition from the existing opencode.yml workflow.
 */

/**
 * Sanitize a value to a trimmed string, defaulting to empty string for non-strings.
 */
function safe(val: unknown): string {
  if (typeof val !== "string") return "";
  return val.trim();
}

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

  "/plan":
    "Crea un plan de implementación detallado para este Issue o Pull Request. " +
    "Divide el trabajo en fases, estima el esfuerzo, identifica dependencias y riesgos. " +
    "NO implementes cambios, solo planifica.",

  "/build":
    "Implementa los cambios solicitados en este Issue o Pull Request. " +
    "Escribe el código necesario, asegurando que compile y pase las pruebas. " +
    "Coordina la implementación delegando a sub-agentes cuando sea necesario.",

  "/refactor":
    "Refactoriza el código de este Pull Request para mejorar su estructura, " +
    "legibilidad y mantenibilidad sin cambiar su comportamiento externo. " +
    "Aplica patrones de diseño apropiados y elimina código duplicado.",

  "/docs":
    "Genera o actualiza la documentación para los cambios en este Pull Request. " +
    "Incluye documentación de API, comentarios en el código, README y guías de uso. " +
    "NO modifiques el código funcional, solo documentación.",

  "/optimize":
    "Optimiza el rendimiento del código en este Pull Request. " +
    "Identifica cuellos de botella, reduce complejidad algorítmica, " +
    "mejora uso de memoria y caching. Proporciona benchmarks si es posible.",

  "/security":
    "Realiza una auditoría de seguridad del código en este Pull Request. " +
    "Busca vulnerabilidades comunes (OWASP Top 10), problemas de autenticación, " +
    "inyección, exposición de datos, y malas prácticas de seguridad. " +
    "NO implementes cambios, solo reporta hallazgos con severidad y recomendaciones.",

  "/summarize":
    "Genera un resumen conciso de este Pull Request. " +
    "Incluye: qué cambia, por qué cambia, impacto, riesgos, y plan de pruebas. " +
    "Formatea el resumen en markdown para facilitar la revisión.",

  "/suggest":
    "Analiza este Pull Request y sugiere mejoras. " +
    "Considera calidad de código, DX, rendimiento, seguridad, testing y documentación. " +
    "Proporciona sugerencias accionables y priorizadas. NO implementes cambios.",
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
    parts.push(safe(ctx.title));
    parts.push("");
  }

  if (ctx.body) {
    parts.push("=== DESCRIPCIÓN ===");
    parts.push(safe(ctx.body));
    parts.push("");
  }

  // PR diff and files for code review
  if (ctx.prFiles) {
    parts.push("=== ARCHIVOS MODIFICADOS ===");
    parts.push(safe(ctx.prFiles));
    parts.push("");
  }

  if (ctx.prDiff) {
    parts.push("=== DIFF DEL PR ===");
    parts.push(safe(ctx.prDiff));
    parts.push("");
  }

  // Slash command takes priority
  if (ctx.trigger === "slash_command" && ctx.command && SLASH_COMMAND_PROMPTS[ctx.command]) {
    parts.push("=== COMANDO ===");
    parts.push(ctx.command);
    parts.push("");
    parts.push(SLASH_COMMAND_PROMPTS[ctx.command]);
    parts.push("");
  }

  // Comment body
  if (ctx.commentBody) {
    parts.push("=== COMENTARIO ===");
    parts.push(safe(ctx.commentBody));
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

  // Agent-specific routing instructions
  if (ctx.agent) {
    const agentInstructions: Record<AgentName, string> = {
      "plan":
        "Eres el agente plan. Solo planifica — NO implementes. Delega implementación a coder.",
      "build":
        "Eres el agente build. Coordina la implementación delegando a coder, scribe, explore y researcher. NO implementes directamente.",
      "coder":
        "Eres el agente coder. Implementa cambios en el código, ejecuta pruebas y verifica.",
      "explore":
        "Eres el agente explore. Analiza el código base y reporta hallazgos. NO modifiques archivos.",
      "researcher":
        "Eres el agente researcher. Investiga usando herramientas externas y reporta hallazgos. NO modifiques archivos.",
      "scribe":
        "Eres el agente scribe. Crea documentación y contenido. Enfócate en claridad y precisión.",
      "reviewer":
        "Eres el agente reviewer. Revisa código, identifica issues y sugiere mejoras. NO implementes cambios.",
    };
    parts.push("=== AGENTE ===");
    parts.push(agentInstructions[ctx.agent]);
    parts.push("");
  }

  return parts.join("\n");
}
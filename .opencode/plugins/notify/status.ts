type LogicalState = "idle" | "animated-busy" | "busy" | "error" | "question" | "permission"

export interface CmuxSessionStatusTransition {
  sessionID: string
  logicalState: LogicalState
}

function normalizeSessionID(value: unknown): string | null {
  return typeof value === "string" && value.trim() ? value.trim() : null
}

function normalizeState(value: unknown): string | null {
  return typeof value === "string" && value.trim() ? value.trim().toLowerCase() : null
}

function toLogicalState(state: string | null): LogicalState {
  switch (state) {
    case "idle":
    case "complete":
    case "completed":
    case "ready":
      return "idle"
    case "error":
    case "failed":
      return "error"
    case "question":
    case "awaiting_question":
      return "question"
    case "permission":
    case "awaiting_permission":
      return "permission"
    default:
      return "animated-busy"
  }
}

export function getCmuxSessionStatusText(state: Exclude<LogicalState, "animated-busy">): string {
  switch (state) {
    case "idle":
      return "Ready"
    case "busy":
      return "Working"
    case "error":
      return "Error"
    case "question":
      return "Question"
    case "permission":
      return "Permission"
  }
}

export function buildCmuxSessionStatusTransitionForQuestionTool(
  sessionID: string,
): CmuxSessionStatusTransition {
  return { sessionID, logicalState: "question" }
}

export function buildCmuxSessionStatusTransitionForEvent(
  eventType: string,
  properties: Record<string, unknown> | undefined,
): CmuxSessionStatusTransition | null {
  const sessionID = normalizeSessionID(properties?.sessionID)
  if (!sessionID) return null

  switch (eventType) {
    case "session.idle":
      return { sessionID, logicalState: "idle" }
    case "session.error":
      return { sessionID, logicalState: "error" }
    case "permission.updated":
    case "permission.asked":
      return { sessionID, logicalState: "permission" }
    case "question.asked":
      return { sessionID, logicalState: "question" }
    case "session.status":
      return { sessionID, logicalState: toLogicalState(normalizeState(properties?.status ?? properties?.state)) }
    default:
      return null
  }
}

export type ResolveExecutable = (command: string) => string | null | undefined
export type CmuxEnvironment = Record<string, string | undefined>

export interface CmuxContext {
  socketPath: string | null
  clientId: string | null
  paneId: string | null
  workspace: string | null
}

function defaultResolveExecutable(command: string): string | null | undefined {
  try {
    return Bun.which(command)
  } catch {
    return undefined
  }
}

export function detectCmuxContext(env: CmuxEnvironment = process.env): CmuxContext {
  const tmux = env.TMUX?.trim()
  const [socketPath, clientId] = tmux ? tmux.split(",", 2) : [undefined, undefined]

  return {
    socketPath: socketPath || null,
    clientId: clientId || null,
    paneId: env.TMUX_PANE?.trim() || null,
    workspace: env.CMUX_WORKSPACE?.trim() || null,
  }
}

export function canUseCmuxWorkflow(
  env: CmuxEnvironment = process.env,
  resolveExecutable: ResolveExecutable = defaultResolveExecutable,
  cmuxCommand = "cmux",
): boolean {
  const hasTmuxContext = Boolean(env.TMUX || env.TMUX_PANE)
  const resolved = resolveExecutable(cmuxCommand)
  return hasTmuxContext && Boolean(resolved)
}

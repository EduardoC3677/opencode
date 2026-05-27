export interface OscTitleContext {
  mayWriteOscTitle: boolean
  baseTitle: string
}

function sanitizeTitle(title: string): string {
  return title.replace(/[\u0007\u001b]/g, " ").trim()
}

export function parseOscTitleContext(): OscTitleContext {
  const baseTitle = sanitizeTitle(process.env.TERM_PROGRAM || process.title || "OpenCode") || "OpenCode"
  return {
    mayWriteOscTitle: Boolean(process.stdout?.isTTY),
    baseTitle,
  }
}

export function writeOscTitleBestEffort(title: string): void {
  try {
    if (!process.stdout?.isTTY) return
    process.stdout.write(`\u001b]0;${sanitizeTitle(title)}\u0007`)
  } catch {
    // best effort
  }
}

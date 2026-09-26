/** Mask an email for the Contact success screen (e.g. j***@domain.com). */
export function maskHelpCentreContactEmail(email: string): string {
  const trimmed = email.trim().toLowerCase()
  const at = trimmed.indexOf("@")

  if (at <= 0 || at === trimmed.length - 1) {
    return trimmed
  }

  const local = trimmed.slice(0, at)
  const domain = trimmed.slice(at + 1)
  const visible = local.slice(0, 1)
  return `${visible}***@${domain}`
}

/** Format a numeric query id as a public reference (e.g. TUM-000042). */
export function formatHelpCentreQueryReference(queryId: number): string {
  const safeId = Number.isFinite(queryId) && queryId > 0 ? Math.floor(queryId) : 0
  return `TUM-${String(safeId).padStart(6, "0")}`
}

export type HelpCentreContactSuccessState = {
  queryId: number
  email: string
}

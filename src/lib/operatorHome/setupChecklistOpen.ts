export const OPERATOR_SETUP_CHECKLIST_OPEN_KEY =
  "tummly-operator-setup-checklist-open"

/** Default expanded. Persists open/closed UI preference only (not progress). */
export function readSetupChecklistOpen(): boolean {
  try {
    const raw = localStorage.getItem(OPERATOR_SETUP_CHECKLIST_OPEN_KEY)
    if (raw === null) {
      return true
    }
    return raw !== "false"
  } catch {
    return true
  }
}

export function writeSetupChecklistOpen(open: boolean): void {
  try {
    localStorage.setItem(
      OPERATOR_SETUP_CHECKLIST_OPEN_KEY,
      open ? "true" : "false"
    )
  } catch {
    // ignore quota / private mode
  }
}

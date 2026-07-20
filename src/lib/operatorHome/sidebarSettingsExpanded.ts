export const OPERATOR_SIDEBAR_SETTINGS_EXPANDED_KEY =
  "tummly-operator-sidebar-settings-expanded"

/** Default open. Persists last Settings disclosure choice. */
export function readSidebarSettingsExpanded(): boolean {
  try {
    const raw = localStorage.getItem(OPERATOR_SIDEBAR_SETTINGS_EXPANDED_KEY)
    if (raw === null) return true
    return raw === "true"
  } catch {
    return true
  }
}

export function writeSidebarSettingsExpanded(expanded: boolean): void {
  try {
    localStorage.setItem(
      OPERATOR_SIDEBAR_SETTINGS_EXPANDED_KEY,
      expanded ? "true" : "false"
    )
  } catch {
    // ignore quota / private mode
  }
}

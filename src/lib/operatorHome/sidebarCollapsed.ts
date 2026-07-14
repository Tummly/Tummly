export const OPERATOR_SIDEBAR_COLLAPSED_KEY =
  "tummly-operator-sidebar-collapsed"

/** Default expanded. Persists last desktop collapse choice. */
export function readSidebarCollapsed(): boolean {
  try {
    return localStorage.getItem(OPERATOR_SIDEBAR_COLLAPSED_KEY) === "true"
  } catch {
    return false
  }
}

export function writeSidebarCollapsed(collapsed: boolean): void {
  try {
    localStorage.setItem(
      OPERATOR_SIDEBAR_COLLAPSED_KEY,
      collapsed ? "true" : "false"
    )
  } catch {
    // ignore quota / private mode
  }
}

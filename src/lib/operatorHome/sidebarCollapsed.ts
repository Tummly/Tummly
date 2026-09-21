export const OPERATOR_SIDEBAR_COLLAPSED_KEY =
  "tummly-operator-sidebar-collapsed"

/**
 * Default collapsed (hover peeks open). `false` means the hamburger pin is set
 * and the rail stays open without hover.
 */
export function readSidebarCollapsed(): boolean {
  try {
    const stored = localStorage.getItem(OPERATOR_SIDEBAR_COLLAPSED_KEY)
    if (stored === null) return true
    return stored === "true"
  } catch {
    return true
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

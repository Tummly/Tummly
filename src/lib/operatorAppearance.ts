/** Device-local storage key for Operator appearance preference (next-themes). */
export const OPERATOR_APPEARANCE_STORAGE_KEY = "tummly-theme"

export const OPERATOR_DASHBOARD_PATHS = [
  "/single-dashboard",
  "/multi-dashboard",
] as const

export const STAFF_DASHBOARD_PATH = "/admin-dashboard"

export type OperatorAppearancePreference = "light" | "dark" | "system"

export type OperatorAppearanceDocumentTheme = "light" | "dark"

export function isOperatorDashboardPath(pathname: string): boolean {
  return OPERATOR_DASHBOARD_PATHS.some(
    (path) => pathname === path || pathname.startsWith(`${path}/`)
  )
}

export function isStaffDashboardPath(pathname: string): boolean {
  return (
    pathname === STAFF_DASHBOARD_PATH ||
    pathname.startsWith(`${STAFF_DASHBOARD_PATH}/`)
  )
}

/** Operator or staff shell — theme toggle allowed (not forced light). */
export function isThemedAppShellPath(pathname: string): boolean {
  return isOperatorDashboardPath(pathname) || isStaffDashboardPath(pathname)
}

export function parseOperatorAppearancePreference(
  value: string | null | undefined
): OperatorAppearancePreference {
  if (value === "light" || value === "dark" || value === "system") {
    return value
  }
  return "system"
}

export function resolveOperatorAppearanceDocumentTheme(input: {
  themeEnabled: boolean
  preference: OperatorAppearancePreference
  systemPrefersDark: boolean
}): OperatorAppearanceDocumentTheme {
  if (!input.themeEnabled) {
    return "light"
  }
  if (input.preference === "system") {
    return input.systemPrefersDark ? "dark" : "light"
  }
  return input.preference
}

export function readSystemPrefersDark(): boolean {
  if (typeof window === "undefined") {
    return false
  }
  return window.matchMedia("(prefers-color-scheme: dark)").matches
}

/** Apply Operator/`op` scope + document theme before paint (`op` / `dark` + color-scheme). */
export function applyOperatorAppearanceDocumentTheme(input: {
  /** Enables Operator design tokens (`html.op`) — Operator and staff shells. */
  applyOpScope: boolean
  theme: OperatorAppearanceDocumentTheme
}): void {
  const root = document.documentElement
  root.classList.toggle("op", input.applyOpScope)
  root.classList.toggle("dark", input.theme === "dark")
  root.style.colorScheme = input.theme
}

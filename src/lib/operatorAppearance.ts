/** Device-local storage key for Operator appearance preference (next-themes). */
export const OPERATOR_APPEARANCE_STORAGE_KEY = "tummly-theme"

export const OPERATOR_DASHBOARD_PATHS = [
  "/single-dashboard",
  "/multi-dashboard",
] as const

export type OperatorAppearancePreference = "light" | "dark" | "system"

export type OperatorAppearanceDocumentTheme = "light" | "dark"

export function isOperatorDashboardPath(pathname: string): boolean {
  return (OPERATOR_DASHBOARD_PATHS as readonly string[]).includes(pathname)
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
  isOperatorDashboard: boolean
  preference: OperatorAppearancePreference
  systemPrefersDark: boolean
}): OperatorAppearanceDocumentTheme {
  if (!input.isOperatorDashboard) {
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

/** Apply document theme before paint (class + color-scheme). */
export function applyOperatorAppearanceDocumentTheme(
  theme: OperatorAppearanceDocumentTheme
): void {
  const root = document.documentElement
  root.classList.toggle("dark", theme === "dark")
  root.style.colorScheme = theme
}

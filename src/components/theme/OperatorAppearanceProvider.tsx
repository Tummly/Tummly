import { useLayoutEffect, type ReactNode } from "react"
import { ThemeProvider, useTheme } from "next-themes"
import { useLocation } from "react-router-dom"

import {
  OPERATOR_APPEARANCE_STORAGE_KEY,
  applyOperatorAppearanceDocumentTheme,
  isOperatorDashboardPath,
  parseOperatorAppearancePreference,
  readSystemPrefersDark,
  resolveOperatorAppearanceDocumentTheme,
} from "@/lib/operatorAppearance"

function OperatorAppearanceDocumentSync({
  isOperatorDashboard,
}: {
  isOperatorDashboard: boolean
}) {
  const { theme, systemTheme } = useTheme()

  useLayoutEffect(() => {
    const preference = parseOperatorAppearancePreference(theme)
    const systemPrefersDark =
      systemTheme === "dark" ||
      (systemTheme === "light" ? false : readSystemPrefersDark())

    applyOperatorAppearanceDocumentTheme({
      isOperatorDashboard,
      theme: resolveOperatorAppearanceDocumentTheme({
        isOperatorDashboard,
        preference,
        systemPrefersDark,
      }),
    })
  }, [isOperatorDashboard, theme, systemTheme])

  return null
}

export function OperatorAppearanceProvider({
  children,
}: {
  children: ReactNode
}) {
  const { pathname } = useLocation()
  const isOperatorDashboard = isOperatorDashboardPath(pathname)

  return (
    <ThemeProvider
      attribute="class"
      defaultTheme="system"
      enableSystem
      storageKey={OPERATOR_APPEARANCE_STORAGE_KEY}
      forcedTheme={isOperatorDashboard ? undefined : "light"}
      disableTransitionOnChange
    >
      <OperatorAppearanceDocumentSync
        isOperatorDashboard={isOperatorDashboard}
      />
      {children}
    </ThemeProvider>
  )
}

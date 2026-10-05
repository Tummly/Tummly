import { useLayoutEffect, type ReactNode } from "react"
import { ThemeProvider, useTheme } from "next-themes"
import { useLocation } from "react-router-dom"

import {
  OPERATOR_APPEARANCE_STORAGE_KEY,
  applyOperatorAppearanceDocumentTheme,
  isThemedAppShellPath,
  parseOperatorAppearancePreference,
  readSystemPrefersDark,
  resolveOperatorAppearanceDocumentTheme,
} from "@/lib/operatorAppearance"

function OperatorAppearanceDocumentSync({
  themeEnabled,
}: {
  themeEnabled: boolean
}) {
  const { theme, systemTheme } = useTheme()

  useLayoutEffect(() => {
    const preference = parseOperatorAppearancePreference(theme)
    const systemPrefersDark =
      systemTheme === "dark" ||
      (systemTheme === "light" ? false : readSystemPrefersDark())

    applyOperatorAppearanceDocumentTheme({
      applyOpScope: themeEnabled,
      theme: resolveOperatorAppearanceDocumentTheme({
        themeEnabled,
        preference,
        systemPrefersDark,
      }),
    })
  }, [theme, systemTheme, themeEnabled])

  return null
}

export function OperatorAppearanceProvider({
  children,
}: {
  children: ReactNode
}) {
  const { pathname } = useLocation()
  const themeEnabled = isThemedAppShellPath(pathname)

  return (
    <ThemeProvider
      attribute="class"
      defaultTheme="system"
      enableSystem
      storageKey={OPERATOR_APPEARANCE_STORAGE_KEY}
      forcedTheme={themeEnabled ? undefined : "light"}
      disableTransitionOnChange
    >
      <OperatorAppearanceDocumentSync themeEnabled={themeEnabled} />
      {children}
    </ThemeProvider>
  )
}

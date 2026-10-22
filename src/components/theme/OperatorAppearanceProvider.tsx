import { useEffect, useLayoutEffect, useState, type ReactNode } from "react"
import { ThemeProvider, useTheme } from "next-themes"
import { useLocation } from "react-router-dom"

import {
  OPERATOR_APPEARANCE_STORAGE_KEY,
  applyOperatorAppearanceDocumentTheme,
  isOperatorTokenScopePath,
  isThemedAppShellPath,
  parseOperatorAppearancePreference,
  readSystemPrefersDark,
  resolveOperatorAppearanceDocumentTheme,
} from "@/lib/operatorAppearance"
import { isDemoTourActive } from "@/lib/productDemo/demoTourSession"

function useDemoTourForceLight(): boolean {
  const [forceLight, setForceLight] = useState(() => isDemoTourActive())

  useEffect(() => {
    const sync = () => setForceLight(isDemoTourActive())
    window.addEventListener("tummly-demo-tour-change", sync)
    window.addEventListener("storage", sync)
    return () => {
      window.removeEventListener("tummly-demo-tour-change", sync)
      window.removeEventListener("storage", sync)
    }
  }, [])

  return forceLight
}

function OperatorAppearanceDocumentSync({
  themeEnabled,
  opScope,
  forceLight,
}: {
  themeEnabled: boolean
  opScope: boolean
  forceLight: boolean
}) {
  const { theme, systemTheme } = useTheme()

  useLayoutEffect(() => {
    const preference = parseOperatorAppearancePreference(theme)
    const systemPrefersDark =
      systemTheme === "dark" ||
      (systemTheme === "light" ? false : readSystemPrefersDark())

    const resolved = forceLight
      ? "light"
      : resolveOperatorAppearanceDocumentTheme({
          themeEnabled,
          preference,
          systemPrefersDark,
        })

    applyOperatorAppearanceDocumentTheme({
      applyOpScope: opScope,
      theme: resolved,
    })
  }, [theme, systemTheme, themeEnabled, opScope, forceLight])

  return null
}

export function OperatorAppearanceProvider({
  children,
}: {
  children: ReactNode
}) {
  const { pathname } = useLocation()
  const themeEnabled = isThemedAppShellPath(pathname)
  const opScope = isOperatorTokenScopePath(pathname)
  const forceLight = useDemoTourForceLight()
  const forcedTheme =
    !themeEnabled || forceLight ? "light" : undefined

  return (
    <ThemeProvider
      attribute="class"
      defaultTheme="system"
      enableSystem
      storageKey={OPERATOR_APPEARANCE_STORAGE_KEY}
      forcedTheme={forcedTheme}
      disableTransitionOnChange
    >
      <OperatorAppearanceDocumentSync
        themeEnabled={themeEnabled}
        opScope={opScope}
        forceLight={forceLight}
      />
      {children}
    </ThemeProvider>
  )
}

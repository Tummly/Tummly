import { useEffect } from "react"
import { useLocation } from "react-router-dom"

/**
 * Resets window scroll on pathname changes.
 * Hash-only updates (e.g. in-page TOC) are left alone so page-local scroll handlers keep working.
 */
export function ScrollToTop() {
  const { pathname } = useLocation()

  useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: "auto" })
  }, [pathname])

  return null
}

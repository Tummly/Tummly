import { useEffect } from "react"
import { useSearchParams } from "react-router-dom"

type Options = {
  /** When active tab equals this id, omit `tab` from the URL (default tab). */
  defaultTabId?: string
}

/**
 * Writes module tab state to `?tab=` after user interaction.
 * Omits `searchParams` from the effect deps so URL→module sync in
 * *PageModuleProvider does not race this write on deep links.
 */
export function useWriteActiveTabToSearchParams(
  activeTabId: string,
  options?: Options
) {
  const [searchParams, setSearchParams] = useSearchParams()
  const defaultTabId = options?.defaultTabId

  useEffect(() => {
    const current = searchParams.get("tab")
    if (current === activeTabId) {
      return
    }
    if (defaultTabId != null && activeTabId === defaultTabId && current == null) {
      return
    }

    const next = new URLSearchParams(searchParams)
    if (defaultTabId != null && activeTabId === defaultTabId) {
      next.delete("tab")
    } else {
      next.set("tab", activeTabId)
    }
    setSearchParams(next, { replace: true })
    // eslint-disable-next-line react-hooks/exhaustive-deps -- see hook docstring
  }, [activeTabId, defaultTabId, setSearchParams])
}

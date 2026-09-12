import { useEffect, useRef, useSyncExternalStore } from "react"

import {
  createOperatorGlobalSearchModule,
  type OperatorGlobalSearchModule,
  type OperatorGlobalSearchSnapshot,
} from "@/lib/operatorGlobalSearch/createOperatorGlobalSearchModule"

function readIsApplePlatform(): boolean {
  if (typeof navigator === "undefined") {
    return false
  }
  const platform = navigator.platform || ""
  const ua = navigator.userAgent || ""
  return /Mac|iPhone|iPad|iPod/i.test(platform) || /Mac OS|iPhone|iPad|iPod/i.test(ua)
}

export type OperatorGlobalSearchHandoff = (prompt: string) => void

export type OperatorGlobalSearchApi = {
  snapshot: OperatorGlobalSearchSnapshot
  open: () => void
  close: () => void
  setOpen: (open: boolean) => void
  setQuery: (query: string) => void
  selectSuggestion: (suggestionId: string) => void
}

export function useGlobalSearchModule(
  handoffSuggestionToAssistant: OperatorGlobalSearchHandoff
): OperatorGlobalSearchApi {
  const handoffRef = useRef(handoffSuggestionToAssistant)
  handoffRef.current = handoffSuggestionToAssistant

  const moduleRef = useRef<OperatorGlobalSearchModule | null>(null)
  if (moduleRef.current == null) {
    moduleRef.current = createOperatorGlobalSearchModule(
      {
        handoffSuggestionToAssistant: (prompt) => {
          handoffRef.current(prompt)
        },
      },
      { isApplePlatform: readIsApplePlatform }
    )
  }
  const search = moduleRef.current

  const snapshot = useSyncExternalStore(
    search.subscribe,
    search.getSnapshot,
    search.getSnapshot
  )

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const handled = search.handleShortcutKeydown({
        key: event.key,
        metaKey: event.metaKey,
        ctrlKey: event.ctrlKey,
        isApplePlatform: readIsApplePlatform(),
      })
      if (handled) {
        event.preventDefault()
      }
    }

    window.addEventListener("keydown", onKeyDown)
    return () => {
      window.removeEventListener("keydown", onKeyDown)
    }
  }, [search])

  return {
    snapshot,
    open: search.open,
    close: search.close,
    setOpen: search.setOpen,
    setQuery: search.setQuery,
    selectSuggestion: search.selectSuggestion,
  }
}

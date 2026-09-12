export type OperatorGlobalSearchSuggestion = {
  id: string
  prompt: string
}

/** Empty-state AI prompts (Figma 6518:13702). Product hits land in later tickets. */
export const EMPTY_AI_SUGGESTIONS: readonly OperatorGlobalSearchSuggestion[] = [
  { id: "analyse-delivery-feedback", prompt: "Analyse delivery Feedback" },
  {
    id: "what-changed-delivery-feedback",
    prompt: "What changed with delivery Feedback?",
  },
  { id: "prepare-response-plan", prompt: "Prepare a response plan" },
] as const

export type OperatorGlobalSearchSnapshot = {
  open: boolean
  query: string
  emptySuggestions: readonly OperatorGlobalSearchSuggestion[]
  /** Modifier glyph for the closed Search field Kbd hint. */
  shortcutModifierLabel: string
}

export type OperatorGlobalSearchShortcutInput = {
  key: string
  metaKey: boolean
  ctrlKey: boolean
  isApplePlatform: boolean
}

export type OperatorGlobalSearchAdapters = {
  /**
   * Close Search side effects already applied by the module.
   * Open AI Assistant and put `prompt` in the composer — do not auto-send.
   * Soft lock / credits may still gate Send inside Assistant.
   */
  handoffSuggestionToAssistant: (prompt: string) => void
}

export type OperatorGlobalSearchModule = {
  getSnapshot: () => OperatorGlobalSearchSnapshot
  subscribe: (listener: () => void) => () => void
  open: () => void
  close: () => void
  setOpen: (open: boolean) => void
  setQuery: (query: string) => void
  dismissFromEscape: () => void
  /** Returns true when the shortcut opened Search (caller should preventDefault). */
  handleShortcutKeydown: (input: OperatorGlobalSearchShortcutInput) => boolean
  selectSuggestion: (suggestionId: string) => void
}

export type OperatorGlobalSearchModuleOptions = {
  isApplePlatform?: () => boolean
}

type SearchState = {
  open: boolean
  query: string
}

export function isGlobalSearchOpenShortcut(
  input: OperatorGlobalSearchShortcutInput
): boolean {
  if (input.key.toLowerCase() !== "k") {
    return false
  }
  if (input.isApplePlatform) {
    return input.metaKey && !input.ctrlKey
  }
  return input.ctrlKey && !input.metaKey
}

export function shortcutModifierLabel(isApplePlatform: boolean): string {
  return isApplePlatform ? "⌘" : "Ctrl"
}

export function createOperatorGlobalSearchModule(
  adapters: OperatorGlobalSearchAdapters,
  options: OperatorGlobalSearchModuleOptions = {}
): OperatorGlobalSearchModule {
  const isApplePlatform = options.isApplePlatform ?? (() => false)
  let state: SearchState = { open: false, query: "" }
  const listeners = new Set<() => void>()

  const publish = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const getSnapshot = (): OperatorGlobalSearchSnapshot => ({
    open: state.open,
    query: state.query,
    emptySuggestions: EMPTY_AI_SUGGESTIONS,
    shortcutModifierLabel: shortcutModifierLabel(isApplePlatform()),
  })

  const close = () => {
    if (!state.open && state.query === "") {
      return
    }
    state = { open: false, query: "" }
    publish()
  }

  const open = () => {
    if (state.open) {
      return
    }
    state = { ...state, open: true }
    publish()
  }

  return {
    getSnapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    open,
    close,
    setOpen: (next) => {
      if (next) {
        open()
      } else {
        close()
      }
    },
    setQuery: (query) => {
      if (state.query === query) {
        return
      }
      state = { ...state, query }
      publish()
    },
    dismissFromEscape: () => {
      close()
    },
    handleShortcutKeydown: (input) => {
      if (!isGlobalSearchOpenShortcut(input)) {
        return false
      }
      if (state.open) {
        return false
      }
      open()
      return true
    },
    selectSuggestion: (suggestionId) => {
      const suggestion = EMPTY_AI_SUGGESTIONS.find((row) => row.id === suggestionId)
      if (suggestion == null) {
        return
      }
      close()
      try {
        adapters.handoffSuggestionToAssistant(suggestion.prompt)
      } catch {
        // Search must stay usable even if Assistant handoff fails.
      }
    },
  }
}

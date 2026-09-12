import { getOperatorInitials } from "@/lib/operatorHome/operatorProfile"

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

export type OperatorGlobalSearchEntityHit = {
  id: string
  title: string
  subtitle: string | null
  status?: string | null
  locationId: number
  initials: string
}

export type OperatorGlobalSearchSnapshot = {
  open: boolean
  query: string
  emptySuggestions: readonly OperatorGlobalSearchSuggestion[]
  /** Modifier glyph for the closed Search field Kbd hint. */
  shortcutModifierLabel: string
  hitsPending: boolean
  guestHits: readonly OperatorGlobalSearchEntityHit[]
  feedbackHits: readonly OperatorGlobalSearchEntityHit[]
  campaignHits: readonly OperatorGlobalSearchEntityHit[]
  offerHits: readonly OperatorGlobalSearchEntityHit[]
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
  searchHits: (args: {
    q: string
    locationId: number
    signal?: AbortSignal
  }) => Promise<{
    guestHits: readonly OperatorGlobalSearchEntityHit[]
    feedbackHits: readonly OperatorGlobalSearchEntityHit[]
    campaignHits: readonly OperatorGlobalSearchEntityHit[]
    offerHits: readonly OperatorGlobalSearchEntityHit[]
  }>
  navigateToGuestProfile: (guestId: number, locationId: number) => void
  navigateToFeedbackDetail: (feedbackId: number, locationId: number) => void
  navigateToCampaignDetail: (campaignId: number, locationId: number) => void
  navigateToOfferDetails: (offerId: number, locationId: number) => void
  getLocationId: () => number | null
  debounceMs?: number
  setTimeout?: typeof globalThis.setTimeout
  clearTimeout?: typeof globalThis.clearTimeout
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
  selectGuestHit: (guestId: string) => void
  selectFeedbackHit: (feedbackId: string) => void
  selectCampaignHit: (campaignId: string) => void
  selectOfferHit: (offerId: string) => void
}

export type OperatorGlobalSearchModuleOptions = {
  isApplePlatform?: () => boolean
}

const DEFAULT_DEBOUNCE_MS = 250
const MIN_QUERY_LENGTH = 2

type SearchState = {
  open: boolean
  query: string
  hitsPending: boolean
  guestHits: readonly OperatorGlobalSearchEntityHit[]
  feedbackHits: readonly OperatorGlobalSearchEntityHit[]
  campaignHits: readonly OperatorGlobalSearchEntityHit[]
  offerHits: readonly OperatorGlobalSearchEntityHit[]
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

function toSnapshot(
  state: SearchState,
  isApplePlatform: () => boolean
): OperatorGlobalSearchSnapshot {
  return {
    open: state.open,
    query: state.query,
    emptySuggestions: EMPTY_AI_SUGGESTIONS,
    shortcutModifierLabel: shortcutModifierLabel(isApplePlatform()),
    hitsPending: state.hitsPending,
    guestHits: state.guestHits,
    feedbackHits: state.feedbackHits,
    campaignHits: state.campaignHits,
    offerHits: state.offerHits,
  }
}

export function mapSearchHit(raw: {
  id: string
  title: string
  subtitle?: string | null
  status?: string | null
  locationId: number
}): OperatorGlobalSearchEntityHit {
  return {
    id: raw.id,
    title: raw.title,
    subtitle: raw.subtitle ?? null,
    status: raw.status ?? null,
    locationId: raw.locationId,
    initials: getOperatorInitials(raw.title),
  }
}

export function createOperatorGlobalSearchModule(
  adapters: OperatorGlobalSearchAdapters,
  options: OperatorGlobalSearchModuleOptions = {}
): OperatorGlobalSearchModule {
  const isApplePlatform = options.isApplePlatform ?? (() => false)
  const debounceMs = adapters.debounceMs ?? DEFAULT_DEBOUNCE_MS
  const scheduleTimeout = adapters.setTimeout ?? globalThis.setTimeout
  const cancelTimeout = adapters.clearTimeout ?? globalThis.clearTimeout

  let state: SearchState = {
    open: false,
    query: "",
    hitsPending: false,
    guestHits: [],
    feedbackHits: [],
    campaignHits: [],
    offerHits: [],
  }
  let snapshot = toSnapshot(state, isApplePlatform)
  const listeners = new Set<() => void>()
  let searchGeneration = 0
  let debounceTimer: ReturnType<typeof setTimeout> | null = null
  let activeAbort: AbortController | null = null

  const publish = () => {
    snapshot = toSnapshot(state, isApplePlatform)
    for (const listener of listeners) {
      listener()
    }
  }

  const clearPendingSearch = () => {
    if (debounceTimer != null) {
      cancelTimeout(debounceTimer)
      debounceTimer = null
    }
    if (activeAbort != null) {
      activeAbort.abort()
      activeAbort = null
    }
  }

  const clearHits = () => {
    searchGeneration += 1
    state = {
      ...state,
      hitsPending: false,
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
    }
    publish()
  }

  const runSearch = (q: string, locationId: number) => {
    const generation = ++searchGeneration
    clearPendingSearch()
    const abort = new AbortController()
    activeAbort = abort

    if (!state.hitsPending) {
      state = { ...state, hitsPending: true }
      publish()
    }

    void adapters
      .searchHits({ q, locationId, signal: abort.signal })
      .then((result) => {
        if (generation !== searchGeneration) {
          return
        }
        state = {
          ...state,
          hitsPending: false,
          guestHits: result.guestHits,
          feedbackHits: result.feedbackHits,
          campaignHits: result.campaignHits,
          offerHits: result.offerHits,
        }
        publish()
      })
      .catch(() => {
        if (generation !== searchGeneration) {
          return
        }
        state = {
          ...state,
          hitsPending: false,
          guestHits: [],
          feedbackHits: [],
          campaignHits: [],
          offerHits: [],
        }
        publish()
      })
  }

  const scheduleSearch = () => {
    clearPendingSearch()
    const trimmed = state.query.trim()
    const locationId = adapters.getLocationId()

    if (!state.open || trimmed.length < MIN_QUERY_LENGTH || locationId == null) {
      if (
        state.hitsPending ||
        state.guestHits.length > 0 ||
        state.feedbackHits.length > 0 ||
        state.campaignHits.length > 0 ||
        state.offerHits.length > 0
      ) {
        clearHits()
      }
      return
    }

    debounceTimer = scheduleTimeout(() => {
      debounceTimer = null
      const latestTrimmed = state.query.trim()
      const latestLocationId = adapters.getLocationId()
      if (
        !state.open ||
        latestTrimmed.length < MIN_QUERY_LENGTH ||
        latestLocationId == null
      ) {
        return
      }
      runSearch(latestTrimmed, latestLocationId)
    }, debounceMs)
  }

  const close = () => {
    if (
      !state.open &&
      state.query === "" &&
      !state.hitsPending &&
      state.guestHits.length === 0 &&
      state.feedbackHits.length === 0 &&
      state.campaignHits.length === 0 &&
      state.offerHits.length === 0
    ) {
      return
    }
    clearPendingSearch()
    searchGeneration += 1
    state = {
      open: false,
      query: "",
      hitsPending: false,
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
    }
    publish()
  }

  const open = () => {
    if (state.open) {
      return
    }
    state = { ...state, open: true }
    publish()
  }

  const selectEntityHit = (
    hitId: string,
    hits: readonly OperatorGlobalSearchEntityHit[],
    navigate: (id: number, locationId: number) => void
  ) => {
    const hit = hits.find((row) => row.id === hitId)
    if (hit == null) {
      return
    }
    const parsedId = Number.parseInt(hitId, 10)
    if (!Number.isFinite(parsedId)) {
      return
    }
    close()
    try {
      navigate(parsedId, hit.locationId)
    } catch {
      // Search must stay usable even if navigation fails.
    }
  }

  return {
    getSnapshot: () => snapshot,
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
      scheduleSearch()
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
    selectGuestHit: (guestId) => {
      selectEntityHit(guestId, state.guestHits, adapters.navigateToGuestProfile)
    },
    selectFeedbackHit: (feedbackId) => {
      selectEntityHit(
        feedbackId,
        state.feedbackHits,
        adapters.navigateToFeedbackDetail
      )
    },
    selectCampaignHit: (campaignId) => {
      selectEntityHit(
        campaignId,
        state.campaignHits,
        adapters.navigateToCampaignDetail
      )
    },
    selectOfferHit: (offerId) => {
      selectEntityHit(offerId, state.offerHits, adapters.navigateToOfferDetails)
    },
  }
}

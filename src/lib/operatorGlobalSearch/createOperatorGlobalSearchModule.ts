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

export type OperatorGlobalSearchLocationScope = "current" | "all"

export type OperatorGlobalSearchStatus =
  | "idle"
  | "loading"
  | "ready"
  | "error"
  | "offline"
  | "partial"

export type OperatorGlobalSearchListEntity =
  | "guests"
  | "feedback"
  | "campaigns"
  | "offers"
  | "qr-codes"

export type OperatorGlobalSearchEntityHit = {
  id: string
  title: string
  subtitle: string | null
  status?: string | null
  locationId: number
  initials: string
}

export type OperatorGlobalSearchHitsResult = {
  guestHits: readonly OperatorGlobalSearchEntityHit[]
  feedbackHits: readonly OperatorGlobalSearchEntityHit[]
  campaignHits: readonly OperatorGlobalSearchEntityHit[]
  offerHits: readonly OperatorGlobalSearchEntityHit[]
  qrCodeHits: readonly OperatorGlobalSearchEntityHit[]
  failedTypes?: readonly string[]
}

export type OperatorGlobalSearchAnalyticsEvent = {
  name: string
  props: Record<string, string | number | boolean>
}

export type OperatorGlobalSearchSnapshot = {
  open: boolean
  query: string
  emptySuggestions: readonly OperatorGlobalSearchSuggestion[]
  typedSuggestions: readonly OperatorGlobalSearchSuggestion[]
  /** Modifier glyph for the closed Search field Kbd hint. */
  shortcutModifierLabel: string
  hitsPending: boolean
  guestHits: readonly OperatorGlobalSearchEntityHit[]
  feedbackHits: readonly OperatorGlobalSearchEntityHit[]
  campaignHits: readonly OperatorGlobalSearchEntityHit[]
  offerHits: readonly OperatorGlobalSearchEntityHit[]
  qrCodeHits: readonly OperatorGlobalSearchEntityHit[]
  locationScope: OperatorGlobalSearchLocationScope
  canWidenLocationScope: boolean
  showWidenFromNoResults: boolean
  showNoResults: boolean
  showViewAllGuests: boolean
  showViewAllFeedback: boolean
  showViewAllCampaigns: boolean
  showViewAllOffers: boolean
  showViewAllQrCodes: boolean
  searchStatus: OperatorGlobalSearchStatus
  showError: boolean
  showOffline: boolean
  showPartialWarning: boolean
  failedGroupTypes: readonly string[]
  resultCountAnnouncement: string
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
    scope: OperatorGlobalSearchLocationScope
    signal?: AbortSignal
  }) => Promise<OperatorGlobalSearchHitsResult>
  navigateToGuestProfile: (guestId: number, locationId: number) => void
  navigateToFeedbackDetail: (feedbackId: number, locationId: number) => void
  navigateToCampaignDetail: (campaignId: number, locationId: number) => void
  navigateToOfferDetails: (offerId: number, locationId: number) => void
  navigateToCapturePlacementDetail: (
    qrCodeId: number,
    locationId: number
  ) => void
  navigateToEntityList: (args: {
    entity: OperatorGlobalSearchListEntity
    q: string
    locationId: number
    scope: OperatorGlobalSearchLocationScope
  }) => void
  isOnline?: () => boolean
  trackAnalytics?: (event: OperatorGlobalSearchAnalyticsEvent) => void
  getLocationId: () => number | null
  /** Shell Owned-location list length (already filtered to authorised). */
  getAuthorisedLocationCount: () => number
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
  selectQrCodeHit: (qrCodeId: string) => void
  viewAllGuests: () => void
  viewAllFeedback: () => void
  viewAllCampaigns: () => void
  viewAllOffers: () => void
  viewAllQrCodes: () => void
  retrySearch: () => void
  setLocationScope: (scope: OperatorGlobalSearchLocationScope) => void
  widenToAllLocations: () => void
  /**
   * Shell Owned-location switcher changed while Search is open.
   * Resets to current-location scope and refreshes for the new locationId.
   */
  notifyOwnedLocationChanged: () => void
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
  qrCodeHits: readonly OperatorGlobalSearchEntityHit[]
  locationScope: OperatorGlobalSearchLocationScope
  /** True after a search has settled for the current query+scope. */
  searchSettled: boolean
  searchStatus: OperatorGlobalSearchStatus
  failedGroupTypes: readonly string[]
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

export function buildTypedAskTummlySuggestions(
  trimmedQuery: string
): readonly OperatorGlobalSearchSuggestion[] {
  if (trimmedQuery.length < MIN_QUERY_LENGTH) {
    return []
  }
  return [
    {
      id: "ask-analyse-feedback",
      prompt: `Analyse Feedback mentioning "${trimmedQuery}"`,
    },
    {
      id: "ask-what-changed",
      prompt: `What changed about "${trimmedQuery}"?`,
    },
  ]
}

function hasAnyHits(state: SearchState): boolean {
  return (
    state.guestHits.length > 0 ||
    state.feedbackHits.length > 0 ||
    state.campaignHits.length > 0 ||
    state.offerHits.length > 0 ||
    state.qrCodeHits.length > 0
  )
}

function totalHitCount(state: SearchState): number {
  return (
    state.guestHits.length +
    state.feedbackHits.length +
    state.campaignHits.length +
    state.offerHits.length +
    state.qrCodeHits.length
  )
}

function isAbortError(error: unknown): boolean {
  if (error == null || typeof error !== "object") {
    return false
  }
  const named = error as { name?: string; code?: string }
  return named.name === "AbortError" || named.code === "ERR_CANCELED"
}

function buildResultCountAnnouncement(state: SearchState): string {
  const trimmed = state.query.trim()
  if (
    !state.open ||
    trimmed.length < MIN_QUERY_LENGTH ||
    state.hitsPending ||
    state.searchStatus === "loading" ||
    state.searchStatus === "idle" ||
    state.searchStatus === "error" ||
    state.searchStatus === "offline"
  ) {
    return ""
  }
  const count = totalHitCount(state)
  if (count === 0) {
    return "No results"
  }
  if (count === 1) {
    return "1 result"
  }
  return `${count} results`
}

function toSnapshot(
  state: SearchState,
  isApplePlatform: () => boolean,
  getAuthorisedLocationCount: () => number
): OperatorGlobalSearchSnapshot {
  const canWidenLocationScope = getAuthorisedLocationCount() > 1
  const trimmed = state.query.trim()
  const typedSuggestions =
    state.open && trimmed.length >= MIN_QUERY_LENGTH
      ? buildTypedAskTummlySuggestions(trimmed)
      : []
  const settledReady =
    state.searchSettled &&
    !state.hitsPending &&
    trimmed.length >= MIN_QUERY_LENGTH &&
    (state.searchStatus === "ready" || state.searchStatus === "partial")
  const showNoResults = settledReady && !hasAnyHits(state)
  const showWidenFromNoResults =
    showNoResults &&
    canWidenLocationScope &&
    state.locationScope === "current"
  const showViewAll =
    settledReady ||
    (state.searchSettled &&
      !state.hitsPending &&
      trimmed.length >= MIN_QUERY_LENGTH &&
      state.searchStatus === "partial")

  return {
    open: state.open,
    query: state.query,
    emptySuggestions: EMPTY_AI_SUGGESTIONS,
    typedSuggestions,
    shortcutModifierLabel: shortcutModifierLabel(isApplePlatform()),
    hitsPending: state.hitsPending,
    guestHits: state.guestHits,
    feedbackHits: state.feedbackHits,
    campaignHits: state.campaignHits,
    offerHits: state.offerHits,
    qrCodeHits: state.qrCodeHits,
    locationScope: state.locationScope,
    canWidenLocationScope,
    showWidenFromNoResults,
    showNoResults,
    showViewAllGuests: showViewAll && state.guestHits.length > 0,
    showViewAllFeedback: showViewAll && state.feedbackHits.length > 0,
    showViewAllCampaigns: showViewAll && state.campaignHits.length > 0,
    showViewAllOffers: showViewAll && state.offerHits.length > 0,
    showViewAllQrCodes: showViewAll && state.qrCodeHits.length > 0,
    searchStatus: state.searchStatus,
    showError: state.searchStatus === "error",
    showOffline: state.searchStatus === "offline",
    showPartialWarning: state.searchStatus === "partial",
    failedGroupTypes: state.failedGroupTypes,
    resultCountAnnouncement: buildResultCountAnnouncement(state),
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

function initialSearchState(): SearchState {
  return {
    open: false,
    query: "",
    hitsPending: false,
    guestHits: [],
    feedbackHits: [],
    campaignHits: [],
    offerHits: [],
    qrCodeHits: [],
    locationScope: "current",
    searchSettled: false,
    searchStatus: "idle",
    failedGroupTypes: [],
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
  const isOnline = adapters.isOnline ?? (() => true)
  const trackAnalytics =
    adapters.trackAnalytics ??
    ((_event: OperatorGlobalSearchAnalyticsEvent) => {})

  let state: SearchState = initialSearchState()
  let snapshot = toSnapshot(
    state,
    isApplePlatform,
    adapters.getAuthorisedLocationCount
  )
  const listeners = new Set<() => void>()
  let searchGeneration = 0
  let debounceTimer: ReturnType<typeof setTimeout> | null = null
  let activeAbort: AbortController | null = null
  let queryStartedForOpen = false
  let searchStartedAtMs = 0

  const publish = () => {
    snapshot = toSnapshot(
      state,
      isApplePlatform,
      adapters.getAuthorisedLocationCount
    )
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
      qrCodeHits: [],
      searchSettled: false,
      searchStatus: "idle",
      failedGroupTypes: [],
    }
    publish()
  }

  const runSearch = (
    q: string,
    locationId: number,
    scope: OperatorGlobalSearchLocationScope
  ) => {
    if (!isOnline()) {
      clearPendingSearch()
      searchGeneration += 1
      state = {
        ...state,
        hitsPending: false,
        guestHits: [],
        feedbackHits: [],
        campaignHits: [],
        offerHits: [],
        qrCodeHits: [],
        searchSettled: true,
        searchStatus: "offline",
        failedGroupTypes: [],
      }
      publish()
      trackAnalytics({
        name: "global_search_error",
        props: {
          reason: "offline",
          queryLength: q.length,
          scope,
        },
      })
      return
    }

    const generation = ++searchGeneration
    clearPendingSearch()
    const abort = new AbortController()
    activeAbort = abort
    searchStartedAtMs = Date.now()

    if (!state.hitsPending) {
      state = {
        ...state,
        hitsPending: true,
        searchSettled: false,
        searchStatus: "loading",
      }
      publish()
    } else {
      state = {
        ...state,
        searchSettled: false,
        searchStatus: "loading",
      }
    }

    void adapters
      .searchHits({ q, locationId, scope, signal: abort.signal })
      .then((result) => {
        if (generation !== searchGeneration) {
          return
        }
        const failedTypes = result.failedTypes ?? []
        const nextStatus: OperatorGlobalSearchStatus =
          failedTypes.length > 0 ? "partial" : "ready"
        state = {
          ...state,
          hitsPending: false,
          guestHits: result.guestHits,
          feedbackHits: result.feedbackHits,
          campaignHits: result.campaignHits,
          offerHits: result.offerHits,
          qrCodeHits: result.qrCodeHits,
          searchSettled: true,
          searchStatus: nextStatus,
          failedGroupTypes: failedTypes,
        }
        publish()

        const resultCount =
          result.guestHits.length +
          result.feedbackHits.length +
          result.campaignHits.length +
          result.offerHits.length +
          result.qrCodeHits.length
        const latencyMs = Math.max(0, Date.now() - searchStartedAtMs)
        trackAnalytics({
          name: "global_search_results_returned",
          props: {
            queryLength: q.length,
            resultCount,
            scope,
            latencyMs,
            failedGroupCount: failedTypes.length,
            status: nextStatus,
          },
        })
        if (resultCount === 0) {
          trackAnalytics({
            name: "global_search_no_results",
            props: {
              queryLength: q.length,
              scope,
              latencyMs,
            },
          })
        }
      })
      .catch((error: unknown) => {
        if (generation !== searchGeneration) {
          return
        }
        if (isAbortError(error)) {
          // Keep prior hits; do not treat cancellation as a hard error.
          state = {
            ...state,
            hitsPending: false,
            searchStatus:
              state.searchStatus === "loading"
                ? hasAnyHits(state)
                  ? state.failedGroupTypes.length > 0
                    ? "partial"
                    : "ready"
                  : "idle"
                : state.searchStatus,
          }
          publish()
          return
        }
        state = {
          ...state,
          hitsPending: false,
          guestHits: [],
          feedbackHits: [],
          campaignHits: [],
          offerHits: [],
          qrCodeHits: [],
          searchSettled: true,
          searchStatus: "error",
          failedGroupTypes: [],
        }
        publish()
        trackAnalytics({
          name: "global_search_error",
          props: {
            reason: "request_failed",
            queryLength: q.length,
            scope,
          },
        })
      })
  }

  const scheduleSearch = () => {
    clearPendingSearch()
    const trimmed = state.query.trim()
    const locationId = adapters.getLocationId()

    if (!state.open || trimmed.length < MIN_QUERY_LENGTH || locationId == null) {
      if (
        state.hitsPending ||
        hasAnyHits(state) ||
        state.searchSettled ||
        state.searchStatus === "offline" ||
        state.searchStatus === "error"
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
      runSearch(latestTrimmed, latestLocationId, state.locationScope)
    }, debounceMs)
  }

  const close = () => {
    if (
      !state.open &&
      state.query === "" &&
      !state.hitsPending &&
      !hasAnyHits(state) &&
      state.locationScope === "current" &&
      !state.searchSettled &&
      state.searchStatus === "idle"
    ) {
      return
    }
    const wasOpen = state.open
    clearPendingSearch()
    searchGeneration += 1
    state = initialSearchState()
    queryStartedForOpen = false
    publish()
    if (wasOpen) {
      trackAnalytics({
        name: "global_search_closed",
        props: {},
      })
    }
  }

  const open = () => {
    if (state.open) {
      return
    }
    state = { ...state, open: true }
    queryStartedForOpen = false
    publish()
    trackAnalytics({
      name: "global_search_opened",
      props: {},
    })
  }

  const selectEntityHit = (
    hitId: string,
    hits: readonly OperatorGlobalSearchEntityHit[],
    entityCategory: OperatorGlobalSearchListEntity,
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
    const queryLength = state.query.trim().length
    const scope = state.locationScope
    trackAnalytics({
      name: "global_search_result_clicked",
      props: {
        entityCategory,
        queryLength,
        scope,
      },
    })
    close()
    try {
      navigate(parsedId, hit.locationId)
    } catch {
      // Search must stay usable even if navigation fails.
    }
  }

  const viewAllEntity = (entity: OperatorGlobalSearchListEntity) => {
    const trimmed = state.query.trim()
    const locationId = adapters.getLocationId()
    if (trimmed.length < MIN_QUERY_LENGTH || locationId == null) {
      return
    }
    const scope = state.locationScope
    const queryLength = trimmed.length
    trackAnalytics({
      name: "global_search_view_all",
      props: {
        entity,
        scope,
        queryLength,
      },
    })
    close()
    try {
      adapters.navigateToEntityList({
        entity,
        q: trimmed,
        locationId,
        scope,
      })
    } catch {
      // Search must stay usable even if navigation fails.
    }
  }

  const setLocationScope = (scope: OperatorGlobalSearchLocationScope) => {
    if (state.locationScope === scope) {
      return
    }
    state = {
      ...state,
      locationScope: scope,
      searchSettled: false,
    }
    publish()
    scheduleSearch()
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
      const previousTrimmed = state.query.trim()
      state = { ...state, query, searchSettled: false }
      publish()
      const trimmed = query.trim()
      if (
        state.open &&
        !queryStartedForOpen &&
        previousTrimmed.length < MIN_QUERY_LENGTH &&
        trimmed.length >= MIN_QUERY_LENGTH
      ) {
        queryStartedForOpen = true
        trackAnalytics({
          name: "global_search_query_started",
          props: {
            queryLength: trimmed.length,
            scope: state.locationScope,
          },
        })
      }
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
      const trimmed = state.query.trim()
      const typed = buildTypedAskTummlySuggestions(trimmed)
      const suggestion =
        EMPTY_AI_SUGGESTIONS.find((row) => row.id === suggestionId) ??
        typed.find((row) => row.id === suggestionId)
      if (suggestion == null) {
        return
      }
      const queryLength = trimmed.length
      trackAnalytics({
        name: "global_search_ai_suggestion_clicked",
        props: {
          suggestionId,
          queryLength,
        },
      })
      close()
      try {
        adapters.handoffSuggestionToAssistant(suggestion.prompt)
      } catch {
        // Search must stay usable even if Assistant handoff fails.
      }
    },
    selectGuestHit: (guestId) => {
      selectEntityHit(
        guestId,
        state.guestHits,
        "guests",
        adapters.navigateToGuestProfile
      )
    },
    selectFeedbackHit: (feedbackId) => {
      selectEntityHit(
        feedbackId,
        state.feedbackHits,
        "feedback",
        adapters.navigateToFeedbackDetail
      )
    },
    selectCampaignHit: (campaignId) => {
      selectEntityHit(
        campaignId,
        state.campaignHits,
        "campaigns",
        adapters.navigateToCampaignDetail
      )
    },
    selectOfferHit: (offerId) => {
      selectEntityHit(
        offerId,
        state.offerHits,
        "offers",
        adapters.navigateToOfferDetails
      )
    },
    selectQrCodeHit: (qrCodeId) => {
      selectEntityHit(
        qrCodeId,
        state.qrCodeHits,
        "qr-codes",
        adapters.navigateToCapturePlacementDetail
      )
    },
    viewAllGuests: () => {
      viewAllEntity("guests")
    },
    viewAllFeedback: () => {
      viewAllEntity("feedback")
    },
    viewAllCampaigns: () => {
      viewAllEntity("campaigns")
    },
    viewAllOffers: () => {
      viewAllEntity("offers")
    },
    viewAllQrCodes: () => {
      viewAllEntity("qr-codes")
    },
    retrySearch: () => {
      scheduleSearch()
    },
    setLocationScope,
    widenToAllLocations: () => {
      setLocationScope("all")
    },
    notifyOwnedLocationChanged: () => {
      if (!state.open) {
        return
      }
      state = {
        ...state,
        locationScope: "current",
        searchSettled: false,
        guestHits: [],
        feedbackHits: [],
        campaignHits: [],
        offerHits: [],
        qrCodeHits: [],
        searchStatus: "idle",
        failedGroupTypes: [],
      }
      publish()
      scheduleSearch()
    },
  }
}

import {
  chipCount,
  emptySelection,
  openSession,
  projectChips,
  removeAppliedChip,
  type FilterChip,
  type FilterSheetSession,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import { buildFeedbackSummarySection } from "@/lib/operatorFeedback/buildFeedbackSummarySection"
import {
  createFeedbackDetailsModule,
  type FeedbackDetailsAdapters,
  type FeedbackDetailsModule,
  type FeedbackDetailsSnapshot,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import { feedbackInboxFilterSheetSchema } from "@/lib/operatorFeedback/feedbackInboxFilterSheetSchema"
import { buildFeedbackInboxListQueryParams } from "@/lib/operatorFeedback/feedbackInboxListQueryParams"
import {
  buildFeedbackExportQueryParams,
  type FeedbackExportFormat,
  type FeedbackExportQueryParams,
  type FeedbackExportScope,
} from "@/lib/operatorFeedback/feedbackExportQueryParams"
import { mapFeedbackInboxApiResponseToViewModel } from "@/lib/operatorFeedback/mapFeedbackInboxApiResponseToViewModel"
import {
  FEEDBACK_PAGE_COPY,
  OPERATOR_FEEDBACK_INBOX_SORT_LABELS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import {
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  ClassificationTerminalSignal,
  FeedbackHomeRealtimeHandlers,
  FeedbackHomeRealtimeSession,
} from "@/lib/operatorHome/createOperatorHomePageModule"
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type {
  FeedbackInboxListResponse,
  FeedbackSummaryResponse,
  FeedbackWorkflowStatus,
} from "@/types/dashboard"
import type {
  OperatorFeedbackInboxSortId,
  OperatorFeedbackInboxTabId,
  OperatorFeedbackPageViewModel,
} from "@/types/operatorFeedback"



export type OperatorFeedbackWorkspaceLocation = {
  id: number
  locationName: string
}



export type OperatorFeedbackWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorFeedbackWorkspaceLocation[]
}



export type OperatorFeedbackPageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorFeedbackPageViewModel | null
  /** Inbox tab target; Review needs attention switches to needs-attention. */
  activeInboxTabId: OperatorFeedbackInboxTabId
  /** Bumps when empty-state Change period should open the header date control. */
  openDateRangeRequestId: number
  /** Bumps when Review needs attention should scroll to the inbox shell. */
  scrollToInboxRequestId: number
  filtersSession: FilterSheetSession | null
  feedbackDetails: FeedbackDetailsSnapshot
  canGoPreviousFeedback: boolean
  canGoNextFeedback: boolean
  exportDialog: OperatorFeedbackExportDialogSnapshot | null
}

export type OperatorFeedbackExportDialogSnapshot = {
  scope: FeedbackExportScope
  format: FeedbackExportFormat
  includeGuestContact: boolean
  currentResultsCount: number
  allInPeriodCount: number
  selectedCount: number
  canDownload: boolean
  locationName: string
  periodLabel: string
  isPreparing: boolean
  errorMessage: string | null
}



export type OperatorFeedbackPageAdapters = FeedbackDetailsAdapters & {
  getFeedbackSummary: (params: {
    locationId: number
    from: string
    to: string
  }) => Promise<FeedbackSummaryResponse>
  getFeedbackInbox: (
    params: ReturnType<typeof buildFeedbackInboxListQueryParams>
  ) => Promise<FeedbackInboxListResponse>
  exportFeedback: (
    params: FeedbackExportQueryParams
  ) => Promise<{ blob: Blob; filename: string }>
  triggerBrowserDownload: (blob: Blob, filename: string) => void
  getFeedbackPageDateRange: () => HomePerformanceDateRange
  connectRealtime?: (
    handlers: FeedbackHomeRealtimeHandlers
  ) => Promise<FeedbackHomeRealtimeSession>
  getNow?: () => Date
  scheduleReady?: () => Promise<void>
  debounceMs?: number
}



export type OperatorFeedbackPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => OperatorFeedbackPageSnapshot
  connect: () => Promise<void>
  disconnect: () => Promise<void>
  syncWorkspace: (input: OperatorFeedbackWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  reloadForFeedbackPageDateRange: () => Promise<void>
  reviewNeedsAttention: () => void
  requestOpenDateRange: () => void
  setActiveInboxTabId: (id: OperatorFeedbackInboxTabId) => void
  setSearchQuery: (query: string) => void
  setSortId: (id: OperatorFeedbackInboxSortId) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  openFilters: () => void
  closeFilters: () => void
  setFiltersSession: (session: FilterSheetSession) => void
  applyFilters: (filters: OperatorFilterSelection) => void
  removeFilterChip: (chip: FilterChip) => void
  clearSearchAndFilters: () => void
  openExportDialog: () => void
  closeExportDialog: () => void
  setExportScope: (scope: FeedbackExportScope) => void
  setExportFormat: (format: FeedbackExportFormat) => void
  setExportIncludeGuestContact: (include: boolean) => void
  downloadExport: () => Promise<void>
  openFeedbackDetails: (feedbackId: number) => Promise<void>
  closeFeedbackDetails: () => void
  openPreviousFeedback: () => Promise<void>
  openNextFeedback: () => Promise<void>
  setRowWorkflowStatus: (
    feedbackId: number,
    status: FeedbackWorkflowStatus
  ) => Promise<void>
  reopenFeedback: (feedbackId: number) => Promise<void>
  markFeedbackNoActionNeeded: (feedbackId: number) => Promise<void>
  retryFeedbackDetails: () => Promise<void>
  startClassificationCorrection: FeedbackDetailsModule["startCorrection"]
  setClassificationDraftSentiment: FeedbackDetailsModule["setDraftSentiment"]
  cancelClassificationCorrection: FeedbackDetailsModule["cancelCorrection"]
  saveClassificationCorrection: FeedbackDetailsModule["saveCorrection"]
  setFeedbackWorkflowStatus: FeedbackDetailsModule["setWorkflowStatus"]
  reopenFeedbackDetails: FeedbackDetailsModule["reopen"]
  markFeedbackDetailsNoActionNeeded: FeedbackDetailsModule["markNoActionNeeded"]
  setFeedbackInternalNoteDraft: FeedbackDetailsModule["setNoteDraft"]
  createFeedbackInternalNote: FeedbackDetailsModule["createNote"]
  startFeedbackNoteEdit: FeedbackDetailsModule["startEditNote"]
  setFeedbackNoteEditDraft: FeedbackDetailsModule["setNoteEditDraft"]
  cancelFeedbackNoteEdit: FeedbackDetailsModule["cancelEditNote"]
  saveFeedbackNoteEdit: FeedbackDetailsModule["saveEditNote"]
  startFeedbackNoteDelete: FeedbackDetailsModule["startDeleteNote"]
  cancelFeedbackNoteDelete: FeedbackDetailsModule["cancelDeleteNote"]
  confirmFeedbackNoteDelete: FeedbackDetailsModule["confirmDeleteNote"]
}



type ModuleState = {
  loadStatus: OperatorFeedbackPageSnapshot["loadStatus"]
  viewModel: OperatorFeedbackPageViewModel | null
  workspace: OperatorFeedbackWorkspaceInput | null
  activeInboxTabId: OperatorFeedbackInboxTabId
  openDateRangeRequestId: number
  scrollToInboxRequestId: number
  lastLoadedAtIso: string | null
  pageLoadGeneration: number
  inboxLoadGeneration: number
  searchQuery: string
  sortId: OperatorFeedbackInboxSortId
  page: number
  appliedFilters: OperatorFilterSelection
  filterChips: FilterChip[]
  filterChipCount: number
  filtersSession: FilterSheetSession | null
  inboxListContextFeedbackIds: number[]
  canGoPreviousFeedback: boolean
  canGoNextFeedback: boolean
  exportDialogOpen: boolean
  exportScope: FeedbackExportScope
  exportFormat: FeedbackExportFormat
  exportIncludeGuestContact: boolean
  exportPreparing: boolean
  exportErrorMessage: string | null
}



const FEEDBACK_LOAD_ERROR_MESSAGE =
  "Could not load Feedback. Please try again."



const DEFAULT_SORT_ID: OperatorFeedbackInboxSortId = "newest-submitted"
const DEFAULT_SEARCH_DEBOUNCE_MS = 300



const INBOX_FILTER_SCHEMA = feedbackInboxFilterSheetSchema()



function resolveLocationName(
  input: OperatorFeedbackWorkspaceInput,
  locationId: number
): string {
  return (
    input.locations.find((location) => location.id === locationId)
      ?.locationName ?? ""
  )
}



function hasActiveInboxQuery(state: ModuleState): boolean {
  return state.searchQuery.trim().length > 0 || state.filterChipCount > 0
}

function buildExportDialogSnapshot(
  state: ModuleState
): OperatorFeedbackExportDialogSnapshot | null {
  if (!state.exportDialogOpen || state.viewModel == null) {
    return null
  }

  const currentResultsCount = state.viewModel.inbox.filteredTotalCount
  const allInPeriodCount =
    state.viewModel.inbox.tabs.find((tab) => tab.id === "all")?.count ?? 0
  const selectedCount =
    state.exportScope === "current" ? currentResultsCount : allInPeriodCount

  return {
    scope: state.exportScope,
    format: state.exportFormat,
    includeGuestContact: state.exportIncludeGuestContact,
    currentResultsCount,
    allInPeriodCount,
    selectedCount,
    canDownload: selectedCount > 0 && !state.exportPreparing,
    locationName: state.viewModel.locationName,
    periodLabel: state.viewModel.dateRangeLabel,
    isPreparing: state.exportPreparing,
    errorMessage: state.exportErrorMessage,
  }
}



function syncFilterProjection(state: ModuleState): Pick<
  ModuleState,
  "filterChips" | "filterChipCount"
> {
  const filterChips = projectChips(INBOX_FILTER_SCHEMA, state.appliedFilters)
  return {
    filterChips,
    filterChipCount: chipCount(INBOX_FILTER_SCHEMA, state.appliedFilters),
  }
}



/**
 * Operator Feedback page module — adapters in, snapshot out.
 * Owns summary + inbox for the selected Owned location.
 */
export function createOperatorFeedbackPageModule(
  adapters: OperatorFeedbackPageAdapters
): OperatorFeedbackPageModule {
  const scheduleReady = adapters.scheduleReady ?? (() => Promise.resolve())
  const getNow = adapters.getNow ?? (() => new Date())
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS



  const feedbackDetails = createFeedbackDetailsModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    correctClassification: adapters.correctClassification,
    setWorkflowStatus: adapters.setWorkflowStatus,
    createInternalNote: adapters.createInternalNote,
    updateInternalNote: adapters.updateInternalNote,
    deleteInternalNote: adapters.deleteInternalNote,
  })



  let state: ModuleState = {
    loadStatus: "idle",
    viewModel: null,
    workspace: null,
    activeInboxTabId: "all",
    openDateRangeRequestId: 0,
    scrollToInboxRequestId: 0,
    lastLoadedAtIso: null,
    pageLoadGeneration: 0,
    inboxLoadGeneration: 0,
    searchQuery: "",
    sortId: DEFAULT_SORT_ID,
    page: 1,
    appliedFilters: emptySelection(INBOX_FILTER_SCHEMA),
    filterChips: [],
    filterChipCount: 0,
    filtersSession: null,
    inboxListContextFeedbackIds: [],
    canGoPreviousFeedback: false,
    canGoNextFeedback: false,
    exportDialogOpen: false,
    exportScope: "current",
    exportFormat: "xlsx",
    exportIncludeGuestContact: false,
    exportPreparing: false,
    exportErrorMessage: null,
  }



  let snapshot: OperatorFeedbackPageSnapshot = {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    activeInboxTabId: state.activeInboxTabId,
    openDateRangeRequestId: state.openDateRangeRequestId,
    scrollToInboxRequestId: state.scrollToInboxRequestId,
    filtersSession: state.filtersSession,
    feedbackDetails: feedbackDetails.getSnapshot(),
    canGoPreviousFeedback: state.canGoPreviousFeedback,
    canGoNextFeedback: state.canGoNextFeedback,
    exportDialog: buildExportDialogSnapshot(state),
  }
  const listeners = new Set<() => void>()
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null



  const publish = () => {
    snapshot = {
      loadStatus: state.loadStatus,
      viewModel: state.viewModel,
      activeInboxTabId: state.activeInboxTabId,
      openDateRangeRequestId: state.openDateRangeRequestId,
      scrollToInboxRequestId: state.scrollToInboxRequestId,
      filtersSession: state.filtersSession,
      feedbackDetails: feedbackDetails.getSnapshot(),
      canGoPreviousFeedback: state.canGoPreviousFeedback,
      canGoNextFeedback: state.canGoNextFeedback,
      exportDialog: buildExportDialogSnapshot(state),
    }
    for (const listener of listeners) {
      listener()
    }
  }



  const clearSearchDebounce = () => {
    if (searchDebounceTimer != null) {
      clearTimeout(searchDebounceTimer)
      searchDebounceTimer = null
    }
  }



  const buildInboxQueryParams = (locationId: number) =>
    buildFeedbackInboxListQueryParams({
      locationId,
      headerDateRange: adapters.getFeedbackPageDateRange(),
      tab: state.activeInboxTabId,
      q: state.searchQuery,
      sort: state.sortId,
      page: state.page,
      filters: state.appliedFilters,
      now: getNow(),
    })



  const buildSummaryParams = (locationId: number) => {
    const performanceWindow = resolveHomePerformanceWindow(
      adapters.getFeedbackPageDateRange(),
      getNow()
    )
    return {
      locationId,
      from: performanceWindow.from.toISOString(),
      to: performanceWindow.to.toISOString(),
    }
  }



  const mapInboxIntoViewModel = (
    viewModel: OperatorFeedbackPageViewModel,
    inboxResponse: FeedbackInboxListResponse
  ): OperatorFeedbackPageViewModel => ({
    ...viewModel,
    inbox: mapFeedbackInboxApiResponseToViewModel({
      response: inboxResponse,
      sortId: state.sortId,
      searchQuery: state.searchQuery,
      filterChips: state.filterChips,
      filterChipCount: state.filterChipCount,
      hasActiveQuery: hasActiveInboxQuery(state),
    }),
  })



  const applyInboxListContext = (inboxResponse: FeedbackInboxListResponse) => {
    state = {
      ...state,
      inboxListContextFeedbackIds: inboxResponse.items.map((item) => item.id),
    }
    refreshListNavigation(feedbackDetails.getSnapshot().feedbackId)
  }



  const syncInboxPresentation = () => {
    if (state.viewModel == null) {
      return
    }
    state = {
      ...state,
      viewModel: {
        ...state.viewModel,
        inbox: {
          ...state.viewModel.inbox,
          searchQuery: state.searchQuery,
          sortId: state.sortId,
          sortLabel: OPERATOR_FEEDBACK_INBOX_SORT_LABELS[state.sortId],
          filterChips: state.filterChips,
          filterChipCount: state.filterChipCount,
        },
      },
    }
  }



  const buildViewModelShell = (
    input: OperatorFeedbackWorkspaceInput,
    locationId: number,
    summary: FeedbackSummaryResponse,
    loadedAtIso: string,
    inboxResponse: FeedbackInboxListResponse
  ): OperatorFeedbackPageViewModel => {
    const dateRange = adapters.getFeedbackPageDateRange()
    const now = getNow()
    const shell: OperatorFeedbackPageViewModel = {
      locationId,
      locationName: resolveLocationName(input, locationId),
      dateRangeLabel: labelForHomePerformanceDateRange(dateRange),
      updatedRelativeLabel: formatRelativeTime(loadedAtIso, now.getTime()),
      needsAttentionCount: summary.needsAttentionTotal,
      summary: buildFeedbackSummarySection(summary),
      inbox: mapFeedbackInboxApiResponseToViewModel({
        response: inboxResponse,
        sortId: state.sortId,
        searchQuery: state.searchQuery,
        filterChips: state.filterChips,
        filterChipCount: state.filterChipCount,
        hasActiveQuery: hasActiveInboxQuery(state),
      }),
    }
    return shell
  }



  feedbackDetails.subscribe(() => {
    publish()
  })



  const refreshListNavigation = (feedbackId: number | null) => {
    if (feedbackId == null) {
      state = {
        ...state,
        canGoPreviousFeedback: false,
        canGoNextFeedback: false,
      }
      return
    }
    const index = state.inboxListContextFeedbackIds.indexOf(feedbackId)
    state = {
      ...state,
      canGoPreviousFeedback: index > 0,
      canGoNextFeedback:
        index >= 0 && index < state.inboxListContextFeedbackIds.length - 1,
    }
  }



  const refreshOpenFeedbackDetails = () => {
    const details = feedbackDetails.getSnapshot()
    if (
      details.isOpen
      && details.feedbackId != null
      && !details.correction.isEditing
    ) {
      void feedbackDetails.retry()
    }
  }



  const fetchPageData = async (options: {
    workspace: OperatorFeedbackWorkspaceInput
    locationId: number
    isInitialLoad: boolean
    quiet?: boolean
  }): Promise<void> => {
    const generation = ++state.pageLoadGeneration
    const isQuiet = options.quiet === true && state.viewModel != null



    state = {
      ...state,
      loadStatus: isQuiet ? state.loadStatus : "loading",
      ...(options.isInitialLoad ? { viewModel: null } : {}),
    }
    publish()



    await scheduleReady()



    const summaryParams = buildSummaryParams(options.locationId)
    const inboxParams = buildInboxQueryParams(options.locationId)



    const [summarySettled, inboxSettled] = await Promise.all([
      adapters
        .getFeedbackSummary(summaryParams)
        .then((response) => ({ ok: true as const, response }))
        .catch(() => ({ ok: false as const })),
      adapters
        .getFeedbackInbox(inboxParams)
        .then((response) => ({ ok: true as const, response }))
        .catch(() => ({ ok: false as const })),
    ])



    if (generation !== state.pageLoadGeneration) {
      return
    }



    if (!summarySettled.ok || !inboxSettled.ok) {
      state = {
        ...state,
        loadStatus: "error",
        ...(options.isInitialLoad ? { viewModel: null } : {}),
        workspace: options.workspace,
      }
      publish()
      return
    }



    const loadedAtIso = getNow().toISOString()
    state = {
      ...state,
      loadStatus: "loaded",
      viewModel: buildViewModelShell(
        options.workspace,
        options.locationId,
        summarySettled.response,
        loadedAtIso,
        inboxSettled.response
      ),
      lastLoadedAtIso: loadedAtIso,
      workspace: options.workspace,
    }
    applyInboxListContext(inboxSettled.response)
    publish()
  }



  const fetchInbox = async (options?: { quiet?: boolean }) => {
    const locationId = state.workspace?.selectedLocationId
    if (locationId == null) {
      return
    }



    const generation = ++state.inboxLoadGeneration
    const isQuiet = options?.quiet === true && state.viewModel != null



    if (!isQuiet) {
      state = {
        ...state,
        loadStatus: "loading",
      }
      publish()
    }



    const inboxParams = buildInboxQueryParams(locationId)



    const settled = await adapters
      .getFeedbackInbox(inboxParams)
      .then((response) => ({ ok: true as const, response }))
      .catch(() => ({ ok: false as const }))



    if (generation !== state.inboxLoadGeneration) {
      return
    }



    if (!settled.ok) {
      if (!isQuiet) {
        state = {
          ...state,
          loadStatus: "error",
        }
        publish()
      }
      return
    }



    if (state.viewModel == null) {
      return
    }



    state = {
      ...state,
      loadStatus: "loaded",
      viewModel: mapInboxIntoViewModel(state.viewModel, settled.response),
    }
    applyInboxListContext(settled.response)
    publish()
  }



  const scheduleInboxFetch = () => {
    clearSearchDebounce()
    searchDebounceTimer = setTimeout(() => {
      searchDebounceTimer = null
      void fetchInbox({ quiet: true })
    }, debounceMs)
  }



  const refreshSummaryAndInbox = async () => {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    if (workspace == null || locationId == null) {
      return
    }
    await fetchPageData({
      workspace,
      locationId,
      isInitialLoad: false,
      quiet: true,
    })
  }



  const afterListAffectingMutation = async (
    action: () => Promise<boolean | void>
  ): Promise<boolean> => {
    const result = await action()
    if (result !== false) {
      await refreshSummaryAndInbox()
    }
    return result !== false
  }



  const loadForWorkspace = async (
    input: OperatorFeedbackWorkspaceInput
  ): Promise<void> => {
    const generation = ++state.pageLoadGeneration
    clearSearchDebounce()
    state = {
      ...state,
      loadStatus: "loading",
      viewModel: null,
      workspace: input,
      activeInboxTabId: "all",
      searchQuery: "",
      sortId: DEFAULT_SORT_ID,
      page: 1,
      appliedFilters: emptySelection(INBOX_FILTER_SCHEMA),
      ...syncFilterProjection({
        ...state,
        appliedFilters: emptySelection(INBOX_FILTER_SCHEMA),
      }),
      filtersSession: null,
      inboxListContextFeedbackIds: [],
      canGoPreviousFeedback: false,
      canGoNextFeedback: false,
      exportDialogOpen: false,
      exportScope: "current",
      exportFormat: "xlsx",
      exportIncludeGuestContact: false,
      exportPreparing: false,
      exportErrorMessage: null,
    }
    publish()



    if (input.selectedLocationId == null) {
      if (generation !== state.pageLoadGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "loaded",
        viewModel: null,
        workspace: input,
      }
      publish()
      return
    }



    if (generation !== state.pageLoadGeneration) {
      return
    }



    await fetchPageData({
      workspace: input,
      locationId: input.selectedLocationId,
      isInitialLoad: true,
    })
  }



  const handleClassificationTerminal = (
    signal: ClassificationTerminalSignal
  ) => {
    const selectedLocationId = state.workspace?.selectedLocationId
    if (
      selectedLocationId == null
      || signal.locationId !== selectedLocationId
    ) {
      return
    }



    void refreshSummaryAndInbox()



    const details = feedbackDetails.getSnapshot()
    if (
      details.isOpen
      && details.feedbackId === signal.feedbackId
      && !details.correction.isEditing
    ) {
      void feedbackDetails.retry()
    }
  }



  let realtimeSession: FeedbackHomeRealtimeSession | null = null
  let connectingRealtime = false



  const ensureRealtime = async () => {
    if (
      adapters.connectRealtime == null
      || realtimeSession != null
      || connectingRealtime
    ) {
      return
    }



    connectingRealtime = true
    try {
      realtimeSession = await adapters.connectRealtime({
        onClassificationTerminal: handleClassificationTerminal,
        onReconnected: () => {
          void refreshSummaryAndInbox()
          refreshOpenFeedbackDetails()
        },
      })
    } finally {
      connectingRealtime = false
    }
  }



  const disconnect = async () => {
    const session = realtimeSession
    realtimeSession = null
    if (session != null) {
      await session.stop()
    }
  }



  return {
    getSnapshot() {
      return snapshot
    },
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    connect: () => ensureRealtime(),
    disconnect,
    async syncWorkspace(input) {
      await loadForWorkspace(input)
    },
    async retryLoad() {
      if (state.workspace == null) {
        return
      }
      await loadForWorkspace(state.workspace)
    },
    async reloadForFeedbackPageDateRange() {
      const workspace = state.workspace
      const locationId = workspace?.selectedLocationId
      if (workspace == null || locationId == null) {
        return
      }
      state = {
        ...state,
        page: 1,
      }
      await fetchPageData({
        workspace,
        locationId,
        isInitialLoad: false,
      })
    },
    reviewNeedsAttention() {
      state = {
        ...state,
        activeInboxTabId: "needs-attention",
        scrollToInboxRequestId: state.scrollToInboxRequestId + 1,
        page: 1,
      }
      publish()
      void fetchInbox({ quiet: true })
    },
    requestOpenDateRange() {
      state = {
        ...state,
        openDateRangeRequestId: state.openDateRangeRequestId + 1,
      }
      publish()
    },
    setActiveInboxTabId(id) {
      state = {
        ...state,
        activeInboxTabId: id,
        page: 1,
      }
      syncInboxPresentation()
      publish()
      void fetchInbox({ quiet: true })
    },
    setSearchQuery(query) {
      state = {
        ...state,
        searchQuery: query,
        page: 1,
      }
      syncInboxPresentation()
      publish()
      scheduleInboxFetch()
    },
    setSortId(id) {
      state = {
        ...state,
        sortId: id,
        page: 1,
      }
      syncInboxPresentation()
      publish()
      void fetchInbox({ quiet: true })
    },
    goToPreviousPage() {
      if (state.page <= 1) {
        return
      }
      state = {
        ...state,
        page: state.page - 1,
      }
      publish()
      void fetchInbox({ quiet: true })
    },
    goToNextPage() {
      state = {
        ...state,
        page: state.page + 1,
      }
      publish()
      void fetchInbox({ quiet: true })
    },
    openFilters() {
      state = {
        ...state,
        filtersSession: openSession(state.appliedFilters),
      }
      publish()
    },
    closeFilters() {
      if (state.filtersSession == null) {
        return
      }
      state = {
        ...state,
        filtersSession: null,
      }
      publish()
    },
    setFiltersSession(session) {
      state = {
        ...state,
        filtersSession: session,
      }
      publish()
    },
    applyFilters(filters) {
      state = {
        ...state,
        appliedFilters: filters,
        filtersSession:
          state.filtersSession != null ? openSession(filters) : null,
        page: 1,
        ...syncFilterProjection({ ...state, appliedFilters: filters }),
      }
      syncInboxPresentation()
      publish()
      void fetchInbox({ quiet: true })
    },
    removeFilterChip(chip) {
      const appliedFilters = removeAppliedChip(
        INBOX_FILTER_SCHEMA,
        state.appliedFilters,
        chip
      )
      state = {
        ...state,
        appliedFilters,
        page: 1,
        ...syncFilterProjection({ ...state, appliedFilters }),
      }
      syncInboxPresentation()
      publish()
      void fetchInbox({ quiet: true })
    },
    clearSearchAndFilters() {
      const appliedFilters = emptySelection(INBOX_FILTER_SCHEMA)
      state = {
        ...state,
        searchQuery: "",
        appliedFilters,
        page: 1,
        ...syncFilterProjection({ ...state, appliedFilters }),
      }
      syncInboxPresentation()
      publish()
      void fetchInbox({ quiet: true })
    },
    openExportDialog() {
      if (state.viewModel == null) {
        return
      }
      state = {
        ...state,
        exportDialogOpen: true,
        exportScope: "current",
        exportFormat: "xlsx",
        exportIncludeGuestContact: false,
        exportPreparing: false,
        exportErrorMessage: null,
      }
      publish()
    },
    closeExportDialog() {
      if (!state.exportDialogOpen) {
        return
      }
      state = {
        ...state,
        exportDialogOpen: false,
        exportPreparing: false,
        exportErrorMessage: null,
      }
      publish()
    },
    setExportScope(scope) {
      if (!state.exportDialogOpen || state.exportPreparing) {
        return
      }
      state = {
        ...state,
        exportScope: scope,
        exportErrorMessage: null,
      }
      publish()
    },
    setExportFormat(format) {
      if (!state.exportDialogOpen || state.exportPreparing) {
        return
      }
      state = {
        ...state,
        exportFormat: format,
        exportErrorMessage: null,
      }
      publish()
    },
    setExportIncludeGuestContact(include) {
      if (!state.exportDialogOpen || state.exportPreparing) {
        return
      }
      state = {
        ...state,
        exportIncludeGuestContact: include,
        exportErrorMessage: null,
      }
      publish()
    },
    async downloadExport() {
      const dialog = buildExportDialogSnapshot(state)
      const locationId = state.workspace?.selectedLocationId
      if (
        dialog == null
        || !dialog.canDownload
        || locationId == null
      ) {
        return
      }

      state = {
        ...state,
        exportPreparing: true,
        exportErrorMessage: null,
      }
      publish()

      try {
        const inboxParams = buildFeedbackInboxListQueryParams({
          locationId,
          headerDateRange: adapters.getFeedbackPageDateRange(),
          tab: state.activeInboxTabId,
          q: state.searchQuery,
          sort: state.sortId,
          page: state.page,
          filters: state.appliedFilters,
          now: getNow(),
        })
        const params = buildFeedbackExportQueryParams({
          inboxParams,
          scope: state.exportScope,
          format: state.exportFormat,
          includeGuestContact: state.exportIncludeGuestContact,
        })
        const result = await adapters.exportFeedback(params)
        adapters.triggerBrowserDownload(result.blob, result.filename)
        state = {
          ...state,
          exportPreparing: false,
          exportDialogOpen: false,
          exportErrorMessage: null,
        }
        publish()
      } catch (error) {
        const message =
          error instanceof Error
          && error.message === FEEDBACK_PAGE_COPY.exportDialog.softMaxError
            ? error.message
            : error instanceof Error && error.message.length > 0
              ? error.message
              : FEEDBACK_PAGE_COPY.exportDialog.genericError
        state = {
          ...state,
          exportPreparing: false,
          exportErrorMessage: message,
        }
        publish()
      }
    },
    async openFeedbackDetails(feedbackId) {
      await feedbackDetails.open(feedbackId)
      refreshListNavigation(feedbackId)
      publish()
    },
    closeFeedbackDetails() {
      feedbackDetails.close()
      refreshListNavigation(null)
      publish()
    },
    async openPreviousFeedback() {
      const currentId = feedbackDetails.getSnapshot().feedbackId
      if (currentId == null) {
        return
      }
      const index = state.inboxListContextFeedbackIds.indexOf(currentId)
      const previousId = state.inboxListContextFeedbackIds[index - 1]
      if (previousId == null) {
        return
      }
      await feedbackDetails.open(previousId)
      refreshListNavigation(previousId)
      publish()
    },
    async openNextFeedback() {
      const currentId = feedbackDetails.getSnapshot().feedbackId
      if (currentId == null) {
        return
      }
      const index = state.inboxListContextFeedbackIds.indexOf(currentId)
      const nextId = state.inboxListContextFeedbackIds[index + 1]
      if (nextId == null) {
        return
      }
      await feedbackDetails.open(nextId)
      refreshListNavigation(nextId)
      publish()
    },
    async setRowWorkflowStatus(feedbackId, status) {
      await afterListAffectingMutation(async () => {
        await adapters.setWorkflowStatus(feedbackId, status)
        return true
      })
      const details = feedbackDetails.getSnapshot()
      if (
        details.isOpen
        && details.feedbackId === feedbackId
        && !details.correction.isEditing
      ) {
        await feedbackDetails.retry()
      }
    },
    async reopenFeedback(feedbackId) {
      await afterListAffectingMutation(async () => {
        await adapters.setWorkflowStatus(feedbackId, "in_progress")
        return true
      })
      const details = feedbackDetails.getSnapshot()
      if (
        details.isOpen
        && details.feedbackId === feedbackId
        && !details.correction.isEditing
      ) {
        await feedbackDetails.retry()
      }
    },
    async markFeedbackNoActionNeeded(feedbackId) {
      await afterListAffectingMutation(async () => {
        await adapters.setWorkflowStatus(feedbackId, "resolved")
        return true
      })
      const details = feedbackDetails.getSnapshot()
      if (
        details.isOpen
        && details.feedbackId === feedbackId
        && !details.correction.isEditing
      ) {
        await feedbackDetails.retry()
      }
    },
    retryFeedbackDetails: () => feedbackDetails.retry(),
    startClassificationCorrection: () => feedbackDetails.startCorrection(),
    setClassificationDraftSentiment: (sentiment) =>
      feedbackDetails.setDraftSentiment(sentiment),
    cancelClassificationCorrection: () => feedbackDetails.cancelCorrection(),
    saveClassificationCorrection: async () => {
      await afterListAffectingMutation(() => feedbackDetails.saveCorrection())
    },
    setFeedbackWorkflowStatus: (status) =>
      afterListAffectingMutation(() => feedbackDetails.setWorkflowStatus(status)),
    reopenFeedbackDetails: () =>
      afterListAffectingMutation(() => feedbackDetails.reopen()),
    markFeedbackDetailsNoActionNeeded: () =>
      afterListAffectingMutation(() => feedbackDetails.markNoActionNeeded()),
    setFeedbackInternalNoteDraft: (value) => feedbackDetails.setNoteDraft(value),
    createFeedbackInternalNote: () =>
      afterListAffectingMutation(() => feedbackDetails.createNote()),
    startFeedbackNoteEdit: (noteId) => feedbackDetails.startEditNote(noteId),
    setFeedbackNoteEditDraft: (value) => feedbackDetails.setNoteEditDraft(value),
    cancelFeedbackNoteEdit: () => feedbackDetails.cancelEditNote(),
    saveFeedbackNoteEdit: () =>
      afterListAffectingMutation(() => feedbackDetails.saveEditNote()),
    startFeedbackNoteDelete: (noteId) => feedbackDetails.startDeleteNote(noteId),
    cancelFeedbackNoteDelete: () => feedbackDetails.cancelDeleteNote(),
    confirmFeedbackNoteDelete: () =>
      afterListAffectingMutation(() => feedbackDetails.confirmDeleteNote()),
  }
}



export { FEEDBACK_LOAD_ERROR_MESSAGE }



export type {
  ClassificationTerminalSignal,
  FeedbackHomeRealtimeHandlers,
  FeedbackHomeRealtimeSession,
}


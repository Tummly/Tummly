import {
  addDeltaFromPending,
  openAddTagSession,
  setAddTagCreateName as setSessionCreateName,
  setAddTagCreateOpen as setSessionCreateOpen,
  setAddTagSearchQuery,
  stageCreatedTag,
  stageTag,
  unstageTag,
  type AddTagDialogSession,
} from "@/lib/operatorGuests/addTagDialogLogic"
import {
  createGuestDetailsModule,
  type GuestDetailsAdapters,
  type GuestDetailsSnapshot,
} from "@/lib/operatorGuests/createGuestDetailsModule"
import {
  createFeedbackDetailsModule,
  type FeedbackDetailsAdapters,
  type FeedbackDetailsModule,
  type FeedbackDetailsSnapshot,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  createRecoveryWizardsModule,
  type RecoveryWizardsAdapters,
  type RecoveryWizardsModule,
  type RecoveryWizardsSnapshot,
} from "@/lib/operatorFeedback/createRecoveryWizardsModule"
import type { StartRecoveryIntentId } from "@/lib/operatorFeedback/startRecoveryPresentation"
import type { GuestTag } from "@/lib/operatorGuests/guestTag"
import type {
  FeedbackSentiment,
  FeedbackWorkflowStatus,
} from "@/types/dashboard"
import { mapGuestsApiResponseToViewModel } from "@/lib/operatorGuests/mapGuestsApiResponseToViewModel"
import { OPERATOR_GUEST_DEFAULT_SORT_ID } from "@/lib/operatorGuests/guestsPresentation"
import {
  chipCount,
  clearLocationOverrideOnShellChange,
  emptySelection,
  getLocationOverride,
  openSession,
  projectChips,
  removeAppliedChip,
  type FilterChip,
  type FilterSheetSession,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import { guestsFilterSheetSchema } from "@/lib/operatorGuests/guestsFilterSheetSchema"
import {
  buildGuestsExportQueryParams,
  buildGuestsListQueryParams,
  type GuestsExportQueryParams,
  type GuestsListQueryParams,
} from "@/lib/operatorGuests/guestsListQueryParams"
import type { GuestsOverviewDateRange } from "@/lib/operatorGuests/guestsOverviewDateRange"
import {
  computeVisibleSelectionState,
  formatGuestSelectionLabel,
  selectionIdsInCheckOrder,
  sortedSelectionIds,
  toggleAllVisibleInSelection,
  toggleGuestInSelection,
} from "@/lib/operatorGuests/guestSelection"
import type { GuestsResponse } from "@/types/dashboard"
import type {
  OperatorGuestSmartGroupId,
  OperatorGuestSortId,
  OperatorGuestsViewModel,
} from "@/types/operatorGuests"

const GUESTS_SCHEMA = guestsFilterSheetSchema()

export type OperatorGuestsWorkspaceLocation = {
  id: number
  locationName: string
}

export type OperatorGuestsWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorGuestsWorkspaceLocation[]
}

export type OperatorGuestsPageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorGuestsViewModel | null
  searchQuery: string
  sortId: OperatorGuestSortId
  selectedGuestIds: readonly string[]
  selectedCount: number
  bulkSelectionLabel: string | null
  isAllVisibleSelected: boolean
  isSomeVisibleSelected: boolean
  isGuestSelected: (guestId: string) => boolean
  appliedFilters: OperatorFilterSelection
  filterChips: FilterChip[]
  filterChipCount: number
  filtersSession: FilterSheetSession | null
  filterCatalog: readonly GuestTag[]
  filtersBusy: boolean
  addTagSession: AddTagDialogSession | null
  addTagBusy: boolean
  exportBusy: boolean
  actionError: string | null
  guestDetails: GuestDetailsSnapshot
  feedbackDetails: FeedbackDetailsSnapshot
} & RecoveryWizardsSnapshot

export type OperatorGuestsPageAdapters = {
  getGuests: (params: GuestsListQueryParams) => Promise<GuestsResponse>
  exportGuestsCsv: (
    params: GuestsExportQueryParams
  ) => Promise<{ blob: Blob; filename: string }>
  listGuestTags: (params: {
    locationId: number
    locationScope?: "all"
    locationIds?: number[]
  }) => Promise<GuestTag[]>
  createGuestTag: (params: {
    locationId: number
    name: string
  }) => Promise<GuestTag>
  applyGuestTags: (params: {
    locationId: number
    guestIds: number[]
    tagIds: number[]
  }) => Promise<void>
  getGuestTagMemberships: (params: {
    locationId: number
    guestIds: number[]
  }) => Promise<ReadonlyMap<string, readonly string[]>>
  getGuestProfile: GuestDetailsAdapters["getGuestProfile"]
  createGuestNote: GuestDetailsAdapters["createGuestNote"]
  getFeedbackDetails: FeedbackDetailsAdapters["getFeedbackDetails"]
  correctClassification: FeedbackDetailsAdapters["correctClassification"]
  setWorkflowStatus: FeedbackDetailsAdapters["setWorkflowStatus"]
  createInternalNote: FeedbackDetailsAdapters["createInternalNote"]
  updateInternalNote: FeedbackDetailsAdapters["updateInternalNote"]
  deleteInternalNote: FeedbackDetailsAdapters["deleteInternalNote"]
  closeOutFeedback: FeedbackDetailsAdapters["closeOutFeedback"]
  sendGuestResponse: RecoveryWizardsAdapters["sendGuestResponse"]
  completeRecovery: RecoveryWizardsAdapters["completeRecovery"]
  prepareRecoveryDraft: RecoveryWizardsAdapters["prepareRecoveryDraft"]
  recordInternalAction: RecoveryWizardsAdapters["recordInternalAction"]
  sendAndRecord: RecoveryWizardsAdapters["sendAndRecord"]
  sendAndIssueRecoveryOffer: RecoveryWizardsAdapters["sendAndIssueRecoveryOffer"]
  prepareRecoveryOfferDraft: RecoveryWizardsAdapters["prepareRecoveryOfferDraft"]
  getGuestsOverviewDateRange: () => GuestsOverviewDateRange
  triggerBrowserDownload: (blob: Blob, filename: string) => void
  getNow?: () => Date
  debounceMs?: number
}

export type OperatorGuestsPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => OperatorGuestsPageSnapshot
  syncWorkspace: (input: OperatorGuestsWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  setActiveSmartGroupId: (id: OperatorGuestSmartGroupId) => void
  setSearchQuery: (query: string) => void
  setSortId: (id: OperatorGuestSortId) => void
  setPage: (page: number) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  toggleGuestSelection: (guestId: string) => void
  toggleSelectAllVisibleRows: () => void
  clearSelection: () => void
  clearSearchAndFilters: () => void
  applyFilters: (filters: OperatorFilterSelection) => void
  removeFilterChip: (chip: FilterChip) => void
  openFilters: () => Promise<void>
  closeFilters: () => void
  setFiltersSession: (session: FilterSheetSession) => void
  reloadForOverviewDateRange: () => Promise<void>
  exportCsv: () => Promise<void>
  exportSelectedCsv: () => Promise<void>
  openAddTag: (guestIds?: readonly string[]) => Promise<void>
  closeAddTag: () => void
  stageAddTag: (tagId: string) => void
  unstageAddTag: (tagId: string) => void
  setAddTagSearch: (query: string) => void
  setAddTagCreateOpen: (open: boolean) => void
  setAddTagCreateName: (name: string) => void
  createAndStageAddTag: () => Promise<void>
  applyAddTag: () => Promise<void>
  openGuestDetails: (guestId: number) => Promise<void>
  closeGuestDetails: () => void
  retryGuestDetails: () => Promise<void>
  setGuestDetailsNoteDraft: (value: string) => void
  createGuestDetailsNote: () => Promise<boolean>
  openFeedbackDetails: (feedbackId: number) => Promise<void>
  closeFeedbackDetails: () => void
  retryFeedbackDetails: () => Promise<void>
  startClassificationCorrection: () => void
  setClassificationDraftSentiment: (sentiment: FeedbackSentiment) => void
  setClassificationDraftReason: FeedbackDetailsModule["setDraftReason"]
  setClassificationDraftNote: FeedbackDetailsModule["setDraftNote"]
  cancelClassificationCorrection: () => void
  saveClassificationCorrection: () => Promise<void>
  setFeedbackWorkflowStatus: (status: FeedbackWorkflowStatus) => Promise<boolean>
  reopenFeedback: () => Promise<boolean>
  startFeedbackMarkNoActionNeeded: () => boolean
  startFeedbackMarkResolved: () => boolean
  setFeedbackCloseOutReason: FeedbackDetailsModule["setCloseOutReason"]
  setFeedbackCloseOutNoteDraft: FeedbackDetailsModule["setCloseOutNoteDraft"]
  setFeedbackCloseOutAcknowledged: FeedbackDetailsModule["setCloseOutAcknowledged"]
  cancelFeedbackCloseOut: FeedbackDetailsModule["cancelCloseOut"]
  confirmFeedbackCloseOut: () => Promise<boolean>
  setFeedbackInternalNoteDraft: (value: string) => void
  createFeedbackInternalNote: () => Promise<boolean>
  startFeedbackNoteEdit: (noteId: number) => void
  setFeedbackNoteEditDraft: (value: string) => void
  cancelFeedbackNoteEdit: () => void
  saveFeedbackNoteEdit: () => Promise<boolean>
  startFeedbackNoteDelete: (noteId: number) => void
  cancelFeedbackNoteDelete: () => void
  confirmFeedbackNoteDelete: () => Promise<boolean>
  startRecovery: (feedbackId: number) => Promise<void>
  closeStartRecovery: () => void
  selectStartRecoveryIntent: (intentId: StartRecoveryIntentId) => boolean
  retryStartRecovery: () => Promise<void>
  /** Wizard actions for the four recovery intents (see `RecoveryWizardsHost`). */
  recoveryWizards: RecoveryWizardsModule
}

type ModuleState = {
  loadStatus: OperatorGuestsPageSnapshot["loadStatus"]
  viewModel: OperatorGuestsViewModel | null
  workspace: OperatorGuestsWorkspaceInput | null
  activeSmartGroupId: OperatorGuestSmartGroupId
  searchQuery: string
  sortId: OperatorGuestSortId
  page: number
  appliedFilters: OperatorFilterSelection
  filtersSession: FilterSheetSession | null
  filterCatalog: GuestTag[]
  filtersBusy: boolean
  addTagSession: AddTagDialogSession | null
  addTagBusy: boolean
  exportBusy: boolean
  actionError: string | null
  loadGeneration: number
}

const DEFAULT_PAGE_SIZE = 25
const DEFAULT_SEARCH_DEBOUNCE_MS = 300

function selectionSetsEqual(
  left: ReadonlySet<string>,
  right: ReadonlySet<string>
): boolean {
  if (left.size !== right.size) {
    return false
  }

  for (const id of left) {
    if (!right.has(id)) {
      return false
    }
  }

  return true
}

function buildSnapshot(
  state: ModuleState,
  selectedGuestIds: ReadonlySet<string>,
  tagNameById: ReadonlyMap<string, string>,
  guestDetails: GuestDetailsSnapshot,
  feedbackDetails: FeedbackDetailsSnapshot,
  recoveryWizards: RecoveryWizardsSnapshot
): OperatorGuestsPageSnapshot {
  const visibleGuestIds =
    state.viewModel?.tableRows.map((row) => row.id) ?? []
  const visibleSelection = computeVisibleSelectionState(
    selectedGuestIds,
    visibleGuestIds
  )
  const selectedCount = selectedGuestIds.size
  const locationNameById = new Map(
    (state.workspace?.locations ?? []).map((location) => [
      String(location.id),
      location.locationName,
    ])
  )
  const filterChips = projectChips(GUESTS_SCHEMA, state.appliedFilters, {
    location: (id) => locationNameById.get(id) ?? id,
    tag: (id) => tagNameById.get(id) ?? id,
  })

  return {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    searchQuery: state.searchQuery,
    sortId: state.sortId,
    selectedGuestIds: sortedSelectionIds(selectedGuestIds),
    selectedCount,
    bulkSelectionLabel: formatGuestSelectionLabel(selectedCount),
    isAllVisibleSelected: visibleSelection.isAllVisibleSelected,
    isSomeVisibleSelected: visibleSelection.isSomeVisibleSelected,
    isGuestSelected(guestId: string) {
      return selectedGuestIds.has(guestId)
    },
    appliedFilters: state.appliedFilters,
    filterChips,
    filterChipCount: chipCount(GUESTS_SCHEMA, state.appliedFilters),
    filtersSession: state.filtersSession,
    filterCatalog: state.filterCatalog,
    filtersBusy: state.filtersBusy,
    addTagSession: state.addTagSession,
    addTagBusy: state.addTagBusy,
    exportBusy: state.exportBusy,
    actionError: state.actionError,
    guestDetails,
    feedbackDetails,
    ...recoveryWizards,
  }
}

function locationScopeParams(filters: OperatorFilterSelection): {
  locationScope?: "all"
  locationIds?: number[]
} {
  const location = getLocationOverride(filters, "location")
  if (location.kind === "all") {
    return { locationScope: "all" }
  }
  if (location.kind === "individual") {
    return {
      locationIds: location.locationIds.map((id) => Number.parseInt(id, 10)),
    }
  }
  return {}
}

export function createOperatorGuestsPageModule(
  adapters: OperatorGuestsPageAdapters
): OperatorGuestsPageModule {
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS
  const getNow = adapters.getNow ?? (() => new Date())
  const guestDetails = createGuestDetailsModule({
    getGuestProfile: adapters.getGuestProfile,
    createGuestNote: adapters.createGuestNote,
  })
  const feedbackDetails = createFeedbackDetailsModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    correctClassification: adapters.correctClassification,
    setWorkflowStatus: adapters.setWorkflowStatus,
    createInternalNote: adapters.createInternalNote,
    updateInternalNote: adapters.updateInternalNote,
    deleteInternalNote: adapters.deleteInternalNote,
    closeOutFeedback: adapters.closeOutFeedback,
  })
  const recoveryWizards = createRecoveryWizardsModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    setWorkflowStatus: adapters.setWorkflowStatus,
    sendGuestResponse: adapters.sendGuestResponse,
    completeRecovery: adapters.completeRecovery,
    prepareRecoveryDraft: adapters.prepareRecoveryDraft,
    recordInternalAction: adapters.recordInternalAction,
    sendAndRecord: adapters.sendAndRecord,
    sendAndIssueRecoveryOffer: adapters.sendAndIssueRecoveryOffer,
    prepareRecoveryOfferDraft: adapters.prepareRecoveryOfferDraft,
  })

  let state: ModuleState = {
    loadStatus: "idle",
    viewModel: null,
    workspace: null,
    activeSmartGroupId: "all-guests",
    searchQuery: "",
    sortId: OPERATOR_GUEST_DEFAULT_SORT_ID,
    page: 1,
    appliedFilters: emptySelection(GUESTS_SCHEMA),
    filtersSession: null,
    filterCatalog: [],
    filtersBusy: false,
    addTagSession: null,
    addTagBusy: false,
    exportBusy: false,
    actionError: null,
    loadGeneration: 0,
  }
  let selectedGuestIds = new Set<string>()
  const tagNameById = new Map<string, string>()
  let tagMembershipsByGuestId = new Map<string, string[]>()
  let snapshot = buildSnapshot(
    state,
    selectedGuestIds,
    tagNameById,
    guestDetails.getSnapshot(),
    feedbackDetails.getSnapshot(),
    recoveryWizards.getSnapshot()
  )
  const listeners = new Set<() => void>()
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null

  const publish = () => {
    snapshot = buildSnapshot(
      state,
      selectedGuestIds,
      tagNameById,
      guestDetails.getSnapshot(),
      feedbackDetails.getSnapshot(),
      recoveryWizards.getSnapshot()
    )
    for (const listener of listeners) {
      listener()
    }
  }

  guestDetails.subscribe(() => {
    publish()
  })
  feedbackDetails.subscribe(() => {
    publish()
  })
  recoveryWizards.subscribe(() => {
    publish()
  })

  const clearSearchDebounce = () => {
    if (searchDebounceTimer != null) {
      clearTimeout(searchDebounceTimer)
      searchDebounceTimer = null
    }
  }

  const clearSelectionIfNeeded = () => {
    if (selectedGuestIds.size === 0) {
      return
    }
    selectedGuestIds = new Set()
  }

  const mergeMembershipsFromResponse = (response: GuestsResponse) => {
    for (const row of response.rows) {
      tagMembershipsByGuestId.set(
        row.id,
        (row.tagIds ?? []).map((id) => String(id))
      )
    }
  }

  const fetchGuests = async (options?: {
    quiet?: boolean
    includeAggregates?: boolean
  }) => {
    const selectedLocationId = state.workspace?.selectedLocationId
    if (selectedLocationId == null) {
      return
    }

    const generation = state.loadGeneration + 1
    const isQuiet = options?.quiet === true && state.viewModel != null
    const includeAggregates = options?.includeAggregates !== false

    state = {
      ...state,
      loadStatus: isQuiet ? state.loadStatus : "loading",
      loadGeneration: generation,
    }
    publish()

    try {
      const response = await adapters.getGuests(
        buildGuestsListQueryParams({
          locationId: selectedLocationId,
          smartGroup: state.activeSmartGroupId,
          q: state.searchQuery,
          sort: state.sortId,
          page: state.page,
          pageSize: DEFAULT_PAGE_SIZE,
          filters: state.appliedFilters,
          overviewDateRange: adapters.getGuestsOverviewDateRange(),
          now: getNow(),
          includeAggregates,
        })
      )

      if (generation !== state.loadGeneration) {
        return
      }

      mergeMembershipsFromResponse(response)

      state = {
        ...state,
        loadStatus: "loaded",
        viewModel: mapGuestsApiResponseToViewModel({
          response,
          activeSmartGroupId: state.activeSmartGroupId,
          sortId: state.sortId,
          previous: state.viewModel,
        }),
      }
      publish()
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }

      state = {
        ...state,
        loadStatus: "error",
      }
      publish()
    }
  }

  const scheduleSearchFetch = () => {
    clearSearchDebounce()
    searchDebounceTimer = setTimeout(() => {
      searchDebounceTimer = null
      void fetchGuests({ quiet: true, includeAggregates: false })
    }, debounceMs)
  }

  const runExport = async (params: GuestsExportQueryParams) => {
    if (state.exportBusy) {
      return
    }

    state = {
      ...state,
      exportBusy: true,
      actionError: null,
    }
    publish()

    try {
      const result = await adapters.exportGuestsCsv(params)
      adapters.triggerBrowserDownload(result.blob, result.filename)
      state = {
        ...state,
        exportBusy: false,
      }
      publish()
    } catch {
      state = {
        ...state,
        exportBusy: false,
        actionError: "Could not export guests. Please try again.",
      }
      publish()
    }
  }

  return {
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot() {
      return snapshot
    },
    syncWorkspace: async (input) => {
      if (input.selectedLocationId == null) {
        clearSearchDebounce()
        guestDetails.reset()
        feedbackDetails.reset()
        recoveryWizards.closeStartRecovery()
        state = {
          loadStatus: "idle",
          viewModel: null,
          workspace: null,
          activeSmartGroupId: "all-guests",
          searchQuery: "",
          sortId: OPERATOR_GUEST_DEFAULT_SORT_ID,
          page: 1,
          appliedFilters: emptySelection(GUESTS_SCHEMA),
          filtersSession: null,
          filterCatalog: [],
          filtersBusy: false,
          addTagSession: null,
          addTagBusy: false,
          exportBusy: false,
          actionError: null,
          loadGeneration: state.loadGeneration,
        }
        selectedGuestIds = new Set()
        tagMembershipsByGuestId = new Map()
        publish()
        return
      }

      const previousLocationId = state.workspace?.selectedLocationId ?? null
      const locationChanged = previousLocationId !== input.selectedLocationId

      state = {
        ...state,
        workspace: input,
      }

      if (locationChanged) {
        clearSearchDebounce()
        clearSelectionIfNeeded()
        guestDetails.reset()
        feedbackDetails.reset()
        recoveryWizards.closeStartRecovery()
        tagMembershipsByGuestId = new Map()
        state = {
          ...state,
          activeSmartGroupId: "all-guests",
          searchQuery: "",
          sortId: OPERATOR_GUEST_DEFAULT_SORT_ID,
          page: 1,
          appliedFilters: clearLocationOverrideOnShellChange(
            state.appliedFilters,
            "location"
          ),
          filtersSession: null,
          filterCatalog: [],
          addTagSession: null,
        }
        await fetchGuests()
        return
      }

      publish()
    },
    retryLoad: () => fetchGuests(),
    reloadForOverviewDateRange: () => fetchGuests({ quiet: true }),
    setActiveSmartGroupId(id) {
      if (state.activeSmartGroupId === id) {
        return
      }

      clearSearchDebounce()
      clearSelectionIfNeeded()
      state = {
        ...state,
        activeSmartGroupId: id,
        page: 1,
        viewModel:
          state.viewModel == null
            ? null
            : {
                ...state.viewModel,
                activeSmartGroupId: id,
                currentPage: 1,
              },
      }
      void fetchGuests({ quiet: true, includeAggregates: false })
    },
    setSearchQuery(query) {
      if (state.searchQuery === query) {
        return
      }

      clearSelectionIfNeeded()
      state = {
        ...state,
        searchQuery: query,
        page: 1,
      }
      publish()
      scheduleSearchFetch()
    },
    setSortId(id) {
      if (state.sortId === id) {
        return
      }

      clearSearchDebounce()
      state = {
        ...state,
        sortId: id,
        page: 1,
      }
      void fetchGuests({ quiet: true, includeAggregates: false })
    },
    setPage(page) {
      if (page < 1 || state.page === page) {
        return
      }

      clearSearchDebounce()
      state = {
        ...state,
        page,
      }
      void fetchGuests({ quiet: true, includeAggregates: false })
    },
    goToPreviousPage() {
      if (state.page <= 1) {
        return
      }

      clearSearchDebounce()
      state = {
        ...state,
        page: state.page - 1,
      }
      void fetchGuests({ quiet: true, includeAggregates: false })
    },
    goToNextPage() {
      const totalFilteredCount = state.viewModel?.totalFilteredCount ?? 0
      const maxPage = Math.max(
        1,
        Math.ceil(totalFilteredCount / DEFAULT_PAGE_SIZE)
      )
      if (state.page >= maxPage) {
        return
      }

      clearSearchDebounce()
      state = {
        ...state,
        page: state.page + 1,
      }
      void fetchGuests({ quiet: true, includeAggregates: false })
    },
    toggleGuestSelection(guestId) {
      const nextSelectedGuestIds = toggleGuestInSelection(
        selectedGuestIds,
        guestId
      )

      if (selectionSetsEqual(selectedGuestIds, nextSelectedGuestIds)) {
        return
      }

      selectedGuestIds = nextSelectedGuestIds
      publish()
    },
    toggleSelectAllVisibleRows() {
      const visibleGuestIds =
        snapshot.viewModel?.tableRows.map((row) => row.id) ?? []
      const nextSelectedGuestIds = toggleAllVisibleInSelection(
        selectedGuestIds,
        visibleGuestIds
      )

      if (selectionSetsEqual(selectedGuestIds, nextSelectedGuestIds)) {
        return
      }

      selectedGuestIds = nextSelectedGuestIds
      publish()
    },
    clearSelection() {
      if (selectedGuestIds.size === 0) {
        return
      }

      selectedGuestIds = new Set()
      publish()
    },
    clearSearchAndFilters() {
      const filtersEmpty =
        JSON.stringify(state.appliedFilters) ===
        JSON.stringify(emptySelection(GUESTS_SCHEMA))
      if (
        state.searchQuery === "" &&
        state.activeSmartGroupId === "all-guests" &&
        state.page === 1 &&
        filtersEmpty
      ) {
        return
      }

      clearSearchDebounce()
      clearSelectionIfNeeded()
      state = {
        ...state,
        searchQuery: "",
        activeSmartGroupId: "all-guests",
        page: 1,
        appliedFilters: emptySelection(GUESTS_SCHEMA),
      }
      void fetchGuests({ quiet: true, includeAggregates: false })
    },
    applyFilters(filters) {
      clearSearchDebounce()
      clearSelectionIfNeeded()
      state = {
        ...state,
        appliedFilters: filters,
        // Keep sheet open; Apply commits without closing (ADR-0017).
        filtersSession:
          state.filtersSession != null ? openSession(filters) : null,
        page: 1,
      }
      void fetchGuests({ quiet: true, includeAggregates: false })
    },
    removeFilterChip(chip) {
      clearSearchDebounce()
      clearSelectionIfNeeded()
      state = {
        ...state,
        appliedFilters: removeAppliedChip(
          GUESTS_SCHEMA,
          state.appliedFilters,
          chip
        ),
        page: 1,
      }
      void fetchGuests({ quiet: true, includeAggregates: false })
    },
    openFilters: async () => {
      const locationId = state.workspace?.selectedLocationId
      if (locationId == null) {
        return
      }

      state = {
        ...state,
        filtersBusy: true,
        filtersSession: openSession(state.appliedFilters),
        actionError: null,
      }
      publish()

      try {
        const catalog = await adapters.listGuestTags({
          locationId,
          ...locationScopeParams(state.appliedFilters),
        })
        for (const tag of catalog) {
          tagNameById.set(tag.id, tag.name)
        }

        state = {
          ...state,
          filtersBusy: false,
          filterCatalog: catalog,
        }
        publish()
      } catch {
        state = {
          ...state,
          filtersBusy: false,
          filterCatalog: [],
          actionError: "Could not load filter tags. Please try again.",
        }
        publish()
      }
    },
    closeFilters() {
      if (state.filtersSession == null && state.filterCatalog.length === 0) {
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
    exportCsv: async () => {
      const locationId = state.workspace?.selectedLocationId
      if (locationId == null) {
        return
      }

      await runExport(
        buildGuestsExportQueryParams({
          locationId,
          smartGroup: state.activeSmartGroupId,
          q: state.searchQuery,
          sort: state.sortId,
          filters: state.appliedFilters,
          now: getNow(),
        })
      )
    },
    exportSelectedCsv: async () => {
      const locationId = state.workspace?.selectedLocationId
      if (locationId == null || selectedGuestIds.size === 0) {
        return
      }

      await runExport({
        locationId,
        smartGroup: state.activeSmartGroupId,
        q: "",
        sort: state.sortId,
        guestIds: selectionIdsInCheckOrder(selectedGuestIds).map((id) =>
          Number.parseInt(id, 10)
        ),
      })
    },
    openAddTag: async (guestIds) => {
      const locationId = state.workspace?.selectedLocationId
      const ids =
        guestIds != null
          ? [...guestIds]
          : selectionIdsInCheckOrder(selectedGuestIds)
      if (locationId == null || ids.length === 0) {
        return
      }

      state = {
        ...state,
        addTagBusy: true,
        actionError: null,
      }
      publish()

      try {
        const guestIdNums = ids.map((id) => Number.parseInt(id, 10))
        const [catalog, memberships] = await Promise.all([
          adapters.listGuestTags({
            locationId,
            ...locationScopeParams(state.appliedFilters),
          }),
          adapters.getGuestTagMemberships({
            locationId,
            guestIds: guestIdNums,
          }),
        ])
        for (const tag of catalog) {
          tagNameById.set(tag.id, tag.name)
        }
        for (const guestId of ids) {
          tagMembershipsByGuestId.set(guestId, [
            ...(memberships.get(guestId) ?? []),
          ])
        }

        state = {
          ...state,
          addTagBusy: false,
          addTagSession: openAddTagSession({
            guestIds: ids,
            membershipsByGuestId: tagMembershipsByGuestId,
            catalog,
          }),
        }
        publish()
      } catch {
        state = {
          ...state,
          addTagBusy: false,
          actionError: "Could not load tags. Please try again.",
        }
        publish()
      }
    },
    closeAddTag() {
      if (state.addTagSession == null) {
        return
      }
      state = {
        ...state,
        addTagSession: null,
      }
      publish()
    },
    stageAddTag(tagId) {
      if (state.addTagSession == null) {
        return
      }
      state = {
        ...state,
        addTagSession: stageTag(state.addTagSession, tagId),
      }
      publish()
    },
    unstageAddTag(tagId) {
      if (state.addTagSession == null) {
        return
      }
      state = {
        ...state,
        addTagSession: unstageTag(state.addTagSession, tagId),
      }
      publish()
    },
    setAddTagSearch(query) {
      if (state.addTagSession == null) {
        return
      }
      state = {
        ...state,
        addTagSession: setAddTagSearchQuery(state.addTagSession, query),
      }
      publish()
    },
    setAddTagCreateOpen(open) {
      if (state.addTagSession == null) {
        return
      }
      state = {
        ...state,
        addTagSession: setSessionCreateOpen(state.addTagSession, open),
      }
      publish()
    },
    setAddTagCreateName(name) {
      if (state.addTagSession == null) {
        return
      }
      state = {
        ...state,
        addTagSession: setSessionCreateName(state.addTagSession, name),
      }
      publish()
    },
    createAndStageAddTag: async () => {
      const locationId = state.workspace?.selectedLocationId
      const session = state.addTagSession
      if (locationId == null || session == null) {
        return
      }

      const name = session.createName.trim()
      if (name.length === 0) {
        return
      }

      state = {
        ...state,
        addTagBusy: true,
        actionError: null,
      }
      publish()

      try {
        const tag = await adapters.createGuestTag({ locationId, name })
        tagNameById.set(tag.id, tag.name)
        state = {
          ...state,
          addTagBusy: false,
          addTagSession: stageCreatedTag(session, tag),
        }
        publish()
      } catch {
        state = {
          ...state,
          addTagBusy: false,
          actionError: "Could not create tag. Please try again.",
        }
        publish()
      }
    },
    applyAddTag: async () => {
      const locationId = state.workspace?.selectedLocationId
      const session = state.addTagSession
      if (locationId == null || session == null) {
        return
      }

      const { addedTagIds } = addDeltaFromPending(
        session.openTagIds,
        session.pendingTagIds
      )
      if (addedTagIds.length === 0) {
        state = {
          ...state,
          addTagSession: null,
        }
        publish()
        return
      }

      state = {
        ...state,
        addTagBusy: true,
        actionError: null,
      }
      publish()

      try {
        await adapters.applyGuestTags({
          locationId,
          guestIds: session.guestIds.map((id) => Number.parseInt(id, 10)),
          tagIds: addedTagIds.map((id) => Number.parseInt(id, 10)),
        })

        for (const guestId of session.guestIds) {
          const existing = new Set(tagMembershipsByGuestId.get(guestId) ?? [])
          for (const tagId of addedTagIds) {
            existing.add(tagId)
          }
          tagMembershipsByGuestId.set(guestId, [...existing])
        }

        state = {
          ...state,
          addTagBusy: false,
          addTagSession: null,
        }
        publish()
        await fetchGuests({ quiet: true })
      } catch {
        state = {
          ...state,
          addTagBusy: false,
          actionError: "Could not apply tags. Please try again.",
        }
        publish()
      }
    },
    openGuestDetails: async (guestId) => {
      const locationId = state.workspace?.selectedLocationId
      if (locationId == null) {
        return
      }
      feedbackDetails.close()
      recoveryWizards.closeStartRecovery()
      await guestDetails.open({ guestId, locationId })
    },
    closeGuestDetails: () => {
      feedbackDetails.close()
      recoveryWizards.closeStartRecovery()
      guestDetails.close()
    },
    retryGuestDetails: () => guestDetails.retry(),
    setGuestDetailsNoteDraft: (value) => {
      guestDetails.setNoteDraft(value)
    },
    createGuestDetailsNote: () => guestDetails.createNote(),
    openFeedbackDetails: (feedbackId) => feedbackDetails.open(feedbackId),
    closeFeedbackDetails: () => {
      feedbackDetails.close()
    },
    retryFeedbackDetails: () => feedbackDetails.retry(),
    startClassificationCorrection: () => {
      feedbackDetails.startCorrection()
    },
    setClassificationDraftSentiment: (sentiment) => {
      feedbackDetails.setDraftSentiment(sentiment)
    },
    setClassificationDraftReason: (reason) => {
      feedbackDetails.setDraftReason(reason)
    },
    setClassificationDraftNote: (note) => {
      feedbackDetails.setDraftNote(note)
    },
    cancelClassificationCorrection: () => {
      feedbackDetails.cancelCorrection()
    },
    saveClassificationCorrection: () => feedbackDetails.saveCorrection(),
    setFeedbackWorkflowStatus: (status) =>
      feedbackDetails.setWorkflowStatus(status),
    reopenFeedback: () => feedbackDetails.reopen(),
    startFeedbackMarkNoActionNeeded: () => feedbackDetails.startMarkNoActionNeeded(),
    startFeedbackMarkResolved: () => feedbackDetails.startMarkResolved(),
    setFeedbackCloseOutReason: (reason) =>
      feedbackDetails.setCloseOutReason(reason),
    setFeedbackCloseOutNoteDraft: (value) =>
      feedbackDetails.setCloseOutNoteDraft(value),
    setFeedbackCloseOutAcknowledged: (value) =>
      feedbackDetails.setCloseOutAcknowledged(value),
    cancelFeedbackCloseOut: () => feedbackDetails.cancelCloseOut(),
    confirmFeedbackCloseOut: () => feedbackDetails.confirmCloseOut(),
    setFeedbackInternalNoteDraft: (value) => {
      feedbackDetails.setNoteDraft(value)
    },
    createFeedbackInternalNote: () => feedbackDetails.createNote(),
    startFeedbackNoteEdit: (noteId) => {
      feedbackDetails.startEditNote(noteId)
    },
    setFeedbackNoteEditDraft: (value) => {
      feedbackDetails.setNoteEditDraft(value)
    },
    cancelFeedbackNoteEdit: () => {
      feedbackDetails.cancelEditNote()
    },
    saveFeedbackNoteEdit: () => feedbackDetails.saveEditNote(),
    startFeedbackNoteDelete: (noteId) => {
      feedbackDetails.startDeleteNote(noteId)
    },
    cancelFeedbackNoteDelete: () => {
      feedbackDetails.cancelDeleteNote()
    },
    confirmFeedbackNoteDelete: () => feedbackDetails.confirmDeleteNote(),
    async startRecovery(feedbackId) {
      guestDetails.close()
      feedbackDetails.close()
      await recoveryWizards.openStartRecovery(feedbackId)
    },
    closeStartRecovery: () => {
      recoveryWizards.closeStartRecovery()
    },
    selectStartRecoveryIntent: (intentId) =>
      recoveryWizards.selectStartRecoveryIntent(intentId),
    retryStartRecovery: () => recoveryWizards.retryStartRecovery(),
    recoveryWizards,
  }
}

import {
  emptyFeedbacksSelection,
  feedbacksFilterChipCount,
  hasActiveFeedbacksQuery,
  openFeedbacksFiltersSession,
  projectFeedbacksFilterChips,
  removeFeedbacksFilterChip,
  type FeedbacksFilterChip,
  type FeedbacksFiltersPanelSession,
  type GuestFeedbacksFilterSelection,
} from "@/lib/operatorGuestProfile/guestFeedbacksFilterSelection"
import {
  buildGuestFeedbacksListQueryParams,
  GUEST_FEEDBACKS_PAGE_SIZE,
  type GuestFeedbacksListQueryParams,
} from "@/lib/operatorGuestProfile/guestFeedbacksListQueryParams"
import { mapGuestFeedbacksApiResponseToViewModel } from "@/lib/operatorGuestProfile/mapGuestFeedbacksApiResponseToViewModel"
import {
  OPERATOR_GUEST_FEEDBACKS_DEFAULT_SORT_ID,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import type { GuestFeedbacksListResponse } from "@/types/dashboard"
import type {
  OperatorGuestFeedbacksSortId,
  OperatorGuestFeedbacksViewModel,
} from "@/types/operatorGuestProfile"

const DEFAULT_SEARCH_DEBOUNCE_MS = 300

export type GuestFeedbacksTabWorkspaceInput = {
  guestId: number | null
  selectedLocationId: number | null
  /** When false, the tab is not active — skip network until activated. */
  active: boolean
}

export type GuestFeedbacksTabSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorGuestFeedbacksViewModel | null
  searchQuery: string
  sortId: OperatorGuestFeedbacksSortId
  filterChips: FeedbacksFilterChip[]
  filterChipCount: number
  filtersSession: FeedbacksFiltersPanelSession | null
}

export type GuestFeedbacksTabAdapters = {
  getGuestFeedbacks: (
    params: GuestFeedbacksListQueryParams
  ) => Promise<GuestFeedbacksListResponse>
  debounceMs?: number
  getNow?: () => Date
}

export type GuestFeedbacksTabModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => GuestFeedbacksTabSnapshot
  syncWorkspace: (input: GuestFeedbacksTabWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  setSearchQuery: (query: string) => void
  setSortId: (sortId: OperatorGuestFeedbacksSortId) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  openFilters: () => void
  closeFilters: () => void
  setFiltersSession: (session: FeedbacksFiltersPanelSession) => void
  applyFilters: (filters: GuestFeedbacksFilterSelection) => void
  removeFilterChip: (chip: FeedbacksFilterChip) => void
  clearSearchAndFilters: () => void
}

type ModuleState = {
  loadStatus: GuestFeedbacksTabSnapshot["loadStatus"]
  viewModel: OperatorGuestFeedbacksViewModel | null
  workspace: GuestFeedbacksTabWorkspaceInput | null
  searchQuery: string
  sortId: OperatorGuestFeedbacksSortId
  page: number
  appliedFilters: GuestFeedbacksFilterSelection
  filtersSession: FeedbacksFiltersPanelSession | null
  loadGeneration: number
  fetchedGuestId: number | null
  fetchedLocationId: number | null
}

function buildSnapshot(state: ModuleState): GuestFeedbacksTabSnapshot {
  return {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    searchQuery: state.searchQuery,
    sortId: state.sortId,
    filterChips: projectFeedbacksFilterChips(state.appliedFilters),
    filterChipCount: feedbacksFilterChipCount(state.appliedFilters),
    filtersSession: state.filtersSession,
  }
}

function resetListState(
  workspace: GuestFeedbacksTabWorkspaceInput | null,
  loadGeneration: number
): ModuleState {
  return {
    loadStatus: "idle",
    viewModel: null,
    workspace,
    searchQuery: "",
    sortId: OPERATOR_GUEST_FEEDBACKS_DEFAULT_SORT_ID,
    page: 1,
    appliedFilters: emptyFeedbacksSelection(),
    filtersSession: null,
    loadGeneration,
    fetchedGuestId: null,
    fetchedLocationId: null,
  }
}

export function createGuestFeedbacksTabModule(
  adapters: GuestFeedbacksTabAdapters
): GuestFeedbacksTabModule {
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS
  const getNow = adapters.getNow ?? (() => new Date())

  let state: ModuleState = resetListState(null, 0)
  let snapshot = buildSnapshot(state)
  const listeners = new Set<() => void>()
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null

  const publish = () => {
    snapshot = buildSnapshot(state)
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

  const fetchFeedbacks = async () => {
    const workspace = state.workspace
    if (
      workspace == null ||
      !workspace.active ||
      workspace.guestId == null ||
      workspace.selectedLocationId == null
    ) {
      return
    }

    const generation = state.loadGeneration + 1
    state = {
      ...state,
      loadStatus: "loading",
      loadGeneration: generation,
    }
    publish()

    try {
      const response = await adapters.getGuestFeedbacks(
        buildGuestFeedbacksListQueryParams({
          guestId: workspace.guestId,
          locationId: workspace.selectedLocationId,
          q: state.searchQuery,
          sort: state.sortId,
          page: state.page,
          pageSize: GUEST_FEEDBACKS_PAGE_SIZE,
          filters: state.appliedFilters,
          now: getNow(),
        })
      )

      if (generation !== state.loadGeneration) {
        return
      }

      state = {
        ...state,
        loadStatus: "loaded",
        viewModel: mapGuestFeedbacksApiResponseToViewModel({
          response,
          sortId: state.sortId,
          hasActiveQuery: hasActiveFeedbacksQuery(
            state.searchQuery,
            state.appliedFilters
          ),
        }),
        fetchedGuestId: workspace.guestId,
        fetchedLocationId: workspace.selectedLocationId,
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
      void fetchFeedbacks()
    }, debounceMs)
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
    async syncWorkspace(input) {
      if (input.guestId == null || input.selectedLocationId == null) {
        clearSearchDebounce()
        state = resetListState(input, state.loadGeneration)
        publish()
        return
      }

      const guestOrLocationChanged =
        state.workspace?.guestId !== input.guestId ||
        state.workspace?.selectedLocationId !== input.selectedLocationId

      if (guestOrLocationChanged) {
        clearSearchDebounce()
        state = {
          ...resetListState(input, state.loadGeneration),
          workspace: input,
        }
        publish()
        if (input.active) {
          await fetchFeedbacks()
        }
        return
      }

      state = {
        ...state,
        workspace: input,
      }

      if (!input.active) {
        publish()
        return
      }

      const samePairLoaded =
        state.fetchedGuestId === input.guestId &&
        state.fetchedLocationId === input.selectedLocationId &&
        (state.loadStatus === "loaded" || state.loadStatus === "error")

      if (samePairLoaded) {
        publish()
        return
      }

      await fetchFeedbacks()
    },
    async retryLoad() {
      if (
        state.workspace == null ||
        !state.workspace.active ||
        state.workspace.guestId == null ||
        state.workspace.selectedLocationId == null
      ) {
        return
      }

      state = {
        ...state,
        fetchedGuestId: null,
        fetchedLocationId: null,
      }
      await fetchFeedbacks()
    },
    setSearchQuery(query) {
      if (!state.viewModel?.toolbarEnabled && state.loadStatus === "loaded") {
        return
      }
      state = {
        ...state,
        searchQuery: query,
        page: 1,
      }
      publish()
      scheduleSearchFetch()
    },
    setSortId(sortId) {
      if (!state.viewModel?.toolbarEnabled && state.loadStatus === "loaded") {
        return
      }
      if (state.sortId === sortId) {
        return
      }
      state = {
        ...state,
        sortId,
        page: 1,
      }
      publish()
      void fetchFeedbacks()
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
      void fetchFeedbacks()
    },
    goToNextPage() {
      const viewModel = state.viewModel
      if (viewModel == null) {
        return
      }
      const maxPage = Math.max(
        1,
        Math.ceil(viewModel.totalCount / viewModel.pageSize)
      )
      if (state.page >= maxPage) {
        return
      }
      state = {
        ...state,
        page: state.page + 1,
      }
      publish()
      void fetchFeedbacks()
    },
    openFilters() {
      if (!state.viewModel?.toolbarEnabled && state.loadStatus === "loaded") {
        return
      }
      state = {
        ...state,
        filtersSession: openFeedbacksFiltersSession(state.appliedFilters),
      }
      publish()
    },
    closeFilters() {
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
        filtersSession: null,
        page: 1,
      }
      publish()
      void fetchFeedbacks()
    },
    removeFilterChip(chip) {
      state = {
        ...state,
        appliedFilters: removeFeedbacksFilterChip(
          state.appliedFilters,
          chip
        ),
        page: 1,
      }
      publish()
      void fetchFeedbacks()
    },
    clearSearchAndFilters() {
      clearSearchDebounce()
      state = {
        ...state,
        searchQuery: "",
        appliedFilters: emptyFeedbacksSelection(),
        filtersSession: null,
        page: 1,
      }
      publish()
      void fetchFeedbacks()
    },
  }
}

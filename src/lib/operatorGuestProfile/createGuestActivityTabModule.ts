import {
  activityFilterChipCount,
  emptyActivitySelection,
  hasActiveActivityFilters,
  openActivityFiltersSession,
  projectActivityFilterChips,
  removeActivityFilterChip,
  type ActivityFilterChip,
  type ActivityFiltersPanelSession,
  type GuestActivityFilterSelection,
} from "@/lib/operatorGuestProfile/guestActivityFilterSelection"
import {
  buildGuestActivityListQueryParams,
  GUEST_ACTIVITY_PAGE_SIZE,
  type GuestActivityListQueryParams,
} from "@/lib/operatorGuestProfile/guestActivityListQueryParams"
import { mapGuestActivityApiResponseToViewModel } from "@/lib/operatorGuestProfile/mapGuestActivityApiResponseToViewModel"
import { OPERATOR_GUEST_ACTIVITY_DEFAULT_SORT_ID } from "@/lib/operatorGuestProfile/guestProfilePresentation"
import type { GuestActivityListResponse } from "@/types/dashboard"
import type {
  OperatorGuestActivitySortId,
  OperatorGuestActivityViewModel,
} from "@/types/operatorGuestProfile"

export type GuestActivityTabWorkspaceInput = {
  guestId: number | null
  selectedLocationId: number | null
  /** When false, the tab is not active — skip network until activated. */
  active: boolean
}

export type GuestActivityTabSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorGuestActivityViewModel | null
  sortId: OperatorGuestActivitySortId
  filterChips: ActivityFilterChip[]
  filterChipCount: number
  filtersSession: ActivityFiltersPanelSession | null
}

export type GuestActivityTabAdapters = {
  getGuestActivity: (
    params: GuestActivityListQueryParams
  ) => Promise<GuestActivityListResponse>
  getNow?: () => Date
}

export type GuestActivityTabModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => GuestActivityTabSnapshot
  syncWorkspace: (input: GuestActivityTabWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  setSortId: (sortId: OperatorGuestActivitySortId) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  openFilters: () => void
  closeFilters: () => void
  setFiltersSession: (session: ActivityFiltersPanelSession) => void
  applyFilters: (filters: GuestActivityFilterSelection) => void
  removeFilterChip: (chip: ActivityFilterChip) => void
  clearFilters: () => void
}

type ModuleState = {
  loadStatus: GuestActivityTabSnapshot["loadStatus"]
  viewModel: OperatorGuestActivityViewModel | null
  workspace: GuestActivityTabWorkspaceInput | null
  sortId: OperatorGuestActivitySortId
  page: number
  appliedFilters: GuestActivityFilterSelection
  filtersSession: ActivityFiltersPanelSession | null
  loadGeneration: number
  fetchedGuestId: number | null
  fetchedLocationId: number | null
}

function buildSnapshot(state: ModuleState): GuestActivityTabSnapshot {
  return {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    sortId: state.sortId,
    filterChips: projectActivityFilterChips(state.appliedFilters),
    filterChipCount: activityFilterChipCount(state.appliedFilters),
    filtersSession: state.filtersSession,
  }
}

function resetListState(
  workspace: GuestActivityTabWorkspaceInput | null,
  loadGeneration: number
): ModuleState {
  return {
    loadStatus: "idle",
    viewModel: null,
    workspace,
    sortId: OPERATOR_GUEST_ACTIVITY_DEFAULT_SORT_ID,
    page: 1,
    appliedFilters: emptyActivitySelection(),
    filtersSession: null,
    loadGeneration,
    fetchedGuestId: null,
    fetchedLocationId: null,
  }
}

export function createGuestActivityTabModule(
  adapters: GuestActivityTabAdapters
): GuestActivityTabModule {
  const getNow = adapters.getNow ?? (() => new Date())

  let state: ModuleState = resetListState(null, 0)
  let snapshot = buildSnapshot(state)
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = buildSnapshot(state)
    for (const listener of listeners) {
      listener()
    }
  }

  const fetchActivity = async () => {
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
      const response = await adapters.getGuestActivity(
        buildGuestActivityListQueryParams({
          guestId: workspace.guestId,
          locationId: workspace.selectedLocationId,
          sort: state.sortId,
          page: state.page,
          pageSize: GUEST_ACTIVITY_PAGE_SIZE,
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
        viewModel: mapGuestActivityApiResponseToViewModel({
          response,
          sortId: state.sortId,
          hasActiveFilters: hasActiveActivityFilters(state.appliedFilters),
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
        state = resetListState(input, state.loadGeneration)
        publish()
        return
      }

      const guestOrLocationChanged =
        state.workspace?.guestId !== input.guestId ||
        state.workspace?.selectedLocationId !== input.selectedLocationId

      if (guestOrLocationChanged) {
        state = {
          ...resetListState(input, state.loadGeneration),
          workspace: input,
        }
        publish()
        if (input.active) {
          await fetchActivity()
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

      await fetchActivity()
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
      await fetchActivity()
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
      void fetchActivity()
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
      void fetchActivity()
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
      void fetchActivity()
    },
    openFilters() {
      if (!state.viewModel?.toolbarEnabled && state.loadStatus === "loaded") {
        return
      }
      state = {
        ...state,
        filtersSession: openActivityFiltersSession(state.appliedFilters),
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
      void fetchActivity()
    },
    removeFilterChip(chip) {
      state = {
        ...state,
        appliedFilters: removeActivityFilterChip(
          state.appliedFilters,
          chip
        ),
        page: 1,
      }
      publish()
      void fetchActivity()
    },
    clearFilters() {
      state = {
        ...state,
        appliedFilters: emptyActivitySelection(),
        filtersSession: null,
        page: 1,
      }
      publish()
      void fetchActivity()
    },
  }
}

export type GuestProfileFilteredListWorkspace = {
  guestId: number | null
  selectedLocationId: number | null
  active: boolean
}

type FilteredListViewModel = {
  totalCount: number
  pageSize: number
  toolbarEnabled: boolean
}

export type GuestProfileFilteredListCoreSnapshot<
  TViewModel extends FilteredListViewModel,
  TSortId,
  TFilters,
  TSession,
> = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: TViewModel | null
  workspace: GuestProfileFilteredListWorkspace | null
  sortId: TSortId
  page: number
  appliedFilters: TFilters
  filtersSession: TSession | null
}

export type GuestProfileFilteredListKernelAdapters<
  TViewModel extends FilteredListViewModel,
  TSortId,
  TFilters,
> = {
  defaultSortId: TSortId
  emptyFilters: () => TFilters
  load: (input: {
    guestId: number
    locationId: number
    sortId: TSortId
    page: number
    filters: TFilters
  }) => Promise<TViewModel>
}

export type GuestProfileFilteredListKernel<
  TViewModel extends FilteredListViewModel,
  TSortId,
  TFilters,
  TSession,
> = {
  subscribe: (listener: () => void) => () => void
  getCoreSnapshot: () => GuestProfileFilteredListCoreSnapshot<
    TViewModel,
    TSortId,
    TFilters,
    TSession
  >
  syncWorkspace: (input: GuestProfileFilteredListWorkspace) => Promise<void>
  reload: () => Promise<void>
  resetPage: () => void
  setSortId: (sortId: TSortId) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  openFiltersSession: (session: TSession) => void
  closeFilters: () => void
  setFiltersSession: (session: TSession) => void
  applyFilters: (filters: TFilters) => void
  /** Updates applied filters and reloads without closing an open filters session. */
  replaceFilters: (filters: TFilters) => void
  clearFilters: () => void
}

export function createGuestProfileFilteredListKernel<
  TViewModel extends FilteredListViewModel,
  TSortId,
  TFilters,
  TSession,
>(
  adapters: GuestProfileFilteredListKernelAdapters<TViewModel, TSortId, TFilters>
): GuestProfileFilteredListKernel<TViewModel, TSortId, TFilters, TSession> {
  type Snapshot = GuestProfileFilteredListCoreSnapshot<
    TViewModel,
    TSortId,
    TFilters,
    TSession
  >

  let loadGeneration = 0
  let completedGuestId: number | null = null
  let completedLocationId: number | null = null
  let snapshot: Snapshot = {
    loadStatus: "idle",
    viewModel: null,
    workspace: null,
    sortId: adapters.defaultSortId,
    page: 1,
    appliedFilters: adapters.emptyFilters(),
    filtersSession: null,
  }
  const listeners = new Set<() => void>()

  const publish = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const reset = (workspace: GuestProfileFilteredListWorkspace) => {
    loadGeneration += 1
    completedGuestId = null
    completedLocationId = null
    snapshot = {
      loadStatus: "idle",
      viewModel: null,
      workspace,
      sortId: adapters.defaultSortId,
      page: 1,
      appliedFilters: adapters.emptyFilters(),
      filtersSession: null,
    }
    publish()
  }

  const fetchList = async () => {
    const { workspace } = snapshot
    if (
      workspace == null ||
      !workspace.active ||
      workspace.guestId == null ||
      workspace.selectedLocationId == null
    ) {
      return
    }

    const generation = ++loadGeneration
    const request = {
      guestId: workspace.guestId,
      locationId: workspace.selectedLocationId,
      sortId: snapshot.sortId,
      page: snapshot.page,
      filters: snapshot.appliedFilters,
    }
    snapshot = { ...snapshot, loadStatus: "loading" }
    publish()

    try {
      const viewModel = await adapters.load(request)
      if (generation !== loadGeneration) {
        return
      }

      completedGuestId = request.guestId
      completedLocationId = request.locationId
      snapshot = { ...snapshot, loadStatus: "loaded", viewModel }
      publish()
    } catch {
      if (generation !== loadGeneration) {
        return
      }

      // Leave completed* from any prior success so same-pair skip still works
      // after a later failed reload; first-load failures stay refetchable.
      snapshot = { ...snapshot, loadStatus: "error" }
      publish()
    }
  }

  const toolbarLocked = () =>
    snapshot.loadStatus === "loaded" && !snapshot.viewModel?.toolbarEnabled

  return {
    subscribe(listener) {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
    getCoreSnapshot() {
      return snapshot
    },
    async syncWorkspace(input) {
      if (input.guestId == null || input.selectedLocationId == null) {
        reset(input)
        return
      }

      const changed =
        snapshot.workspace?.guestId !== input.guestId ||
        snapshot.workspace?.selectedLocationId !== input.selectedLocationId
      if (changed) {
        reset(input)
        if (input.active) {
          await fetchList()
        }
        return
      }

      snapshot = { ...snapshot, workspace: input }
      if (!input.active) {
        publish()
        return
      }

      const completedSamePair =
        completedGuestId === input.guestId &&
        completedLocationId === input.selectedLocationId &&
        (snapshot.loadStatus === "loaded" || snapshot.loadStatus === "error")
      if (completedSamePair) {
        publish()
        return
      }

      await fetchList()
    },
    async reload() {
      completedGuestId = null
      completedLocationId = null
      await fetchList()
    },
    resetPage() {
      if (snapshot.page === 1) {
        return
      }
      snapshot = { ...snapshot, page: 1 }
      publish()
    },
    setSortId(sortId) {
      if (toolbarLocked() || snapshot.sortId === sortId) {
        return
      }
      snapshot = { ...snapshot, sortId, page: 1 }
      publish()
      void fetchList()
    },
    goToPreviousPage() {
      if (snapshot.page <= 1) {
        return
      }
      snapshot = { ...snapshot, page: snapshot.page - 1 }
      publish()
      void fetchList()
    },
    goToNextPage() {
      const { viewModel } = snapshot
      if (viewModel == null) {
        return
      }
      const maxPage = Math.max(1, Math.ceil(viewModel.totalCount / viewModel.pageSize))
      if (snapshot.page >= maxPage) {
        return
      }
      snapshot = { ...snapshot, page: snapshot.page + 1 }
      publish()
      void fetchList()
    },
    openFiltersSession(session) {
      if (toolbarLocked()) {
        return
      }
      snapshot = { ...snapshot, filtersSession: session }
      publish()
    },
    closeFilters() {
      snapshot = { ...snapshot, filtersSession: null }
      publish()
    },
    setFiltersSession(session) {
      snapshot = { ...snapshot, filtersSession: session }
      publish()
    },
    applyFilters(filters) {
      snapshot = {
        ...snapshot,
        appliedFilters: filters,
        filtersSession: null,
        page: 1,
      }
      publish()
      void fetchList()
    },
    replaceFilters(filters) {
      snapshot = {
        ...snapshot,
        appliedFilters: filters,
        page: 1,
      }
      publish()
      void fetchList()
    },
    clearFilters() {
      snapshot = {
        ...snapshot,
        appliedFilters: adapters.emptyFilters(),
        filtersSession: null,
        page: 1,
      }
      publish()
      void fetchList()
    },
  }
}

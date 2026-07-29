import {
  OPERATOR_CAPTURE_LOCATION_ROW_ACTIONS,
  type OperatorCaptureLocationRowActionId,
} from "@/lib/operatorCapture/capturePresentation"
import {
  buildCaptureOverviewKpis,
  type CaptureOverviewFacts,
  type OperatorCaptureOverviewKpi,
} from "@/lib/operatorMultiCapture/buildCaptureOverviewKpis"
import {
  buildCaptureLocationPerformanceRows,
  formatCaptureLocationPageRangeLabel,
  OPERATOR_CAPTURE_LOCATION_DEFAULT_SORT_ID,
  OPERATOR_CAPTURE_LOCATION_PAGE_SIZE,
  OPERATOR_CAPTURE_LOCATION_SORT_LABELS,
  type CaptureLocationPerformanceEmptyKind,
  type OperatorCaptureLocationPerformanceRow,
} from "@/lib/operatorMultiCapture/buildCaptureLocationPerformance"
import { captureLocationsFilterSheetSchema } from "@/lib/operatorMultiCapture/captureLocationsFilterSheetSchema"
import {
  chipCount,
  cloneSelection,
  emptySelection,
  openSession,
  projectChips,
  removeAppliedChip,
  type FilterChip,
  type FilterSheetSession,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import {
  getLocationOverride,
  getMultiSelectIds,
} from "@/lib/operatorFilterSheet/selectionHelpers"
import {
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  CaptureLocationsQueryParams,
  CaptureLocationsResponse,
  CaptureLocationsSortId,
  CaptureLocationStatus,
  CaptureOverviewResponse,
} from "@/types/dashboard"

export type OperatorMultiCaptureWorkspaceLocation = {
  id: number
  locationName: string
  address?: string
}

export type OperatorMultiCaptureWorkspaceInput = {
  locations: readonly OperatorMultiCaptureWorkspaceLocation[]
}

export type OperatorMultiCaptureOverviewView = {
  kpis: OperatorCaptureOverviewKpi[]
  /** Zero owned locations — show no-locations empty copy. */
  isNoLocations: boolean
  /** Overview load failed — empty body, no KPI strip. */
  isLoadError: boolean
}

export type OperatorMultiCaptureLocationPerformanceView = {
  rows: OperatorCaptureLocationPerformanceRow[]
  totalCount: number
  currentPage: number
  pageSize: number
  pageRangeLabel: string
  sortId: CaptureLocationsSortId
  sortLabel: string
  emptyKind: CaptureLocationPerformanceEmptyKind | null
  showToolbar: boolean
  showPagination: boolean
}

export type OperatorMultiCaptureViewModel = {
  dateRangeLabel: string
  overview: OperatorMultiCaptureOverviewView
  locationPerformance: OperatorMultiCaptureLocationPerformanceView
}

export type OperatorMultiCapturePageSnapshot = {
  loadStatus: "idle" | "loading" | "ready" | "error"
  overviewLoadStatus: "idle" | "loading" | "loaded" | "error"
  listLoadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorMultiCaptureViewModel | null
  searchQuery: string
  sortId: CaptureLocationsSortId
  page: number
  appliedFilters: OperatorFilterSelection
  filtersSession: FilterSheetSession | null
  filterChips: readonly FilterChip[]
  filterChipCount: number
}

export type OperatorCaptureLocationRowAction = {
  id: OperatorCaptureLocationRowActionId
  label: string
  enabled: boolean
}

export type OperatorMultiCapturePageAdapters = {
  getCaptureOverview: (
    from: string,
    to: string
  ) => Promise<CaptureOverviewResponse>
  getCaptureLocations: (
    params: CaptureLocationsQueryParams
  ) => Promise<CaptureLocationsResponse>
  getMultiCaptureOverviewDateRange: () => HomePerformanceDateRange
  /** Sync workspace selected location before nested Capture navigation. */
  syncSelectedLocation: (locationId: number) => void
  /** Navigate to `/multi-dashboard/capture/locations/:locationId`. */
  navigateToCaptureLocation: (locationId: number) => void
  onOverviewLoadError?: (message: string) => void
  onLocationsLoadError?: (message: string) => void
  /** Optional delay seam for tests and brief first-load spinner. */
  scheduleReady?: () => Promise<void>
  debounceMs?: number
  getNow?: () => Date
}

export type OperatorMultiCapturePageModule = {
  getSnapshot: () => OperatorMultiCapturePageSnapshot
  subscribe: (listener: () => void) => () => void
  syncWorkspace: (input: OperatorMultiCaptureWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  /** Re-load overview + list using the current Multi Capture overview date range. */
  reloadForMultiCaptureOverviewDateRange: () => Promise<void>
  /** Sync workspace selected location + navigate to nested Capture. */
  navigateToLocationCapture: (locationId: number) => void
  /** Row ⋯ action catalog — enabled View + stubbed future actions. */
  getLocationRowActions: () => readonly OperatorCaptureLocationRowAction[]
  setSearchQuery: (query: string) => void
  setSortId: (id: CaptureLocationsSortId) => void
  setPage: (page: number) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  clearSearchAndFilters: () => void
  applyFilters: (filters: OperatorFilterSelection) => void
  removeFilterChip: (chip: FilterChip) => void
  openFilters: () => void
  closeFilters: () => void
  setFiltersSession: (session: FilterSheetSession) => void
}

type ModuleState = {
  loadStatus: OperatorMultiCapturePageSnapshot["loadStatus"]
  overviewLoadStatus: OperatorMultiCapturePageSnapshot["overviewLoadStatus"]
  listLoadStatus: OperatorMultiCapturePageSnapshot["listLoadStatus"]
  viewModel: OperatorMultiCaptureViewModel | null
  workspace: OperatorMultiCaptureWorkspaceInput | null
  searchQuery: string
  sortId: CaptureLocationsSortId
  page: number
  appliedFilters: OperatorFilterSelection
  filtersSession: FilterSheetSession | null
  loadGeneration: number
  overviewLoadGeneration: number
  listLoadGeneration: number
}

const OVERVIEW_LOAD_ERROR_MESSAGE =
  "Could not load Capture overview. Please try again."
const LOCATIONS_LOAD_ERROR_MESSAGE =
  "Could not load location performance. Please try again."
const DEFAULT_SEARCH_DEBOUNCE_MS = 300
const FILTER_SCHEMA = captureLocationsFilterSheetSchema()

function factsFromResponse(
  response: CaptureOverviewResponse
): CaptureOverviewFacts {
  return {
    activeLocations: response.activeLocations,
    totalLocations: response.totalLocations,
    activeQrPlacements: response.activeQrPlacements,
    qrScans: response.qrScans,
    qrScansPrevious: response.qrScansPrevious,
    feedbackSubmitted: response.feedbackSubmitted,
    feedbackSubmittedPrevious: response.feedbackSubmittedPrevious,
    marketingOptIns: response.marketingOptIns,
    marketingOptInsPrevious: response.marketingOptInsPrevious,
    offerClaims: response.offerClaims,
    offerClaimsHasRealData: response.offerClaimsHasRealData,
  }
}

function noLocationsOverviewView(): OperatorMultiCaptureOverviewView {
  return {
    kpis: [],
    isNoLocations: true,
    isLoadError: false,
  }
}

function loadErrorOverviewView(): OperatorMultiCaptureOverviewView {
  return {
    kpis: [],
    isNoLocations: false,
    isLoadError: true,
  }
}

function emptyLocationPerformanceView(
  options: {
    sortId: CaptureLocationsSortId
    emptyKind: CaptureLocationPerformanceEmptyKind
    showToolbar: boolean
  }
): OperatorMultiCaptureLocationPerformanceView {
  return {
    rows: [],
    totalCount: 0,
    currentPage: 1,
    pageSize: OPERATOR_CAPTURE_LOCATION_PAGE_SIZE,
    pageRangeLabel: formatCaptureLocationPageRangeLabel(
      1,
      OPERATOR_CAPTURE_LOCATION_PAGE_SIZE,
      0
    ),
    sortId: options.sortId,
    sortLabel: OPERATOR_CAPTURE_LOCATION_SORT_LABELS[options.sortId],
    emptyKind: options.emptyKind,
    showToolbar: options.showToolbar,
    showPagination: false,
  }
}

function buildListQueryParams(options: {
  from: string
  to: string
  searchQuery: string
  sortId: CaptureLocationsSortId
  page: number
  filters: OperatorFilterSelection
}): CaptureLocationsQueryParams {
  const location = getLocationOverride(options.filters, "location")
  const locationIds =
    location.kind === "individual"
      ? location.locationIds
          .map((id) => Number.parseInt(id, 10))
          .filter((id) => Number.isFinite(id))
      : undefined

  const statusIds = getMultiSelectIds(options.filters, "status") as
    | CaptureLocationStatus[]
    | undefined

  return {
    from: options.from,
    to: options.to,
    q: options.searchQuery.trim() || undefined,
    status:
      statusIds != null && statusIds.length > 0 ? statusIds : undefined,
    locationIds:
      locationIds != null && locationIds.length > 0
        ? locationIds
        : undefined,
    sort: options.sortId,
    page: options.page,
    pageSize: OPERATOR_CAPTURE_LOCATION_PAGE_SIZE,
  }
}

function mapLocationsResponse(options: {
  response: CaptureLocationsResponse
  sortId: CaptureLocationsSortId
  nowMs: number
}): OperatorMultiCaptureLocationPerformanceView {
  const rows = buildCaptureLocationPerformanceRows(
    options.response.items,
    options.nowMs
  )
  const totalCount = options.response.totalCount
  const page = options.response.page
  const pageSize = options.response.pageSize
  // Zero Owned locations never reach this mapper (handled before API). A zero
  // total here means search/filters matched nothing — keep toolbar visible.
  const emptyKind: CaptureLocationPerformanceEmptyKind | null =
    totalCount === 0 ? "no-results" : null

  return {
    rows,
    totalCount,
    currentPage: page,
    pageSize,
    pageRangeLabel: formatCaptureLocationPageRangeLabel(
      page,
      pageSize,
      totalCount
    ),
    sortId: options.sortId,
    sortLabel: OPERATOR_CAPTURE_LOCATION_SORT_LABELS[options.sortId],
    emptyKind,
    showToolbar: true,
    showPagination: totalCount > 0,
  }
}

/**
 * Operator Multi Capture page module — adapters in, snapshot out.
 * Owns Capture overview + Location performance list chrome and date co-refresh.
 */
export function createOperatorMultiCapturePageModule(
  adapters: OperatorMultiCapturePageAdapters
): OperatorMultiCapturePageModule {
  const scheduleReady =
    adapters.scheduleReady ?? (() => Promise.resolve())
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS
  const getNow = adapters.getNow ?? (() => new Date())

  let state: ModuleState = {
    loadStatus: "idle",
    overviewLoadStatus: "idle",
    listLoadStatus: "idle",
    viewModel: null,
    workspace: null,
    searchQuery: "",
    sortId: OPERATOR_CAPTURE_LOCATION_DEFAULT_SORT_ID,
    page: 1,
    appliedFilters: emptySelection(FILTER_SCHEMA),
    filtersSession: null,
    loadGeneration: 0,
    overviewLoadGeneration: 0,
    listLoadGeneration: 0,
  }
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

  const currentDateRangeLabel = () =>
    labelForHomePerformanceDateRange(
      adapters.getMultiCaptureOverviewDateRange()
    )

  const locationSchema = () =>
    captureLocationsFilterSheetSchema({
      locations: (state.workspace?.locations ?? []).map((location) => ({
        id: String(location.id),
        label: location.locationName,
      })),
    })

  const buildViewModel = (
    overview: OperatorMultiCaptureOverviewView,
    locationPerformance: OperatorMultiCaptureLocationPerformanceView
  ): OperatorMultiCaptureViewModel => ({
    dateRangeLabel: currentDateRangeLabel(),
    overview,
    locationPerformance,
  })

  const fetchList = async (options?: {
    quiet?: boolean
  }): Promise<void> => {
    const workspace = state.workspace
    if (workspace == null) {
      return
    }

    const generation = ++state.listLoadGeneration
    const isQuiet = options?.quiet === true && state.viewModel != null

    state = {
      ...state,
      listLoadStatus: "loading",
      ...(isQuiet
        ? {}
        : state.loadStatus === "ready"
          ? {}
          : { loadStatus: "loading" as const }),
    }
    publish()

    if (workspace.locations.length === 0) {
      if (generation !== state.listLoadGeneration) {
        return
      }

      const overview =
        state.viewModel?.overview ?? noLocationsOverviewView()
      state = {
        ...state,
        listLoadStatus: "loaded",
        viewModel: buildViewModel(
          overview,
          emptyLocationPerformanceView({
            sortId: state.sortId,
            emptyKind: "no-locations",
            showToolbar: false,
          })
        ),
      }
      publish()
      return
    }

    const overviewWindow = resolveHomePerformanceWindow(
      adapters.getMultiCaptureOverviewDateRange()
    )
    const from = overviewWindow.from.toISOString()
    const to = overviewWindow.to.toISOString()

    const listSettled = await adapters
      .getCaptureLocations(
        buildListQueryParams({
          from,
          to,
          searchQuery: state.searchQuery,
          sortId: state.sortId,
          page: state.page,
          filters: state.appliedFilters,
        })
      )
      .then((response) => ({ ok: true as const, response }))
      .catch(() => ({ ok: false as const }))

    if (generation !== state.listLoadGeneration) {
      return
    }

    const overview =
      state.viewModel?.overview ??
      (listSettled.ok
        ? {
            kpis: [],
            isNoLocations: false,
            isLoadError: false,
          }
        : loadErrorOverviewView())

    if (!listSettled.ok) {
      state = {
        ...state,
        listLoadStatus: "error",
        viewModel: buildViewModel(
          overview,
          emptyLocationPerformanceView({
            sortId: state.sortId,
            emptyKind: "load-error",
            showToolbar: false,
          })
        ),
      }
      publish()
      adapters.onLocationsLoadError?.(LOCATIONS_LOAD_ERROR_MESSAGE)
      return
    }

    state = {
      ...state,
      listLoadStatus: "loaded",
      viewModel: buildViewModel(
        overview,
        mapLocationsResponse({
          response: listSettled.response,
          sortId: state.sortId,
          nowMs: getNow().getTime(),
        })
      ),
    }
    publish()
  }

  const fetchOverviewAndList = async (options: {
    workspace: OperatorMultiCaptureWorkspaceInput
    isInitialLoad: boolean
  }): Promise<void> => {
    const overviewGeneration = ++state.overviewLoadGeneration
    const listGeneration = ++state.listLoadGeneration

    state = {
      ...state,
      overviewLoadStatus: "loading",
      listLoadStatus: "loading",
      ...(options.isInitialLoad
        ? { loadStatus: "loading" as const, viewModel: null }
        : {}),
    }
    publish()

    await scheduleReady()

    if (
      overviewGeneration !== state.overviewLoadGeneration ||
      listGeneration !== state.listLoadGeneration
    ) {
      return
    }

    if (options.workspace.locations.length === 0) {
      state = {
        ...state,
        loadStatus: "ready",
        overviewLoadStatus: "loaded",
        listLoadStatus: "loaded",
        viewModel: buildViewModel(
          noLocationsOverviewView(),
          emptyLocationPerformanceView({
            sortId: state.sortId,
            emptyKind: "no-locations",
            showToolbar: false,
          })
        ),
        workspace: options.workspace,
      }
      publish()
      return
    }

    const overviewWindow = resolveHomePerformanceWindow(
      adapters.getMultiCaptureOverviewDateRange()
    )
    const from = overviewWindow.from.toISOString()
    const to = overviewWindow.to.toISOString()

    const [overviewSettled, listSettled] = await Promise.all([
      adapters
        .getCaptureOverview(from, to)
        .then((response) => ({ ok: true as const, response }))
        .catch(() => ({ ok: false as const })),
      adapters
        .getCaptureLocations(
          buildListQueryParams({
            from,
            to,
            searchQuery: state.searchQuery,
            sortId: state.sortId,
            page: state.page,
            filters: state.appliedFilters,
          })
        )
        .then((response) => ({ ok: true as const, response }))
        .catch(() => ({ ok: false as const })),
    ])

    if (
      overviewGeneration !== state.overviewLoadGeneration ||
      listGeneration !== state.listLoadGeneration
    ) {
      return
    }

    const overview = overviewSettled.ok
      ? {
          kpis: buildCaptureOverviewKpis(
            factsFromResponse(overviewSettled.response)
          ).kpis,
          isNoLocations: false,
          isLoadError: false,
        }
      : loadErrorOverviewView()

    const locationPerformance = listSettled.ok
      ? mapLocationsResponse({
          response: listSettled.response,
          sortId: state.sortId,
          nowMs: getNow().getTime(),
        })
      : emptyLocationPerformanceView({
          sortId: state.sortId,
          emptyKind: "load-error",
          showToolbar: false,
        })

    state = {
      ...state,
      loadStatus: "ready",
      overviewLoadStatus: overviewSettled.ok ? "loaded" : "error",
      listLoadStatus: listSettled.ok ? "loaded" : "error",
      viewModel: buildViewModel(overview, locationPerformance),
      workspace: options.workspace,
    }
    publish()

    if (!overviewSettled.ok) {
      adapters.onOverviewLoadError?.(OVERVIEW_LOAD_ERROR_MESSAGE)
    }
    if (!listSettled.ok) {
      adapters.onLocationsLoadError?.(LOCATIONS_LOAD_ERROR_MESSAGE)
    }
  }

  const loadForWorkspace = async (
    input: OperatorMultiCaptureWorkspaceInput
  ): Promise<void> => {
    clearSearchDebounce()
    const generation = ++state.loadGeneration
    state = {
      ...state,
      loadStatus: "loading",
      overviewLoadStatus: "idle",
      listLoadStatus: "idle",
      viewModel: null,
      workspace: input,
      searchQuery: "",
      sortId: OPERATOR_CAPTURE_LOCATION_DEFAULT_SORT_ID,
      page: 1,
      appliedFilters: emptySelection(FILTER_SCHEMA),
      filtersSession: null,
    }
    publish()

    await fetchOverviewAndList({ workspace: input, isInitialLoad: true })

    if (generation !== state.loadGeneration) {
      return
    }
  }

  const scheduleSearchFetch = () => {
    clearSearchDebounce()
    searchDebounceTimer = setTimeout(() => {
      searchDebounceTimer = null
      void fetchList({ quiet: true })
    }, debounceMs)
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
    async syncWorkspace(input) {
      await loadForWorkspace(input)
    },
    async retryLoad() {
      if (state.workspace == null) {
        return
      }
      await loadForWorkspace(state.workspace)
    },
    async reloadForMultiCaptureOverviewDateRange() {
      if (state.workspace == null) {
        return
      }
      await fetchOverviewAndList({
        workspace: state.workspace,
        isInitialLoad: false,
      })
    },
    navigateToLocationCapture(locationId) {
      adapters.syncSelectedLocation(locationId)
      adapters.navigateToCaptureLocation(locationId)
    },
    getLocationRowActions() {
      return OPERATOR_CAPTURE_LOCATION_ROW_ACTIONS
    },
    setSearchQuery(query) {
      if (state.searchQuery === query) {
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
      publish()
      void fetchList({ quiet: true })
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
      publish()
      void fetchList({ quiet: true })
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
      publish()
      void fetchList({ quiet: true })
    },
    goToNextPage() {
      const totalFilteredCount =
        state.viewModel?.locationPerformance.totalCount ?? 0
      const maxPage = Math.max(
        1,
        Math.ceil(totalFilteredCount / OPERATOR_CAPTURE_LOCATION_PAGE_SIZE)
      )
      if (state.page >= maxPage) {
        return
      }

      clearSearchDebounce()
      state = {
        ...state,
        page: state.page + 1,
      }
      publish()
      void fetchList({ quiet: true })
    },
    clearSearchAndFilters() {
      const filtersEmpty =
        JSON.stringify(state.appliedFilters) ===
        JSON.stringify(emptySelection(FILTER_SCHEMA))
      if (
        state.searchQuery === "" &&
        state.page === 1 &&
        filtersEmpty
      ) {
        return
      }

      clearSearchDebounce()
      state = {
        ...state,
        searchQuery: "",
        page: 1,
        appliedFilters: emptySelection(FILTER_SCHEMA),
      }
      publish()
      void fetchList({ quiet: true })
    },
    applyFilters(filters) {
      clearSearchDebounce()
      state = {
        ...state,
        appliedFilters: filters,
        filtersSession:
          state.filtersSession != null ? openSession(filters) : null,
        page: 1,
      }
      publish()
      void fetchList({ quiet: true })
    },
    removeFilterChip(chip) {
      clearSearchDebounce()
      state = {
        ...state,
        appliedFilters: removeAppliedChip(
          locationSchema(),
          state.appliedFilters,
          chip
        ),
        page: 1,
      }
      publish()
      void fetchList({ quiet: true })
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
  }
}

function buildSnapshot(
  state: ModuleState
): OperatorMultiCapturePageSnapshot {
  const schema = captureLocationsFilterSheetSchema({
    locations: (state.workspace?.locations ?? []).map((location) => ({
      id: String(location.id),
      label: location.locationName,
    })),
  })
  const filterChips = projectChips(schema, state.appliedFilters, {
    location: (id) =>
      state.workspace?.locations.find(
        (location) => String(location.id) === id
      )?.locationName ?? id,
  })

  return {
    loadStatus: state.loadStatus,
    overviewLoadStatus: state.overviewLoadStatus,
    listLoadStatus: state.listLoadStatus,
    viewModel: state.viewModel,
    searchQuery: state.searchQuery,
    sortId: state.sortId,
    page: state.page,
    appliedFilters: cloneSelection(state.appliedFilters),
    filtersSession: state.filtersSession,
    filterChips,
    filterChipCount: chipCount(schema, state.appliedFilters),
  }
}

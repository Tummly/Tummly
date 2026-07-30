import {
  OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY,
  OPERATOR_CAPTURE_LOCATION_ROW_ACTION_DEFS,
  type OperatorCaptureLocationRowActionId,
} from "@/lib/operatorCapture/capturePresentation"
import {
  buildLocationCaptureConfirm,
  type LocationCaptureConfirmView,
} from "@/lib/operatorCapture/buildLocationCaptureConfirm"
import {
  buildGuestExperiencePreviewPicker,
  type GuestExperiencePreviewPickerFact,
} from "@/lib/operatorCapture/buildGuestExperiencePreviewPicker"
import type {
  CreateDigitalGuestLinkAdapterResult,
  CreateDigitalGuestLinkModuleInput,
  CreateDigitalGuestLinkModuleResult,
  GuestExperiencePreviewPickerSnapshot,
  OpenGuestExperiencePreviewResult,
} from "@/lib/operatorCapture/createOperatorCapturePageModule"
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
  CaptureLocationItem,
  CaptureLocationsQueryParams,
  CaptureLocationsResponse,
  CaptureLocationsSortId,
  CaptureLocationStatus,
  CaptureOverviewResponse,
  CapturePlacementsResponse,
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

export type OperatorMultiCaptureCreateDialogSnapshot = {
  isOpen: boolean
  /** When true, Locations is pre-bound (row Create) — hide or read-only in UI. */
  locationBound: boolean
  selectedLocationId: number | null
  busy: boolean
}

export type OperatorMultiCaptureLocationCaptureConfirmSnapshot = {
  isOpen: boolean
  busy: boolean
  details: LocationCaptureConfirmView | null
}

export type ConfirmLocationCaptureResult =
  | { outcome: "paused" | "activated"; toastMessage: string }
  | "failed"
  | "noop"

export type OperatorMultiCaptureGuestExperiencePreviewSnapshot = {
  isOpen: boolean
  placementLabel: string | null
  locationName: string
  locationAddress: string
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
  canCreateDigitalGuestLink: boolean
  createDialog: OperatorMultiCaptureCreateDialogSnapshot
  guestExperiencePreviewPicker: GuestExperiencePreviewPickerSnapshot
  guestExperiencePreview: OperatorMultiCaptureGuestExperiencePreviewSnapshot
  locationCaptureConfirm: OperatorMultiCaptureLocationCaptureConfirmSnapshot
}

export type OperatorCaptureLocationRowAction = {
  id: OperatorCaptureLocationRowActionId
  label: string
  enabled: boolean
}

export type NavigateToCaptureLocationOptions = {
  openPlacementDetailQrCodeId?: number
}

export type OperatorMultiCapturePageAdapters = {
  getCaptureOverview: (
    from: string,
    to: string
  ) => Promise<CaptureOverviewResponse>
  getCaptureLocations: (
    params: CaptureLocationsQueryParams
  ) => Promise<CaptureLocationsResponse>
  getCapturePlacements: (
    locationId: number,
    from: string,
    to: string
  ) => Promise<CapturePlacementsResponse>
  createDigitalGuestLink: (
    locationId: number,
    input: CreateDigitalGuestLinkModuleInput
  ) => Promise<CreateDigitalGuestLinkAdapterResult>
  pauseLocationCapture: (
    locationId: number
  ) => Promise<
    | { ok: true; status: CaptureLocationStatus; pauseRestoreQrCodeCount: number }
    | { ok: false; message: string }
  >
  activateLocationCapture: (
    locationId: number
  ) => Promise<
    | { ok: true; status: CaptureLocationStatus; pauseRestoreQrCodeCount: number }
    | { ok: false; message: string }
  >
  getMultiCaptureOverviewDateRange: () => HomePerformanceDateRange
  /** Sync workspace selected location before nested Capture navigation. */
  syncSelectedLocation: (locationId: number) => void
  /** Navigate to `/multi-dashboard/capture/locations/:locationId`. */
  navigateToCaptureLocation: (
    locationId: number,
    options?: NavigateToCaptureLocationOptions
  ) => void
  /** Owner/USER operators may see Pause/Activate location capture chrome. */
  canManageLocationCapture: () => boolean
  onOverviewLoadError?: (message: string) => void
  onLocationsLoadError?: (message: string) => void
  onCreateDigitalGuestLinkError?: (message: string) => void
  onDigitalGuestLinkCreated?: (message: string) => void
  onLocationCaptureError?: (message: string) => void
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
  /** Per-row ⋯ action catalog with enablement. */
  getLocationRowActions: (
    locationId: number
  ) => readonly OperatorCaptureLocationRowAction[]
  openCreateDialog: (options?: { locationId?: number }) => void
  closeCreateDialog: () => void
  setCreateDialogLocationId: (locationId: number | null) => void
  createDigitalGuestLink: (
    input: CreateDigitalGuestLinkModuleInput
  ) => Promise<CreateDigitalGuestLinkModuleResult>
  openLocationPreview: (
    locationId: number
  ) => Promise<OpenGuestExperiencePreviewResult>
  closeGuestExperiencePreview: () => void
  closeGuestExperiencePreviewPicker: () => void
  selectGuestExperiencePreviewPickerOption: (
    qrCodeId: number | null
  ) => "selected" | "noop"
  confirmGuestExperiencePreviewPicker: () => "opened" | "noop"
  requestPauseLocationCapture: (locationId: number) => "opened" | "noop"
  requestActivateLocationCapture: (locationId: number) => "opened" | "noop"
  cancelLocationCaptureConfirm: () => void
  confirmLocationCapture: () => Promise<ConfirmLocationCaptureResult>
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
  locationItems: readonly CaptureLocationItem[]
  searchQuery: string
  sortId: CaptureLocationsSortId
  page: number
  appliedFilters: OperatorFilterSelection
  filtersSession: FilterSheetSession | null
  loadGeneration: number
  overviewLoadGeneration: number
  listLoadGeneration: number
  createDialogOpen: boolean
  createDialogLocationBound: boolean
  createDialogLocationId: number | null
  createDialogBusy: boolean
  previewableCountByLocationId: ReadonlyMap<number, number>
  previewFactsByLocationId: ReadonlyMap<
    number,
    readonly GuestExperiencePreviewPickerFact[]
  >
  previewPickerLocationId: number | null
  isGuestExperiencePreviewPickerOpen: boolean
  guestExperiencePreviewPickerSelectedQrCodeId: number | null
  isGuestExperiencePreviewOpen: boolean
  guestExperiencePreviewPlacementLabel: string | null
  guestExperiencePreviewLocationName: string
  guestExperiencePreviewLocationAddress: string
  locationCaptureConfirm: OperatorMultiCaptureLocationCaptureConfirmSnapshot
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

function closedGuestExperiencePreviewPicker(): GuestExperiencePreviewPickerSnapshot {
  return {
    isOpen: false,
    groups: [],
    selectedQrCodeId: null,
    selectedLabel: null,
    canConfirm: false,
  }
}

function closedLocationCaptureConfirm(): OperatorMultiCaptureLocationCaptureConfirmSnapshot {
  return {
    isOpen: false,
    busy: false,
    details: null,
  }
}

function placementLabelForPreview(
  fact: GuestExperiencePreviewPickerFact
): string {
  const view = buildGuestExperiencePreviewPicker({
    placements: [fact],
    selectedQrCodeId: fact.qrCodeId,
  })
  return view.selectedLabel ?? "QR placement"
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
    locationItems: [],
    searchQuery: "",
    sortId: OPERATOR_CAPTURE_LOCATION_DEFAULT_SORT_ID,
    page: 1,
    appliedFilters: emptySelection(FILTER_SCHEMA),
    filtersSession: null,
    loadGeneration: 0,
    overviewLoadGeneration: 0,
    listLoadGeneration: 0,
    createDialogOpen: false,
    createDialogLocationBound: false,
    createDialogLocationId: null,
    createDialogBusy: false,
    previewableCountByLocationId: new Map(),
    previewFactsByLocationId: new Map(),
    previewPickerLocationId: null,
    isGuestExperiencePreviewPickerOpen: false,
    guestExperiencePreviewPickerSelectedQrCodeId: null,
    isGuestExperiencePreviewOpen: false,
    guestExperiencePreviewPlacementLabel: null,
    guestExperiencePreviewLocationName: "",
    guestExperiencePreviewLocationAddress: "",
    locationCaptureConfirm: closedLocationCaptureConfirm(),
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
        locationItems: [],
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
        locationItems: [],
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
      locationItems: listSettled.response.items,
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
        locationItems: [],
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
      locationItems: listSettled.ok ? listSettled.response.items : [],
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
      locationItems: [],
      searchQuery: "",
      sortId: OPERATOR_CAPTURE_LOCATION_DEFAULT_SORT_ID,
      page: 1,
      appliedFilters: emptySelection(FILTER_SCHEMA),
      filtersSession: null,
      createDialogOpen: false,
      createDialogLocationBound: false,
      createDialogLocationId: null,
      createDialogBusy: false,
      previewableCountByLocationId: new Map(),
      previewFactsByLocationId: new Map(),
      previewPickerLocationId: null,
      isGuestExperiencePreviewPickerOpen: false,
      guestExperiencePreviewPickerSelectedQrCodeId: null,
      isGuestExperiencePreviewOpen: false,
      guestExperiencePreviewPlacementLabel: null,
      guestExperiencePreviewLocationName: "",
      guestExperiencePreviewLocationAddress: "",
      locationCaptureConfirm: closedLocationCaptureConfirm(),
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
    getLocationRowActions(locationId) {
      const item = state.locationItems.find(
        (row) => row.locationId === locationId
      )
      const status: CaptureLocationStatus = item?.status ?? "Active"
      const cachedPreviewable =
        state.previewableCountByLocationId.get(locationId)
      const previewEnabled =
        cachedPreviewable != null
          ? cachedPreviewable > 0
          : (item?.activePlacementsCount ?? 0) > 0
      const canManage = adapters.canManageLocationCapture()

      const actions: OperatorCaptureLocationRowAction[] = []
      for (const def of OPERATOR_CAPTURE_LOCATION_ROW_ACTION_DEFS) {
        if (def.id === "pause-location-capture") {
          if (!canManage || status !== "Active") {
            continue
          }
          actions.push({
            id: def.id,
            label: def.label,
            enabled: true,
          })
          continue
        }
        if (def.id === "activate-location-capture") {
          if (!canManage || status !== "Paused") {
            continue
          }
          actions.push({
            id: def.id,
            label: def.label,
            enabled: true,
          })
          continue
        }
        if (def.id === "preview-guest-experience") {
          actions.push({
            id: def.id,
            label: def.label,
            enabled: previewEnabled,
          })
          continue
        }
        if (def.id === "order-print-materials") {
          actions.push({
            id: def.id,
            label: def.label,
            enabled: false,
          })
          continue
        }
        actions.push({
          id: def.id,
          label: def.label,
          enabled: true,
        })
      }
      return actions
    },
    openCreateDialog(options) {
      const ownedCount = state.workspace?.locations.length ?? 0
      if (ownedCount === 0) {
        return
      }
      const boundId = options?.locationId
      if (boundId != null) {
        state = {
          ...state,
          createDialogOpen: true,
          createDialogLocationBound: true,
          createDialogLocationId: boundId,
          createDialogBusy: false,
        }
        publish()
        return
      }
      state = {
        ...state,
        createDialogOpen: true,
        createDialogLocationBound: false,
        createDialogLocationId: null,
        createDialogBusy: false,
      }
      publish()
    },
    closeCreateDialog() {
      if (!state.createDialogOpen || state.createDialogBusy) {
        return
      }
      state = {
        ...state,
        createDialogOpen: false,
        createDialogLocationBound: false,
        createDialogLocationId: null,
        createDialogBusy: false,
      }
      publish()
    },
    setCreateDialogLocationId(locationId) {
      if (!state.createDialogOpen || state.createDialogLocationBound) {
        return
      }
      state = {
        ...state,
        createDialogLocationId: locationId,
      }
      publish()
    },
    async createDigitalGuestLink(input) {
      const locationId =
        input.locationId ?? state.createDialogLocationId
      if (locationId == null || !state.createDialogOpen) {
        return "noop"
      }

      state = {
        ...state,
        createDialogBusy: true,
      }
      publish()

      let result: CreateDigitalGuestLinkAdapterResult
      try {
        result = await adapters.createDigitalGuestLink(locationId, {
          ...input,
          locationId,
        })
      } catch {
        state = {
          ...state,
          createDialogBusy: false,
        }
        publish()
        adapters.onCreateDigitalGuestLinkError?.(
          OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY.failureToast
        )
        return "failed"
      }

      if (!result.ok) {
        state = {
          ...state,
          createDialogBusy: false,
        }
        publish()
        if (result.reason === "duplicate_link_name") {
          return "duplicate_link_name"
        }
        adapters.onCreateDigitalGuestLinkError?.(result.message)
        return "failed"
      }

      state = {
        ...state,
        createDialogOpen: false,
        createDialogLocationBound: false,
        createDialogLocationId: null,
        createDialogBusy: false,
      }
      publish()

      adapters.onDigitalGuestLinkCreated?.(
        OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY.successToast
      )
      adapters.syncSelectedLocation(locationId)
      adapters.navigateToCaptureLocation(locationId, {
        openPlacementDetailQrCodeId: result.qrCodeId,
      })
      return "created"
    },
    async openLocationPreview(locationId) {
      const workspaceLocation = state.workspace?.locations.find(
        (location) => location.id === locationId
      )
      if (workspaceLocation == null) {
        return "noop"
      }

      const overviewWindow = resolveHomePerformanceWindow(
        adapters.getMultiCaptureOverviewDateRange()
      )
      const from = overviewWindow.from.toISOString()
      const to = overviewWindow.to.toISOString()

      let placements: CapturePlacementsResponse
      try {
        placements = await adapters.getCapturePlacements(
          locationId,
          from,
          to
        )
      } catch {
        return "noop"
      }

      const facts: GuestExperiencePreviewPickerFact[] =
        placements.placements.map((item) => ({
          qrCodeId: item.qrCodeId,
          qrType: item.qrType,
          status: item.status,
          linkName: item.linkName,
        }))
      const previewable = facts.filter(
        (item) => item.status === "Active" || item.status === "Paused"
      )
      const nextPreviewableCounts = new Map(state.previewableCountByLocationId)
      nextPreviewableCounts.set(locationId, previewable.length)
      const nextPreviewFacts = new Map(state.previewFactsByLocationId)
      nextPreviewFacts.set(locationId, facts)

      if (previewable.length === 0) {
        state = {
          ...state,
          previewableCountByLocationId: nextPreviewableCounts,
          previewFactsByLocationId: nextPreviewFacts,
        }
        publish()
        return "noop"
      }

      const locationName = workspaceLocation.locationName
      const locationAddress = workspaceLocation.address ?? ""

      if (previewable.length === 1) {
        const only = previewable[0]!
        state = {
          ...state,
          previewableCountByLocationId: nextPreviewableCounts,
          previewFactsByLocationId: nextPreviewFacts,
          isGuestExperiencePreviewOpen: true,
          isGuestExperiencePreviewPickerOpen: false,
          previewPickerLocationId: null,
          guestExperiencePreviewPickerSelectedQrCodeId: null,
          guestExperiencePreviewPlacementLabel:
            placementLabelForPreview(only),
          guestExperiencePreviewLocationName: locationName,
          guestExperiencePreviewLocationAddress: locationAddress,
        }
        publish()
        return "opened"
      }

      state = {
        ...state,
        previewableCountByLocationId: nextPreviewableCounts,
        previewFactsByLocationId: nextPreviewFacts,
        isGuestExperiencePreviewPickerOpen: true,
        isGuestExperiencePreviewOpen: false,
        previewPickerLocationId: locationId,
        guestExperiencePreviewPickerSelectedQrCodeId: null,
        guestExperiencePreviewPlacementLabel: null,
        guestExperiencePreviewLocationName: locationName,
        guestExperiencePreviewLocationAddress: locationAddress,
      }
      publish()
      return "picker"
    },
    closeGuestExperiencePreview() {
      if (!state.isGuestExperiencePreviewOpen) {
        return
      }
      state = {
        ...state,
        isGuestExperiencePreviewOpen: false,
        guestExperiencePreviewPlacementLabel: null,
      }
      publish()
    },
    closeGuestExperiencePreviewPicker() {
      if (!state.isGuestExperiencePreviewPickerOpen) {
        return
      }
      state = {
        ...state,
        isGuestExperiencePreviewPickerOpen: false,
        previewPickerLocationId: null,
        guestExperiencePreviewPickerSelectedQrCodeId: null,
      }
      publish()
    },
    selectGuestExperiencePreviewPickerOption(qrCodeId) {
      if (!state.isGuestExperiencePreviewPickerOpen) {
        return "noop"
      }
      const locationId = state.previewPickerLocationId
      if (locationId == null) {
        return "noop"
      }
      const facts = state.previewFactsByLocationId.get(locationId)
      if (facts == null) {
        return "noop"
      }
      const picker = buildGuestExperiencePreviewPicker({
        placements: facts,
        selectedQrCodeId: qrCodeId,
      })
      if (
        qrCodeId != null
        && !picker.groups.some((group) =>
          group.options.some((option) => option.qrCodeId === qrCodeId)
        )
      ) {
        return "noop"
      }
      state = {
        ...state,
        guestExperiencePreviewPickerSelectedQrCodeId: qrCodeId,
      }
      publish()
      return "selected"
    },
    confirmGuestExperiencePreviewPicker() {
      if (!state.isGuestExperiencePreviewPickerOpen) {
        return "noop"
      }
      const locationId = state.previewPickerLocationId
      if (locationId == null) {
        return "noop"
      }
      const facts = state.previewFactsByLocationId.get(locationId)
      if (facts == null) {
        return "noop"
      }
      const picker = buildGuestExperiencePreviewPicker({
        placements: facts,
        selectedQrCodeId: state.guestExperiencePreviewPickerSelectedQrCodeId,
      })
      if (!picker.canConfirm || picker.selectedLabel == null) {
        return "noop"
      }
      state = {
        ...state,
        isGuestExperiencePreviewOpen: true,
        isGuestExperiencePreviewPickerOpen: false,
        previewPickerLocationId: null,
        guestExperiencePreviewPickerSelectedQrCodeId: null,
        guestExperiencePreviewPlacementLabel: picker.selectedLabel,
      }
      publish()
      return "opened"
    },
    requestPauseLocationCapture(locationId) {
      if (!adapters.canManageLocationCapture()) {
        return "noop"
      }
      const item = state.locationItems.find(
        (row) => row.locationId === locationId
      )
      if (item == null || item.status !== "Active") {
        return "noop"
      }
      state = {
        ...state,
        locationCaptureConfirm: {
          isOpen: true,
          busy: false,
          details: buildLocationCaptureConfirm({
            locationId,
            locationName: item.locationName,
            action: "pause",
            codesCount: item.activePlacementsCount,
          }),
        },
      }
      publish()
      return "opened"
    },
    requestActivateLocationCapture(locationId) {
      if (!adapters.canManageLocationCapture()) {
        return "noop"
      }
      const item = state.locationItems.find(
        (row) => row.locationId === locationId
      )
      if (item == null || item.status !== "Paused") {
        return "noop"
      }
      state = {
        ...state,
        locationCaptureConfirm: {
          isOpen: true,
          busy: false,
          details: buildLocationCaptureConfirm({
            locationId,
            locationName: item.locationName,
            action: "activate",
            codesCount: item.pauseRestoreQrCodeCount,
          }),
        },
      }
      publish()
      return "opened"
    },
    cancelLocationCaptureConfirm() {
      if (!state.locationCaptureConfirm.isOpen) {
        return
      }
      if (state.locationCaptureConfirm.busy) {
        return
      }
      state = {
        ...state,
        locationCaptureConfirm: closedLocationCaptureConfirm(),
      }
      publish()
    },
    async confirmLocationCapture() {
      const confirm = state.locationCaptureConfirm
      const details = confirm.details
      if (!confirm.isOpen || details == null || confirm.busy) {
        return "noop"
      }

      state = {
        ...state,
        locationCaptureConfirm: {
          ...confirm,
          busy: true,
        },
      }
      publish()

      const toastMessage = details.successToastMessage
      const action = details.action

      try {
        const result =
          action === "pause"
            ? await adapters.pauseLocationCapture(details.locationId)
            : await adapters.activateLocationCapture(details.locationId)

        if (!result.ok) {
          state = {
            ...state,
            locationCaptureConfirm: {
              ...state.locationCaptureConfirm,
              busy: false,
            },
          }
          publish()
          adapters.onLocationCaptureError?.(result.message)
          return "failed"
        }

        state = {
          ...state,
          locationCaptureConfirm: closedLocationCaptureConfirm(),
        }
        publish()

        if (state.workspace != null) {
          await fetchOverviewAndList({
            workspace: state.workspace,
            isInitialLoad: false,
          })
        }

        return {
          outcome: action === "pause" ? "paused" : "activated",
          toastMessage,
        }
      } catch {
        state = {
          ...state,
          locationCaptureConfirm: {
            ...state.locationCaptureConfirm,
            busy: false,
          },
        }
        publish()
        adapters.onLocationCaptureError?.(
          action === "pause"
            ? "Could not pause location capture. Please try again."
            : "Could not activate location capture. Please try again."
        )
        return "failed"
      }
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

  let guestExperiencePreviewPicker: GuestExperiencePreviewPickerSnapshot =
    closedGuestExperiencePreviewPicker()
  if (
    state.isGuestExperiencePreviewPickerOpen
    && state.previewPickerLocationId != null
  ) {
    const facts = state.previewFactsByLocationId.get(
      state.previewPickerLocationId
    )
    if (facts != null) {
      guestExperiencePreviewPicker = {
        isOpen: true,
        ...buildGuestExperiencePreviewPicker({
          placements: facts,
          selectedQrCodeId:
            state.guestExperiencePreviewPickerSelectedQrCodeId,
        }),
      }
    }
  }

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
    canCreateDigitalGuestLink: (state.workspace?.locations.length ?? 0) > 0,
    createDialog: {
      isOpen: state.createDialogOpen,
      locationBound: state.createDialogLocationBound,
      selectedLocationId: state.createDialogLocationId,
      busy: state.createDialogBusy,
    },
    guestExperiencePreviewPicker,
    guestExperiencePreview: {
      isOpen: state.isGuestExperiencePreviewOpen,
      placementLabel: state.guestExperiencePreviewPlacementLabel,
      locationName: state.guestExperiencePreviewLocationName,
      locationAddress: state.guestExperiencePreviewLocationAddress,
    },
    locationCaptureConfirm: state.locationCaptureConfirm,
  }
}

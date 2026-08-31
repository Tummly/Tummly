import {
  chipCount,
  commitPending,
  emptySelection,
  openSession,
  projectChips,
  removeAppliedChip,
  type FilterChip,
  type FilterSheetSession,
} from "@/lib/operatorFilterSheet"
import { locationsFilterSheetSchema } from "@/lib/operatorLocations/locationsFilterSheetSchema"
import {
  buildLocationsListQueryParams,
  type LocationsActivityApiItem,
  type LocationsListApiRow,
  type LocationsListResponse,
} from "@/lib/operatorLocations/locationsListQueryParams"
import {
  buildLocationsKpis,
  formatLocationsLastActivityAt,
  formatLocationsPageRange,
  locationRowActionsForLifecycle,
  LOCATIONS_DEFAULT_SORT_ID,
  LOCATIONS_PAGE_SIZE,
  LOCATIONS_SORT_LABELS,
  LOCATIONS_TAB_IDS,
  LOCATIONS_TAB_LABELS,
  resolveLocationsTabId,
  type LocationRowAction,
  type LocationRowActionId,
  type LocationsActivityItem,
  type LocationsKpi,
  type LocationsSetupAttentionItem,
  type LocationsSetupAttentionItemId,
  type LocationsSortId,
  type LocationsTabId,
} from "@/lib/operatorLocations/locationsPresentation"

export type LocationsTableRow = {
  id: string
  name: string
  lifecycleStatus: LocationsListApiRow["lifecycleStatus"]
  setupStatus: LocationsListApiRow["setupStatus"]
  managerName: string
  managerUserId: number | null
  cityPostcode: string
  cityId: string
  lastActivityLabel: string
  searchText: string
}

export type LocationsSnapshot = {
  activeTabId: LocationsTabId
  tabs: Array<{
    id: LocationsTabId
    label: string
    count: number | null
  }>
  kpis: LocationsKpi[]
  searchQuery: string
  sortId: LocationsSortId
  sortLabel: string
  filterChips: FilterChip[]
  filterChipCount: number
  filtersOpen: boolean
  filtersSession: FilterSheetSession | null
  cityFilterOptions: Array<{ id: string; label: string }>
  rows: LocationsTableRow[]
  rowActionsById: Record<string, LocationRowAction[]>
  setupAttentionItems: LocationsSetupAttentionItem[]
  activityItems: LocationsActivityItem[]
  page: number
  pageSize: number
  totalFilteredCount: number
  pageRangeLabel: string
  canGoPrevious: boolean
  canGoNext: boolean
  empty: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
}

export type OperatorLocationsPageAdapters = {
  getList: (params: ReturnType<typeof buildLocationsListQueryParams>) => Promise<LocationsListResponse>
  getActivity: () => Promise<{ items: LocationsActivityApiItem[] }>
  createDraft?: (input: {
    locationName: string
    address: string
    city: string
    postcode: string
  }) => Promise<void>
  importDrafts?: (
    rows: Array<{
      locationName: string
      address: string
      city: string
      postcode: string
      locationPhone?: string
      localContact?: string
    }>
  ) => Promise<{
    createdCount: number
    errors: Array<{ rowIndex: number; message: string }>
  }>
  activateDraft?: (locationId: string) => Promise<void>
  deleteDraft?: (locationId: string) => Promise<void>
  setManager?: (
    locationId: string,
    managerUserId: number | null
  ) => Promise<void>
  mutateLifecycle?: (
    locationId: number,
    action: "pause" | "resume" | "archive" | "restore"
  ) => Promise<void>
  debounceMs?: number
  getNow?: () => Date
}

export type OperatorLocationsPageModule = {
  getSnapshot: () => LocationsSnapshot
  subscribe: (listener: () => void) => () => void
  load: () => Promise<void>
  setActiveTabFromUrl: (raw: string | null | undefined) => void
  requestTabChange: (tabId: LocationsTabId) => void
  setSearchQuery: (query: string) => void
  setSortId: (sortId: LocationsSortId) => void
  setFiltersSession: (session: FilterSheetSession | null) => void
  setFiltersOpen: (open: boolean) => void
  openFilters: () => void
  applyFilters: () => void
  removeFilterChip: (chip: FilterChip) => void
  clearSearchAndFilters: () => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  createDraft: (input: {
    locationName: string
    address: string
    city: string
    postcode: string
  }) => Promise<void>
  importDrafts: (
    rows: Array<{
      locationName: string
      address: string
      city: string
      postcode: string
      locationPhone?: string
      localContact?: string
    }>
  ) => Promise<{
    createdCount: number
    errors: Array<{ rowIndex: number; message: string }>
  }>
  activateDraft: (locationId: string) => Promise<void>
  deleteDraft: (locationId: string) => Promise<void>
  setManager: (
    locationId: string,
    managerUserId: number | null
  ) => Promise<void>
  onRowAction: (locationId: string, actionId: LocationRowActionId) => void | Promise<void>
  onReviewSetupAttention: (itemId: LocationsSetupAttentionItemId) => void
}

const DEFAULT_SEARCH_DEBOUNCE_MS = 300

/** UI seed — Figma Needs attention card until readiness API lands (ticket 02). */
const DEMO_SETUP_ATTENTION_ITEMS: LocationsSetupAttentionItem[] = [
  {
    id: "privacy-review",
    message: "1 location needs a privacy review",
  },
  {
    id: "no-active-qr",
    message: "1 location has no active QR placement",
  },
]

function mapApiRowToTableRow(
  row: LocationsListApiRow,
  now: Date
): LocationsTableRow {
  return {
    id: String(row.id),
    name: row.name,
    lifecycleStatus: row.lifecycleStatus,
    setupStatus: row.setupStatus,
    managerName: row.managerName?.trim() ? row.managerName : "—",
    managerUserId: row.managerUserId ?? null,
    cityPostcode: row.cityPostcode || "—",
    cityId: row.cityId ?? "",
    lastActivityLabel: formatLocationsLastActivityAt(row.lastActivityAt, now),
    searchText: (row.searchText ?? "").toLowerCase(),
  }
}

function mapActivityFeedItem(
  item: LocationsActivityApiItem,
  now: Date
): LocationsActivityItem {
  return {
    id: String(item.id),
    timeLabel: formatLocationsLastActivityAt(item.occurredAt, now),
    description: item.description?.trim() ? item.description : "—",
  }
}

export function createOperatorLocationsPageModule(
  adapters: OperatorLocationsPageAdapters,
  options: {
    initialTabId?: string | null
  } = {}
): OperatorLocationsPageModule {
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS
  const getNow = adapters.getNow ?? (() => new Date())

  let activeTabId = resolveLocationsTabId(options.initialTabId)
  let searchQuery = ""
  let sortId: LocationsSortId = LOCATIONS_DEFAULT_SORT_ID
  let filtersSession: FilterSheetSession | null = null
  let filtersOpen = false
  let page = 1
  let rows: LocationsTableRow[] = []
  let totalFilteredCount = 0
  let cityFilterOptions: Array<{ id: string; label: string }> = []
  let kpis = buildLocationsKpis({
    active: 0,
    draft: 0,
    paused: 0,
    setupNeedsAttention: 0,
  })
  let setupNeedsAttentionCount = 0
  let loadStatus: LocationsSnapshot["loadStatus"] = "idle"
  let loadGeneration = 0
  let searchTimer: ReturnType<typeof setTimeout> | null = null

  // Setup tab stays on Figma seeds until ticket 02.
  const setupAttentionItems = DEMO_SETUP_ATTENTION_ITEMS
  let activityItems: LocationsActivityItem[] = []

  const listeners = new Set<() => void>()
  let snapshot: LocationsSnapshot

  const schema = () =>
    locationsFilterSheetSchema({
      cities: cityFilterOptions,
    })

  const emit = () => {
    snapshot = projectSnapshot()
    for (const listener of listeners) {
      listener()
    }
  }

  const projectSnapshot = (): LocationsSnapshot => {
    const pageSize = LOCATIONS_PAGE_SIZE
    const maxPage = Math.max(1, Math.ceil(totalFilteredCount / pageSize))
    const safePage = Math.min(page, maxPage)

    const rowActionsById: Record<string, LocationRowAction[]> = {}
    for (const row of rows) {
      rowActionsById[row.id] = locationRowActionsForLifecycle(
        row.lifecycleStatus
      )
    }

    return {
      activeTabId,
      tabs: LOCATIONS_TAB_IDS.map((id) => ({
        id,
        label: LOCATIONS_TAB_LABELS[id],
        count:
          id === "setup-readiness" && setupNeedsAttentionCount > 0
            ? setupNeedsAttentionCount
            : null,
      })),
      kpis,
      searchQuery,
      sortId,
      sortLabel: LOCATIONS_SORT_LABELS[sortId],
      filterChips:
        filtersSession == null
          ? []
          : projectChips(schema(), filtersSession.applied),
      filterChipCount:
        filtersSession == null
          ? 0
          : chipCount(schema(), filtersSession.applied),
      filtersOpen,
      filtersSession,
      cityFilterOptions,
      rows,
      rowActionsById,
      setupAttentionItems,
      activityItems,
      page: safePage,
      pageSize,
      totalFilteredCount,
      pageRangeLabel: formatLocationsPageRange({
        page: safePage,
        pageSize,
        totalCount: totalFilteredCount,
      }),
      canGoPrevious: safePage > 1,
      canGoNext: safePage * pageSize < totalFilteredCount,
      empty: totalFilteredCount === 0 && loadStatus !== "loading",
      loadStatus,
    }
  }

  snapshot = projectSnapshot()

  const applyListResponse = (response: LocationsListResponse, now: Date) => {
    rows = response.rows.map((row) => mapApiRowToTableRow(row, now))
    totalFilteredCount = response.totalCount
    page = response.page
    cityFilterOptions = response.cityFacets.map((facet) => ({
      id: facet.id,
      label: facet.label,
    }))
    kpis = buildLocationsKpis(response.kpis)
    setupNeedsAttentionCount = response.kpis.setupNeedsAttention
  }

  const applyActivityResponse = (
    response: Awaited<ReturnType<OperatorLocationsPageAdapters["getActivity"]>>,
    now: Date
  ) => {
    activityItems = response.items.map((item) =>
      mapActivityFeedItem(item, now)
    )
  }

  const fetchList = async () => {
    const generation = ++loadGeneration
    if (loadStatus === "idle" || loadStatus === "error") {
      loadStatus = "loading"
      emit()
    }

    try {
      const response = await adapters.getList(
        buildLocationsListQueryParams({
          searchQuery,
          sortId,
          page,
          pageSize: LOCATIONS_PAGE_SIZE,
          applied: filtersSession?.applied ?? null,
        })
      )

      if (generation !== loadGeneration) {
        return
      }

      applyListResponse(response, getNow())
      loadStatus = "loaded"
      emit()
    } catch {
      if (generation !== loadGeneration) {
        return
      }
      loadStatus = "error"
      emit()
    }
  }

  const fetchListAndActivity = async () => {
    const generation = ++loadGeneration
    if (loadStatus === "idle" || loadStatus === "error") {
      loadStatus = "loading"
      emit()
    }

    try {
      const listParams = buildLocationsListQueryParams({
        searchQuery,
        sortId,
        page,
        pageSize: LOCATIONS_PAGE_SIZE,
        applied: filtersSession?.applied ?? null,
      })
      const [listResponse, activityResponse] = await Promise.all([
        adapters.getList(listParams),
        adapters.getActivity(),
      ])

      if (generation !== loadGeneration) {
        return
      }

      const now = getNow()
      applyListResponse(listResponse, now)
      applyActivityResponse(activityResponse, now)
      loadStatus = "loaded"
      emit()
    } catch {
      if (generation !== loadGeneration) {
        return
      }
      loadStatus = "error"
      emit()
    }
  }

  const scheduleSearchFetch = () => {
    if (searchTimer != null) {
      clearTimeout(searchTimer)
    }
    searchTimer = setTimeout(() => {
      searchTimer = null
      void fetchList()
    }, debounceMs)
  }

  return {
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot: () => snapshot,
    load: async () => {
      await fetchListAndActivity()
    },
    setActiveTabFromUrl: (raw) => {
      activeTabId = resolveLocationsTabId(raw)
      emit()
    },
    requestTabChange: (tabId) => {
      if (tabId === activeTabId) {
        return
      }
      activeTabId = tabId
      emit()
    },
    setSearchQuery: (query) => {
      searchQuery = query
      page = 1
      emit()
      scheduleSearchFetch()
    },
    setSortId: (next) => {
      sortId = next
      page = 1
      emit()
      void fetchList()
    },
    setFiltersSession: (session) => {
      filtersSession = session
      emit()
    },
    setFiltersOpen: (open) => {
      filtersOpen = open
      if (!open) {
        emit()
        return
      }
      filtersSession =
        filtersSession ?? openSession(emptySelection(schema()))
      emit()
    },
    openFilters: () => {
      filtersSession =
        filtersSession ?? openSession(emptySelection(schema()))
      filtersOpen = true
      emit()
    },
    applyFilters: () => {
      if (filtersSession == null) {
        return
      }
      filtersSession = commitPending(filtersSession)
      filtersOpen = false
      page = 1
      emit()
      void fetchList()
    },
    removeFilterChip: (chip) => {
      if (filtersSession == null) {
        return
      }
      filtersSession = openSession(
        removeAppliedChip(schema(), filtersSession.applied, chip)
      )
      page = 1
      emit()
      void fetchList()
    },
    clearSearchAndFilters: () => {
      searchQuery = ""
      filtersSession = null
      page = 1
      emit()
      void fetchList()
    },
    goToPreviousPage: () => {
      if (page <= 1) {
        return
      }
      page -= 1
      emit()
      void fetchList()
    },
    goToNextPage: () => {
      const current = projectSnapshot()
      if (!current.canGoNext) {
        return
      }
      page += 1
      emit()
      void fetchList()
    },
    createDraft: async (input) => {
      if (adapters.createDraft == null) {
        throw new Error("Create draft is not configured.")
      }
      await adapters.createDraft(input)
      page = 1
      await fetchListAndActivity()
    },
    importDrafts: async (rows) => {
      if (adapters.importDrafts == null) {
        throw new Error("Import drafts is not configured.")
      }
      const result = await adapters.importDrafts(rows)
      page = 1
      await fetchListAndActivity()
      return result
    },
    activateDraft: async (locationId) => {
      if (adapters.activateDraft == null) {
        throw new Error("Activate draft is not configured.")
      }
      await adapters.activateDraft(locationId)
      await fetchListAndActivity()
    },
    deleteDraft: async (locationId) => {
      if (adapters.deleteDraft == null) {
        throw new Error("Delete draft is not configured.")
      }
      await adapters.deleteDraft(locationId)
      await fetchListAndActivity()
    },
    setManager: async (locationId, managerUserId) => {
      if (adapters.setManager == null) {
        throw new Error("Set manager is not configured.")
      }
      await adapters.setManager(locationId, managerUserId)
      await fetchListAndActivity()
    },
    onRowAction: (locationId, actionId) => {
      const mutate = adapters.mutateLifecycle
      if (mutate == null) {
        return
      }

      const lifecycleAction =
        actionId === "pause-location"
          ? "pause"
          : actionId === "resume-location"
            ? "resume"
            : actionId === "archive-location"
              ? "archive"
              : actionId === "restore-location"
                ? "restore"
                : null

      if (lifecycleAction == null) {
        // Activate / delete / manager use dedicated methods + confirm chrome.
        return
      }

      const id = Number.parseInt(locationId, 10)
      if (!Number.isFinite(id)) {
        return
      }

      void (async () => {
        try {
          await mutate(id, lifecycleAction)
          await fetchListAndActivity()
        } catch {
          loadStatus = "error"
          emit()
        }
      })()
    },
    onReviewSetupAttention: () => {
      // Wire review navigation when Setup attention API lands.
    },
  }
}

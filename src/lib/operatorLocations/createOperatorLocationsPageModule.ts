import {
  chipCount,
  commitPending,
  emptySelection,
  openSession,
  projectChips,
  removeAppliedChip,
  type FilterChip,
  type FilterSheetSession,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import { locationsFilterSheetSchema } from "@/lib/operatorLocations/locationsFilterSheetSchema"
import {
  buildLocationsKpis,
  formatLocationsPageRange,
  locationRowActionsForLifecycle,
  LOCATIONS_DEFAULT_SORT_ID,
  LOCATIONS_PAGE_SIZE,
  LOCATIONS_SORT_LABELS,
  LOCATIONS_TAB_IDS,
  LOCATIONS_TAB_LABELS,
  resolveLocationsTabId,
  type LocationLifecycleStatus,
  type LocationRowAction,
  type LocationRowActionId,
  type LocationSetupStatus,
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
  lifecycleStatus: LocationLifecycleStatus
  setupStatus: LocationSetupStatus
  managerName: string
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
}

export type OperatorLocationsPageModule = {
  getSnapshot: () => LocationsSnapshot
  subscribe: (listener: () => void) => () => void
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
  onRowAction: (locationId: string, actionId: LocationRowActionId) => void
  onReviewSetupAttention: (itemId: LocationsSetupAttentionItemId) => void
}

/** UI seed rows — Figma Locations table until the list API lands. */
const DEMO_ROWS: LocationsTableRow[] = [
  {
    id: "1",
    name: "Mehmet’s Grill — Camden",
    lifecycleStatus: "active",
    setupStatus: "ready",
    managerName: "Aisha Khan",
    cityPostcode: "Camden, London",
    cityId: "camden",
    lastActivityLabel: "Today, 13:42",
    searchText: "mehmet's grill camden london nw1",
  },
  {
    id: "2",
    name: "Mehmet’s Grill — Soho",
    lifecycleStatus: "draft",
    setupStatus: "ready",
    managerName: "Aisha Khan",
    cityPostcode: "Camden, London",
    cityId: "camden",
    lastActivityLabel: "Today, 13:42",
    searchText: "mehmet's grill soho camden london",
  },
  {
    id: "3",
    name: "Mehmet’s Grill — King’s Cross",
    lifecycleStatus: "paused",
    setupStatus: "ready",
    managerName: "Aisha Khan",
    cityPostcode: "Camden, London",
    cityId: "camden",
    lastActivityLabel: "Today, 13:42",
    searchText: "mehmet's grill king's cross camden london",
  },
  {
    id: "4",
    name: "Mehmet’s Grill — Greenwich",
    lifecycleStatus: "archived",
    setupStatus: "ready",
    managerName: "Aisha Khan",
    cityPostcode: "Camden, London",
    cityId: "camden",
    lastActivityLabel: "Today, 13:42",
    searchText: "mehmet's grill greenwich camden london",
  },
  {
    id: "5",
    name: "Mehmet’s Grill — Shoreditch",
    lifecycleStatus: "active",
    setupStatus: "ready",
    managerName: "Aisha Khan",
    cityPostcode: "Camden, London",
    cityId: "camden",
    lastActivityLabel: "Today, 13:42",
    searchText: "mehmet's grill shoreditch camden london",
  },
]

/** UI seed — Figma Needs attention card until readiness API lands. */
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

/** UI seed — Figma Activity card until activity API lands. */
const DEMO_ACTIVITY_ITEMS: LocationsActivityItem[] = [
  {
    id: "1",
    timeLabel: "Today, 10:42",
    description: "James updated SMS consent wording.",
  },
  {
    id: "2",
    timeLabel: "Yesterday, 16:05",
    description: "A guest unsubscribed from email campaigns.",
  },
  {
    id: "3",
    timeLabel: "Today, 10:42",
    description: "James updated SMS consent wording.",
  },
  {
    id: "4",
    timeLabel: "Yesterday, 16:05",
    description: "A guest unsubscribed from email campaigns.",
  },
  {
    id: "5",
    timeLabel: "Today, 10:42",
    description: "James updated SMS consent wording.",
  },
  {
    id: "6",
    timeLabel: "Yesterday, 16:05",
    description: "A guest unsubscribed from email campaigns.",
  },
  {
    id: "7",
    timeLabel: "Today, 10:42",
    description: "James updated SMS consent wording.",
  },
  {
    id: "8",
    timeLabel: "Yesterday, 16:05",
    description: "A guest unsubscribed from email campaigns.",
  },
]

function multiSelectIds(
  selection: OperatorFilterSelection | undefined,
  fieldId: string
): string[] {
  const value = selection?.[fieldId]
  if (value == null || value.kind !== "multi-select") {
    return []
  }
  return value.ids
}

function rowMatchesFilters(
  row: LocationsTableRow,
  selection: OperatorFilterSelection | undefined
): boolean {
  if (selection == null) {
    return true
  }

  const lifecycleIds = multiSelectIds(selection, "lifecycle")
  if (
    lifecycleIds.length > 0 &&
    !lifecycleIds.includes(row.lifecycleStatus)
  ) {
    return false
  }

  const setupIds = multiSelectIds(selection, "setup")
  if (setupIds.length > 0 && !setupIds.includes(row.setupStatus)) {
    return false
  }

  const cityIds = multiSelectIds(selection, "city")
  if (cityIds.length > 0 && !cityIds.includes(row.cityId)) {
    return false
  }

  return true
}

function sortRows(
  rows: LocationsTableRow[],
  sortId: LocationsSortId
): LocationsTableRow[] {
  const next = [...rows]
  next.sort((a, b) => {
    const cmp = a.name.localeCompare(b.name, undefined, {
      sensitivity: "base",
    })
    return sortId === "name-asc" ? cmp : -cmp
  })
  return next
}

function cityOptionsFromRows(rows: LocationsTableRow[]) {
  const seen = new Map<string, string>()
  for (const row of rows) {
    if (!seen.has(row.cityId)) {
      const cityLabel = row.cityPostcode.split(",")[0]?.trim() || row.cityId
      seen.set(row.cityId, cityLabel)
    }
  }
  return [...seen.entries()].map(([id, label]) => ({ id, label }))
}

export function createOperatorLocationsPageModule(options: {
  initialTabId?: string | null
  rows?: LocationsTableRow[]
  /** Setup & readiness tab badge until readiness API lands. */
  setupNeedsAttentionCount?: number
  setupAttentionItems?: LocationsSetupAttentionItem[]
  activityItems?: LocationsActivityItem[]
} = {}): OperatorLocationsPageModule {
  const allRows = options.rows ?? DEMO_ROWS
  const setupAttentionItems =
    options.setupAttentionItems ?? DEMO_SETUP_ATTENTION_ITEMS
  const activityItems = options.activityItems ?? DEMO_ACTIVITY_ITEMS
  const setupNeedsAttentionCount =
    options.setupNeedsAttentionCount ??
    (setupAttentionItems.length > 0
      ? setupAttentionItems.length
      : allRows.filter((row) => row.setupStatus === "needs-attention").length)

  let activeTabId = resolveLocationsTabId(options.initialTabId)
  let searchQuery = ""
  let sortId: LocationsSortId = LOCATIONS_DEFAULT_SORT_ID
  let filtersSession: FilterSheetSession | null = null
  let filtersOpen = false
  let page = 1
  const listeners = new Set<() => void>()
  let snapshot: LocationsSnapshot

  const schema = () =>
    locationsFilterSheetSchema({
      cities: cityOptionsFromRows(allRows),
    })

  const emit = () => {
    snapshot = projectSnapshot()
    for (const listener of listeners) {
      listener()
    }
  }

  const projectSnapshot = (): LocationsSnapshot => {
    const applied = filtersSession?.applied
    const query = searchQuery.trim().toLowerCase()
    const filtered = sortRows(
      allRows.filter((row) => {
        if (!rowMatchesFilters(row, applied)) {
          return false
        }
        if (query === "") {
          return true
        }
        return (
          row.name.toLowerCase().includes(query) ||
          row.cityPostcode.toLowerCase().includes(query) ||
          row.searchText.includes(query)
        )
      }),
      sortId
    )

    const totalFilteredCount = filtered.length
    const pageSize = LOCATIONS_PAGE_SIZE
    const maxPage = Math.max(1, Math.ceil(totalFilteredCount / pageSize))
    const safePage = Math.min(page, maxPage)
    const start = (safePage - 1) * pageSize
    const rows = filtered.slice(start, start + pageSize)

    const rowActionsById: Record<string, LocationRowAction[]> = {}
    for (const row of rows) {
      rowActionsById[row.id] = locationRowActionsForLifecycle(
        row.lifecycleStatus
      )
    }

    const activeCount = allRows.filter(
      (row) => row.lifecycleStatus === "active"
    ).length
    const draftCount = allRows.filter(
      (row) => row.lifecycleStatus === "draft"
    ).length
    const pausedCount = allRows.filter(
      (row) => row.lifecycleStatus === "paused"
    ).length

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
      kpis: buildLocationsKpis({
        active: activeCount,
        draft: draftCount,
        paused: pausedCount,
        setupNeedsAttention: setupNeedsAttentionCount,
      }),
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
      cityFilterOptions: cityOptionsFromRows(allRows),
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
      empty: totalFilteredCount === 0,
    }
  }

  snapshot = projectSnapshot()

  return {
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot: () => snapshot,
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
    },
    setSortId: (next) => {
      sortId = next
      page = 1
      emit()
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
    },
    clearSearchAndFilters: () => {
      searchQuery = ""
      filtersSession = null
      page = 1
      emit()
    },
    goToPreviousPage: () => {
      if (page <= 1) {
        return
      }
      page -= 1
      emit()
    },
    goToNextPage: () => {
      const current = projectSnapshot()
      if (!current.canGoNext) {
        return
      }
      page += 1
      emit()
    },
      onRowAction: () => {
        // Wire mutations when Locations API lands.
      },
      onReviewSetupAttention: () => {
        // Wire review navigation when Locations API lands.
      },
    }
  }

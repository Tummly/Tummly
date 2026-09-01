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
import type { SavePrivacyConsentInput } from "@/api/privacyConsentApi"
import {
  mapGuestPermissionCardsFromApi,
  mapPermissionRecordRowFromApi,
  mapPrivacyActivityItemFromApi,
  mapPrivacySetupRowsFromApi,
  patchPayloadForGuestPermission,
  type PrivacyConsentPageApiData,
  type PrivacyConsentActivityApiItem,
} from "@/lib/operatorPrivacyConsent/mapPrivacyConsentApiResponse"
import { permissionRecordsFilterSheetSchema } from "@/lib/operatorPrivacyConsent/permissionRecordsFilterSheetSchema"
import {
  buildPermissionRecordsListQueryParams,
  PERMISSION_RECORDS_PAGE_SIZE,
  type PermissionRecordsListQueryParams,
  type PermissionRecordsListResponse,
} from "@/lib/operatorPrivacyConsent/permissionRecordsListQueryParams"
import {
  GUEST_PERMISSIONS_DEMO_CARDS,
  PERMISSION_RECORDS_DEMO_ROWS,
  PRIVACY_ACTIVITY_DEMO_ITEMS,
  PRIVACY_CONSENT_TAB_IDS,
  PRIVACY_CONSENT_TAB_LABELS,
  PRIVACY_SETUP_STATUS_DEMO_ROWS,
  resolvePrivacyConsentTabId,
  type GuestPermissionCard,
  type GuestPermissionId,
  type PermissionRecordRow,
  type PrivacyActivityItem,
  type PrivacyConsentTabId,
  type PrivacySetupStatusRow,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"

export type PrivacyConsentSnapshot = {
  activeTabId: PrivacyConsentTabId
  tabs: Array<{
    id: PrivacyConsentTabId
    label: string
  }>
  loadStatus: "idle" | "loading" | "loaded" | "error"
  actorCanManage: boolean
  canViewGuests: boolean
  privacySetupRows: PrivacySetupStatusRow[]
  guestPermissions: GuestPermissionCard[]
  permissionRecordsSearchQuery: string
  permissionRecordsFilterChips: FilterChip[]
  permissionRecordsFilterChipCount: number
  permissionRecordsFiltersOpen: boolean
  permissionRecordsFiltersSession: FilterSheetSession | null
  permissionRecordsLocationOptions: Array<{ id: string; label: string }>
  permissionRecordsRows: PermissionRecordRow[]
  permissionRecordsEmpty: boolean
  activityItems: PrivacyActivityItem[]
}

export type OperatorPrivacyConsentPageAdapters = {
  getPage: () => Promise<PrivacyConsentPageApiData>
  patchToggles: (
    payload: ReturnType<typeof patchPayloadForGuestPermission>
  ) => Promise<void>
  saveWording?: (input: SavePrivacyConsentInput) => Promise<void>
  getPermissionRecords: (
    params: PermissionRecordsListQueryParams
  ) => Promise<PermissionRecordsListResponse>
  getActivity: () => Promise<{ items: PrivacyConsentActivityApiItem[] }>
  getLocationFilterOptions: () => Array<{ id: string; label: string }>
  navigateToGuestProfile?: (
    locationGuestId: number,
    locationId: number
  ) => void
  debounceMs?: number
  getNow?: () => Date
}

export type OperatorPrivacyConsentPageModule = {
  getSnapshot: () => PrivacyConsentSnapshot
  subscribe: (listener: () => void) => () => void
  load: () => Promise<void>
  retryLoad: () => Promise<void>
  setActiveTabFromUrl: (raw: string | null | undefined) => void
  requestTabChange: (tabId: PrivacyConsentTabId) => void
  setGuestPermissionEnabled: (
    id: GuestPermissionId,
    enabled: boolean
  ) => Promise<void>
  setPermissionRecordsSearchQuery: (query: string) => void
  setPermissionRecordsFiltersSession: (
    session: FilterSheetSession | null
  ) => void
  setPermissionRecordsFiltersOpen: (open: boolean) => void
  openPermissionRecordsFilters: () => void
  applyPermissionRecordsFilters: () => void
  removePermissionRecordsFilterChip: (chip: FilterChip) => void
  clearPermissionRecordsSearchAndFilters: () => void
  viewPermissionRecord: (recordId: string) => void
}

const DEFAULT_SEARCH_DEBOUNCE_MS = 300

/** Figma 5746:100788 shows Filters (3): Eligible to contact, Negative, Camden. */
function demoPermissionRecordsApplied(
  schemaFieldsEmpty: ReturnType<typeof emptySelection>
) {
  return {
    ...schemaFieldsEmpty,
    currentState: {
      kind: "multi-select" as const,
      ids: ["eligible-to-contact", "negative"],
    },
    location: {
      kind: "multi-select" as const,
      ids: ["camden"],
    },
  }
}

function filterClientSideDemoRows(
  rows: readonly PermissionRecordRow[],
  applied: FilterSheetSession["applied"] | undefined,
  query: string
): PermissionRecordRow[] {
  const lowered = query.trim().toLowerCase()
  return rows.filter((row) => {
    const permissionIds =
      applied?.permission?.kind === "multi-select"
        ? applied.permission.ids
        : []
    if (permissionIds.length > 0 && !permissionIds.includes(row.permissionId)) {
      return false
    }

    const currentStateIds =
      applied?.currentState?.kind === "multi-select"
        ? applied.currentState.ids.filter(
            (id) => id === "granted" || id === "withdrawn"
          )
        : []
    if (
      currentStateIds.length > 0
      && !currentStateIds.includes(row.currentState)
    ) {
      return false
    }

    const locationIds =
      applied?.location?.kind === "multi-select" ? applied.location.ids : []
    if (locationIds.length > 0 && !locationIds.includes(row.locationId)) {
      return false
    }

    if (lowered === "") {
      return true
    }
    return (
      row.guestName.toLowerCase().includes(lowered)
      || row.searchText.includes(lowered)
    )
  })
}

export function createOperatorPrivacyConsentPageModule(
  adapters: OperatorPrivacyConsentPageAdapters,
  options: {
    initialTabId?: string | null
    /** Test/demo seed path — skips live adapters on load. */
    demo?: {
      privacySetupRows?: PrivacySetupStatusRow[]
      guestPermissions?: GuestPermissionCard[]
      permissionRecordsRows?: PermissionRecordRow[]
      activityItems?: PrivacyActivityItem[]
      skipPermissionRecordsDemoFilters?: boolean
    }
  } = {}
): OperatorPrivacyConsentPageModule {
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS
  const getNow = adapters.getNow ?? (() => new Date())
  const isDemo = options.demo != null

  let privacySetupRows = options.demo?.privacySetupRows ?? [
    ...PRIVACY_SETUP_STATUS_DEMO_ROWS,
  ]
  let guestPermissions = (options.demo?.guestPermissions ?? [
    ...GUEST_PERMISSIONS_DEMO_CARDS,
  ]).map((card) => ({ ...card }))
  let permissionRecordsRows: PermissionRecordRow[] = isDemo
    ? [...(options.demo?.permissionRecordsRows ?? PERMISSION_RECORDS_DEMO_ROWS)]
    : []
  let activityItems: PrivacyActivityItem[] = options.demo?.activityItems ?? [
    ...PRIVACY_ACTIVITY_DEMO_ITEMS,
  ]
  let actorCanManage = false
  let canViewGuests = false
  let loadStatus: PrivacyConsentSnapshot["loadStatus"] = isDemo
    ? "loaded"
    : "idle"
  let loadGeneration = 0
  let recordsGeneration = 0
  let searchTimer: ReturnType<typeof setTimeout> | null = null
  const permissionRecordsById = new Map<string, PermissionRecordRow>()

  let activeTabId = resolvePrivacyConsentTabId(options.initialTabId)
  let permissionRecordsSearchQuery = ""
  let permissionRecordsFiltersOpen = false
  let permissionRecordsFiltersSession: FilterSheetSession | null = null
  let permissionRecordsLocationOptions = isDemo
    ? [...new Map(
        permissionRecordsRows.map((row) => [
          row.locationId,
          { id: row.locationId, label: row.locationLabel },
        ])
      ).values()]
    : adapters.getLocationFilterOptions()
  let permissionRecordsTotalCount = isDemo ? permissionRecordsRows.length : 0
  let permissionRecordsPage = 1

  const listeners = new Set<() => void>()
  let snapshot: PrivacyConsentSnapshot

  const schema = () =>
    permissionRecordsFilterSheetSchema({
      locations: permissionRecordsLocationOptions,
    })

  if (isDemo && !options.demo?.skipPermissionRecordsDemoFilters) {
    permissionRecordsFiltersSession = openSession(
      demoPermissionRecordsApplied(emptySelection(schema()))
    )
  }

  const emit = () => {
    snapshot = projectSnapshot()
    for (const listener of listeners) {
      listener()
    }
  }

  const projectSnapshot = (): PrivacyConsentSnapshot => {
    const applied = permissionRecordsFiltersSession?.applied
    const filteredRows = isDemo
      ? filterClientSideDemoRows(
          permissionRecordsRows,
          applied,
          permissionRecordsSearchQuery
        )
      : permissionRecordsRows
    const filterChips =
      applied == null ? [] : projectChips(schema(), applied)

    return {
      activeTabId,
      tabs: PRIVACY_CONSENT_TAB_IDS.map((id) => ({
        id,
        label: PRIVACY_CONSENT_TAB_LABELS[id],
      })),
      loadStatus,
      actorCanManage,
      canViewGuests,
      privacySetupRows,
      guestPermissions: guestPermissions.map((card) => ({ ...card })),
      permissionRecordsSearchQuery,
      permissionRecordsFilterChips: filterChips,
      permissionRecordsFilterChipCount:
        applied == null ? 0 : chipCount(schema(), applied),
      permissionRecordsFiltersOpen,
      permissionRecordsFiltersSession,
      permissionRecordsLocationOptions,
      permissionRecordsRows: filteredRows,
      permissionRecordsEmpty:
        permissionRecordsTotalCount === 0 && loadStatus !== "loading",
      activityItems,
    }
  }

  snapshot = projectSnapshot()

  const applyPageData = (data: PrivacyConsentPageApiData) => {
    privacySetupRows = mapPrivacySetupRowsFromApi(data.privacySetupRows)
    guestPermissions = mapGuestPermissionCardsFromApi(data)
    actorCanManage = data.actorCanManage
    canViewGuests = data.canViewGuests
  }

  const applyRecordsResponse = (response: PermissionRecordsListResponse) => {
    const now = getNow()
    permissionRecordsRows = response.rows.map((row) => {
      const mapped = mapPermissionRecordRowFromApi(row, now)
      permissionRecordsById.set(mapped.id, mapped)
      return mapped
    })
    permissionRecordsTotalCount = response.totalCount
    permissionRecordsPage = response.page
  }

  const applyActivityResponse = (
    response: Awaited<ReturnType<OperatorPrivacyConsentPageAdapters["getActivity"]>>
  ) => {
    const now = getNow()
    activityItems = response.items.map((item) =>
      mapPrivacyActivityItemFromApi(item, now)
    )
  }

  const fetchPermissionRecords = async () => {
    if (isDemo) {
      return
    }

    const generation = ++recordsGeneration
    try {
      const response = await adapters.getPermissionRecords(
        buildPermissionRecordsListQueryParams({
          searchQuery: permissionRecordsSearchQuery,
          page: permissionRecordsPage,
          pageSize: PERMISSION_RECORDS_PAGE_SIZE,
          applied: permissionRecordsFiltersSession?.applied ?? null,
          now: getNow(),
        })
      )
      if (generation !== recordsGeneration) {
        return
      }
      applyRecordsResponse(response)
      emit()
    } catch {
      if (generation !== recordsGeneration) {
        return
      }
      loadStatus = "error"
      emit()
    }
  }

  const scheduleRecordsFetch = () => {
    if (isDemo) {
      emit()
      return
    }
    if (searchTimer != null) {
      clearTimeout(searchTimer)
    }
    searchTimer = setTimeout(() => {
      searchTimer = null
      permissionRecordsPage = 1
      void fetchPermissionRecords()
    }, debounceMs)
  }

  const fetchAll = async () => {
    if (isDemo) {
      return
    }

    const generation = ++loadGeneration
    if (loadStatus === "idle" || loadStatus === "error") {
      loadStatus = "loading"
      emit()
    }

    try {
      permissionRecordsLocationOptions = adapters.getLocationFilterOptions()
      const [pageData, recordsResponse, activityResponse] = await Promise.all([
        adapters.getPage(),
        adapters.getPermissionRecords(
          buildPermissionRecordsListQueryParams({
            searchQuery: permissionRecordsSearchQuery,
            page: permissionRecordsPage,
            pageSize: PERMISSION_RECORDS_PAGE_SIZE,
            applied: permissionRecordsFiltersSession?.applied ?? null,
            now: getNow(),
          })
        ),
        adapters.getActivity(),
      ])

      if (generation !== loadGeneration) {
        return
      }

      applyPageData(pageData)
      applyRecordsResponse(recordsResponse)
      applyActivityResponse(activityResponse)
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

  return {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    load: fetchAll,
    retryLoad: fetchAll,
    setActiveTabFromUrl: (raw) => {
      const next = resolvePrivacyConsentTabId(raw)
      if (next === activeTabId) {
        return
      }
      activeTabId = next
      emit()
    },
    requestTabChange: (tabId) => {
      if (tabId === activeTabId) {
        return
      }
      activeTabId = tabId
      emit()
    },
    setGuestPermissionEnabled: async (id, enabled) => {
      if (isDemo) {
        const index = guestPermissions.findIndex((card) => card.id === id)
        if (index < 0) {
          return
        }
        const current = guestPermissions[index]
        if (current == null || current.enabled === enabled) {
          return
        }
        guestPermissions = guestPermissions.map((card, cardIndex) =>
          cardIndex === index ? { ...card, enabled } : card
        )
        emit()
        return
      }

      if (!actorCanManage) {
        return
      }

      const index = guestPermissions.findIndex((card) => card.id === id)
      if (index < 0) {
        return
      }
      const current = guestPermissions[index]
      if (current == null || current.enabled === enabled) {
        return
      }

      const previous = guestPermissions
      guestPermissions = guestPermissions.map((card, cardIndex) =>
        cardIndex === index ? { ...card, enabled } : card
      )
      emit()

      try {
        await adapters.patchToggles(patchPayloadForGuestPermission(id, enabled))
        const [pageData, activityResponse] = await Promise.all([
          adapters.getPage(),
          adapters.getActivity(),
        ])
        applyPageData(pageData)
        applyActivityResponse(activityResponse)
        emit()
      } catch {
        guestPermissions = previous
        emit()
      }
    },
    setPermissionRecordsSearchQuery: (query) => {
      permissionRecordsSearchQuery = query
      if (isDemo) {
        emit()
        return
      }
      scheduleRecordsFetch()
    },
    setPermissionRecordsFiltersSession: (session) => {
      permissionRecordsFiltersSession = session
      emit()
    },
    setPermissionRecordsFiltersOpen: (open) => {
      permissionRecordsFiltersOpen = open
      if (!open) {
        emit()
        return
      }
      permissionRecordsFiltersSession =
        permissionRecordsFiltersSession
        ?? openSession(emptySelection(schema()))
      emit()
    },
    openPermissionRecordsFilters: () => {
      permissionRecordsFiltersSession =
        permissionRecordsFiltersSession
        ?? openSession(emptySelection(schema()))
      permissionRecordsFiltersOpen = true
      emit()
    },
    applyPermissionRecordsFilters: () => {
      if (permissionRecordsFiltersSession == null) {
        return
      }
      permissionRecordsFiltersSession = commitPending(
        permissionRecordsFiltersSession
      )
      permissionRecordsFiltersOpen = false
      permissionRecordsPage = 1
      if (isDemo) {
        emit()
        return
      }
      void fetchPermissionRecords()
    },
    removePermissionRecordsFilterChip: (chip) => {
      if (permissionRecordsFiltersSession == null) {
        return
      }
      permissionRecordsFiltersSession = openSession(
        removeAppliedChip(
          schema(),
          permissionRecordsFiltersSession.applied,
          chip
        )
      )
      permissionRecordsPage = 1
      if (isDemo) {
        emit()
        return
      }
      void fetchPermissionRecords()
    },
    clearPermissionRecordsSearchAndFilters: () => {
      permissionRecordsSearchQuery = ""
      permissionRecordsFiltersSession = null
      permissionRecordsPage = 1
      if (isDemo) {
        emit()
        return
      }
      void fetchPermissionRecords()
    },
    viewPermissionRecord: (recordId) => {
      if (!canViewGuests || adapters.navigateToGuestProfile == null) {
        return
      }
      const row = permissionRecordsById.get(recordId)
      if (row == null) {
        return
      }
      adapters.navigateToGuestProfile(row.locationGuestId, Number.parseInt(row.locationId, 10))
    },
  }
}

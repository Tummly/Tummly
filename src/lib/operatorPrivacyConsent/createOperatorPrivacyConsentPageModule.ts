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
import { permissionRecordsFilterSheetSchema } from "@/lib/operatorPrivacyConsent/permissionRecordsFilterSheetSchema"
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

export type OperatorPrivacyConsentPageModule = {
  getSnapshot: () => PrivacyConsentSnapshot
  subscribe: (listener: () => void) => () => void
  setActiveTabFromUrl: (raw: string | null | undefined) => void
  requestTabChange: (tabId: PrivacyConsentTabId) => void
  setGuestPermissionEnabled: (
    id: GuestPermissionId,
    enabled: boolean
  ) => void
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

function locationOptionsFromRows(rows: readonly PermissionRecordRow[]) {
  const seen = new Map<string, string>()
  for (const row of rows) {
    if (!seen.has(row.locationId)) {
      seen.set(row.locationId, row.locationLabel)
    }
  }
  return [...seen.entries()].map(([id, label]) => ({ id, label }))
}

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

function rowMatchesPermissionRecordsFilters(
  row: PermissionRecordRow,
  applied: OperatorFilterSelection | undefined
): boolean {
  if (applied == null) {
    return true
  }

  const permissionIds = multiSelectIds(applied, "permission")
  if (permissionIds.length > 0 && !permissionIds.includes(row.permissionId)) {
    return false
  }

  const currentStateIds = multiSelectIds(applied, "currentState").filter(
    (id) => id === "granted" || id === "withdrawn"
  )
  if (
    currentStateIds.length > 0
    && !currentStateIds.includes(row.currentState)
  ) {
    return false
  }

  const locationIds = multiSelectIds(applied, "location")
  if (locationIds.length > 0 && !locationIds.includes(row.locationId)) {
    return false
  }

  return true
}

/** Figma 5746:100788 shows Filters (3): Eligible to contact, Negative, Camden. */
function demoPermissionRecordsApplied(
  schemaFieldsEmpty: OperatorFilterSelection
): OperatorFilterSelection {
  return {
    ...schemaFieldsEmpty,
    currentState: {
      kind: "multi-select",
      ids: ["eligible-to-contact", "negative"],
    },
    location: {
      kind: "multi-select",
      ids: ["camden"],
    },
  }
}

export function createOperatorPrivacyConsentPageModule(options: {
  initialTabId?: string | null
  privacySetupRows?: PrivacySetupStatusRow[]
  guestPermissions?: GuestPermissionCard[]
  permissionRecordsRows?: PermissionRecordRow[]
  activityItems?: PrivacyActivityItem[]
  /** When true, skip Figma Filters (3) seed. */
  skipPermissionRecordsDemoFilters?: boolean
} = {}): OperatorPrivacyConsentPageModule {
  const privacySetupRows = options.privacySetupRows ?? [
    ...PRIVACY_SETUP_STATUS_DEMO_ROWS,
  ]
  let guestPermissions = (options.guestPermissions ?? [
    ...GUEST_PERMISSIONS_DEMO_CARDS,
  ]).map((card) => ({ ...card }))
  const allPermissionRecords =
    options.permissionRecordsRows ?? [...PERMISSION_RECORDS_DEMO_ROWS]
  const activityItems = options.activityItems ?? [...PRIVACY_ACTIVITY_DEMO_ITEMS]

  let activeTabId = resolvePrivacyConsentTabId(options.initialTabId)
  let permissionRecordsSearchQuery = ""
  let permissionRecordsFiltersOpen = false
  let permissionRecordsFiltersSession: FilterSheetSession | null = null
  const listeners = new Set<() => void>()
  let snapshot: PrivacyConsentSnapshot

  const schema = () =>
    permissionRecordsFilterSheetSchema({
      locations: locationOptionsFromRows(allPermissionRecords),
    })

  if (!options.skipPermissionRecordsDemoFilters) {
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
    const query = permissionRecordsSearchQuery.trim().toLowerCase()
    const filtered = allPermissionRecords.filter((row) => {
      if (!rowMatchesPermissionRecordsFilters(row, applied)) {
        return false
      }
      if (query === "") {
        return true
      }
      return (
        row.guestName.toLowerCase().includes(query)
        || row.searchText.includes(query)
      )
    })
    const filterChips =
      applied == null ? [] : projectChips(schema(), applied)

    return {
      activeTabId,
      tabs: PRIVACY_CONSENT_TAB_IDS.map((id) => ({
        id,
        label: PRIVACY_CONSENT_TAB_LABELS[id],
      })),
      privacySetupRows,
      guestPermissions: guestPermissions.map((card) => ({ ...card })),
      permissionRecordsSearchQuery,
      permissionRecordsFilterChips: filterChips,
      permissionRecordsFilterChipCount:
        applied == null ? 0 : chipCount(schema(), applied),
      permissionRecordsFiltersOpen,
      permissionRecordsFiltersSession,
      permissionRecordsLocationOptions: locationOptionsFromRows(
        allPermissionRecords
      ),
      permissionRecordsRows: filtered,
      permissionRecordsEmpty: filtered.length === 0,
      activityItems,
    }
  }

  snapshot = projectSnapshot()

  return {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
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
    setGuestPermissionEnabled: (id, enabled) => {
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
    },
    setPermissionRecordsSearchQuery: (query) => {
      permissionRecordsSearchQuery = query
      emit()
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
      emit()
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
      emit()
    },
    clearPermissionRecordsSearchAndFilters: () => {
      permissionRecordsSearchQuery = ""
      permissionRecordsFiltersSession = null
      emit()
    },
    viewPermissionRecord: (_recordId) => {
      // Guest profile deep-link lands with the records API.
    },
  }
}

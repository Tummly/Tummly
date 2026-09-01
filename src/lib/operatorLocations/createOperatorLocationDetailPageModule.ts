import type { LocationsListApiRow } from "@/lib/operatorLocations/locationsListQueryParams"
import type { LocationsListResponse } from "@/lib/operatorLocations/locationsListQueryParams"
import {
  buildEmptyOverviewMetrics,
  buildLocationGuestActivityChecklist,
  buildLocationControlsStatus,
  buildLocationSetupChecklist,
  buildLocationTeamAccessRows,
  formatLocationDetailHeaderMeta,
  locationControlsDangerActions,
  LOCATION_DETAIL_TAB_IDS,
  LOCATION_DETAIL_TAB_LABELS,
  resolveLocationDetailTabId,
  type LocationControlsDangerAction,
  type LocationControlsStatusFieldId,
  type LocationDetailLatestFeedbackRow,
  type LocationDetailOfferCard,
  type LocationDetailOverviewMetricId,
  type LocationDetailQrRow,
  type LocationDetailTabId,
  type LocationDetailTeamAccessRow,
  type LocationGuestActivityChecklistItemId,
  type LocationGuestActivityChecklistStatusId,
  type LocationControlsLifecycleActionId,
  type LocationSetupChecklistItemId,
  type LocationSetupChecklistStatusId,
} from "@/lib/operatorLocations/locationDetailPresentation"
import type { LocationLifecycleStatus } from "@/lib/operatorLocations/locationsPresentation"
import type { LocationSetupStatus } from "@/lib/operatorLocations/locationsPresentation"

export type LocationDetailSnapshot = {
  locationId: number
  name: string
  city: string
  lifecycleStatus: LocationLifecycleStatus
  headerMeta: string
  activeTabId: LocationDetailTabId
  tabs: Array<{ id: LocationDetailTabId; label: string }>
  overviewMetrics: Record<LocationDetailOverviewMetricId, number>
  setupChecklist: Record<
    LocationSetupChecklistItemId,
    LocationSetupChecklistStatusId
  >
  guestActivityChecklist: Record<
    LocationGuestActivityChecklistItemId,
    LocationGuestActivityChecklistStatusId
  >
  latestFeedbackRows: LocationDetailLatestFeedbackRow[]
  teamAccessRows: LocationDetailTeamAccessRow[]
  locationControlsStatus: Record<LocationControlsStatusFieldId, string>
  locationControlsActions: LocationControlsDangerAction[]
  lifecycleMutationPending: boolean
  qrRows: LocationDetailQrRow[]
  offerCards: LocationDetailOfferCard[]
  loadStatus: "idle" | "loading" | "loaded" | "error" | "not-found"
}

export type OperatorLocationDetailPageAdapters = {
  getList: (params: {
    page: number
    pageSize: number
    sort: "name-asc"
  }) => Promise<LocationsListResponse>
  /** Optional fallback name when the list response does not include the row. */
  fallbackName?: string
  mutateLifecycle?: (
    locationId: number,
    action: LocationControlsLifecycleActionId
  ) => Promise<void>
}

export type OperatorLocationDetailPageModule = {
  getSnapshot: () => LocationDetailSnapshot
  subscribe: (listener: () => void) => () => void
  load: () => Promise<void>
  setActiveTabFromUrl: (raw: string | null | undefined) => void
  requestTabChange: (tabId: LocationDetailTabId) => void
  requestLifecycleAction: (action: LocationControlsLifecycleActionId) => void
}

function cityFromRow(row: LocationsListApiRow | null, fallbackAddress: string) {
  if (row?.city?.trim()) {
    return row.city.trim()
  }
  if (row?.cityPostcode?.trim()) {
    return row.cityPostcode.split(",")[0]?.trim() || "—"
  }
  const parts = fallbackAddress
    .split(",")
    .map((part) => part.trim())
    .filter(Boolean)
  if (parts.length >= 2) {
    return parts[parts.length - 1] ?? "—"
  }
  return "—"
}

export function createOperatorLocationDetailPageModule(
  locationId: number,
  adapters: OperatorLocationDetailPageAdapters,
  options: {
    initialTabId?: string | null
    fallbackName?: string
    fallbackAddress?: string
  } = {}
): OperatorLocationDetailPageModule {
  let activeTabId = resolveLocationDetailTabId(options.initialTabId)
  let name = options.fallbackName?.trim() || "Location"
  let city = "—"
  let lifecycleStatus: LocationLifecycleStatus = "active"
  let setupStatus: LocationSetupStatus = "not-started"
  let overviewMetrics = buildEmptyOverviewMetrics()
  let setupChecklist = buildLocationSetupChecklist({
    lifecycleStatus: "active",
    setupStatus: "not-started",
    managerName: null,
    qrCount: 0,
    hasOffer: false,
  })
  let guestActivityChecklist = buildLocationGuestActivityChecklist({
    guestsCaptured: 0,
    optIns: 0,
    feedback: 0,
    offersClaimed: 0,
    offersRedeemed: 0,
    pendingRecoveryCount: 0,
    pendingFeedbackActionCount: 0,
  })
  let latestFeedbackRows: LocationDetailLatestFeedbackRow[] = []
  let teamAccessRows: LocationDetailTeamAccessRow[] = []
  let locationControlsStatus = buildLocationControlsStatus({
    lifecycleStatus: "active",
    setupStatus: "not-started",
    liveQrCount: 0,
  })
  let locationControlsActions = locationControlsDangerActions("active")
  let lifecycleMutationPending = false
  let qrRows: LocationDetailQrRow[] = []
  let offerCards: LocationDetailOfferCard[] = []
  let loadStatus: LocationDetailSnapshot["loadStatus"] = "idle"
  let loadGeneration = 0

  const listeners = new Set<() => void>()
  let snapshot: LocationDetailSnapshot

  const emit = () => {
    snapshot = projectSnapshot()
    for (const listener of listeners) {
      listener()
    }
  }

  const projectSnapshot = (): LocationDetailSnapshot => {
    const headerMeta = formatLocationDetailHeaderMeta({
      city,
      qrCount: qrRows.length,
      guestCount: overviewMetrics.guestsCaptured,
    })

    return {
      locationId,
      name,
      city,
      lifecycleStatus,
      headerMeta,
      activeTabId,
      tabs: LOCATION_DETAIL_TAB_IDS.map((id) => ({
        id,
        label: LOCATION_DETAIL_TAB_LABELS[id],
      })),
      overviewMetrics,
      setupChecklist,
      guestActivityChecklist,
      latestFeedbackRows,
      teamAccessRows,
      locationControlsStatus,
      locationControlsActions,
      lifecycleMutationPending,
      qrRows,
      offerCards,
      loadStatus,
    }
  }

  snapshot = projectSnapshot()

  const load = async () => {
    const generation = ++loadGeneration
    loadStatus = "loading"
    emit()

    try {
      const response = await adapters.getList({
        page: 1,
        pageSize: 100,
        sort: "name-asc",
      })

      if (generation !== loadGeneration) {
        return
      }

      const row =
        response.rows.find((entry) => entry.id === locationId) ?? null

      if (row == null) {
        if (adapters.fallbackName?.trim() || options.fallbackName?.trim()) {
          name =
            adapters.fallbackName?.trim()
            || options.fallbackName?.trim()
            || name
          city = cityFromRow(null, options.fallbackAddress ?? "")
          loadStatus = "loaded"
          emit()
          return
        }
        loadStatus = "not-found"
        emit()
        return
      }

      name = row.name
      city = cityFromRow(row, "")
      lifecycleStatus = row.lifecycleStatus
      setupStatus = row.setupStatus
      overviewMetrics = buildEmptyOverviewMetrics()
      setupChecklist = buildLocationSetupChecklist({
        lifecycleStatus: row.lifecycleStatus,
        setupStatus: row.setupStatus,
        managerName: row.managerName,
        qrCount: qrRows.length,
        hasOffer: offerCards.length > 0,
      })
      guestActivityChecklist = buildLocationGuestActivityChecklist({
        guestsCaptured: overviewMetrics.guestsCaptured,
        optIns: overviewMetrics.optIns,
        feedback: overviewMetrics.feedback,
        offersClaimed: overviewMetrics.offersClaimed,
        offersRedeemed: overviewMetrics.offersRedeemed,
        pendingRecoveryCount: latestFeedbackRows.filter(
          (entry) => entry.canStartRecovery
        ).length,
        pendingFeedbackActionCount: latestFeedbackRows.filter(
          (entry) => entry.sentiment === "negative"
        ).length,
      })
      qrRows = []
      offerCards = []
      latestFeedbackRows = []
      teamAccessRows = buildLocationTeamAccessRows({
        managerName: row.managerName,
        managerUserId: row.managerUserId ?? null,
      })
      locationControlsStatus = buildLocationControlsStatus({
        lifecycleStatus: row.lifecycleStatus,
        setupStatus: row.setupStatus,
        liveQrCount: qrRows.length,
      })
      locationControlsActions = locationControlsDangerActions(
        row.lifecycleStatus
      )
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
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot: () => snapshot,
    load,
    setActiveTabFromUrl: (raw) => {
      const next = resolveLocationDetailTabId(raw)
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
    requestLifecycleAction: (action) => {
      const mutate = adapters.mutateLifecycle
      if (mutate == null || lifecycleMutationPending) {
        return
      }

      lifecycleMutationPending = true
      emit()

      void (async () => {
        try {
          await mutate(locationId, action)
          lifecycleMutationPending = false
          await load()
        } catch {
          lifecycleMutationPending = false
          loadStatus = "error"
          emit()
        }
      })()
    },
  }
}

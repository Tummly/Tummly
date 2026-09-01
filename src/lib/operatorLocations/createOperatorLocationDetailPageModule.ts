import type { LocationDetailApiResponse } from "@/lib/operatorLocations/locationDetailApi"
import { mapLocationDetailSetupChecklist } from "@/lib/operatorLocations/locationDetailApi"
import {
  buildEmptyOverviewMetrics,
  buildLocationGuestActivityChecklist,
  buildLocationControlsStatus,
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
import { isAxiosError } from "axios"

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
  getDetail: (locationId: number) => Promise<LocationDetailApiResponse>
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

function cityFromHeader(city: string | null | undefined): string {
  return city?.trim() || "—"
}

export function createOperatorLocationDetailPageModule(
  locationId: number,
  adapters: OperatorLocationDetailPageAdapters,
  options: {
    initialTabId?: string | null
    fallbackName?: string
  } = {}
): OperatorLocationDetailPageModule {
  let activeTabId = resolveLocationDetailTabId(options.initialTabId)
  let name = options.fallbackName?.trim() || "Location"
  let city = "—"
  let lifecycleStatus: LocationLifecycleStatus = "active"
  let liveQrCount = 0
  let guestsCapturedThisMonth = 0
  let overviewMetrics = buildEmptyOverviewMetrics()
  let setupChecklist: Record<
    LocationSetupChecklistItemId,
    LocationSetupChecklistStatusId
  > = mapLocationDetailSetupChecklist({})
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
      qrCount: liveQrCount,
      guestCount: guestsCapturedThisMonth,
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

  const applyDetailResponse = (response: LocationDetailApiResponse) => {
    const header = response.header
    name = header.name
    city = cityFromHeader(header.city)
    lifecycleStatus = header.lifecycleStatus
    liveQrCount = header.liveQrCount
    guestsCapturedThisMonth = header.guestsCapturedThisMonth
    setupChecklist = mapLocationDetailSetupChecklist(response.setupChecklist)
    teamAccessRows = buildLocationTeamAccessRows({
      managerName: header.managerName,
      managerUserId: header.managerUserId,
    })
    locationControlsStatus = buildLocationControlsStatus({
      lifecycleStatus: header.lifecycleStatus,
      setupStatus: header.setupStatus,
      liveQrCount: header.liveQrCount,
    })
    locationControlsActions = locationControlsDangerActions(
      header.lifecycleStatus
    )
  }

  const load = async () => {
    const generation = ++loadGeneration
    loadStatus = "loading"
    emit()

    try {
      const response = await adapters.getDetail(locationId)

      if (generation !== loadGeneration) {
        return
      }

      applyDetailResponse(response)

      // Ticket 02+ will populate these from detail GET extensions.
      overviewMetrics = buildEmptyOverviewMetrics()
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
      loadStatus = "loaded"
      emit()
    } catch (error) {
      if (generation !== loadGeneration) {
        return
      }
      if (isAxiosError(error) && error.response?.status === 404) {
        loadStatus = "not-found"
        emit()
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

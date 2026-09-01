import type {
  LocationDetailApiResponse,
  UpdateLocationDetailInput,
} from "@/lib/operatorLocations/locationDetailApi"
import {
  mapLocationDetailGuestActivityChecklist,
  mapLocationDetailLatestFeedbackRows,
  mapLocationDetailOfferCards,
  mapLocationDetailOverviewMetrics,
  mapLocationDetailQrRows,
  mapLocationDetailSetupChecklist,
  mapLocationDetailTeamAccessRows,
} from "@/lib/operatorLocations/locationDetailApi"
import {
  buildEmptyOverviewMetrics,
  buildLocationControlsStatus,
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
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import {
  formatLocationControlsLastScanAt,
  formatLocationsLastActivityAt,
} from "@/lib/operatorLocations/locationsPresentation"
import { isAxiosError } from "axios"

export type LocationDetailEditFields = {
  locationName: string
  address: string
  city: string
  postcode: string
  locationPhone: string
  localContact: string
}

export type LocationDetailSnapshot = {
  locationId: number
  name: string
  city: string
  lifecycleStatus: LocationLifecycleStatus
  headerMeta: string
  editFields: LocationDetailEditFields
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
  editDetailsPending: boolean
  qrRows: LocationDetailQrRow[]
  offerCards: LocationDetailOfferCard[]
  loadStatus: "idle" | "loading" | "loaded" | "error" | "not-found"
}

export type OperatorLocationDetailPageAdapters = {
  getDetail: (locationId: number) => Promise<LocationDetailApiResponse>
  updateDetails?: (
    locationId: number,
    input: UpdateLocationDetailInput
  ) => Promise<void>
  mutateLifecycle?: (
    locationId: number,
    action: LocationControlsLifecycleActionId
  ) => Promise<void>
  getNow?: () => Date
}

export type OperatorLocationDetailPageModule = {
  getSnapshot: () => LocationDetailSnapshot
  subscribe: (listener: () => void) => () => void
  load: () => Promise<void>
  setActiveTabFromUrl: (raw: string | null | undefined) => void
  requestTabChange: (tabId: LocationDetailTabId) => void
  requestLifecycleAction: (
    action: LocationControlsLifecycleActionId
  ) => Promise<void>
  saveEditDetails: (input: UpdateLocationDetailInput) => Promise<void>
}

function cityFromHeader(city: string | null | undefined): string {
  return city?.trim() || "—"
}

function editFieldsFromHeader(
  header: LocationDetailApiResponse["header"]
): LocationDetailEditFields {
  return {
    locationName: header.name,
    address: header.address,
    city: header.city?.trim() ?? "",
    postcode: header.postcode?.trim() ?? "",
    locationPhone: header.locationPhone?.trim() ?? "",
    localContact: header.localContact?.trim() ?? "",
  }
}

export function createOperatorLocationDetailPageModule(
  locationId: number,
  adapters: OperatorLocationDetailPageAdapters,
  options: {
    initialTabId?: string | null
    fallbackName?: string
    dashboardMode?: OperatorDashboardMode
    nowMs?: () => number
  } = {}
): OperatorLocationDetailPageModule {
  const getNow = adapters.getNow ?? (() => new Date())
  let activeTabId = resolveLocationDetailTabId(options.initialTabId)
  let name = options.fallbackName?.trim() || "Location"
  let city = "—"
  let editFields: LocationDetailEditFields = {
    locationName: name,
    address: "",
    city: "",
    postcode: "",
    locationPhone: "",
    localContact: "",
  }
  let lifecycleStatus: LocationLifecycleStatus = "active"
  let liveQrCount = 0
  let guestsCapturedThisMonth = 0
  let overviewMetrics = buildEmptyOverviewMetrics()
  let setupChecklist: Record<
    LocationSetupChecklistItemId,
    LocationSetupChecklistStatusId
  > = mapLocationDetailSetupChecklist({})
  let guestActivityChecklist = mapLocationDetailGuestActivityChecklist({})
  let latestFeedbackRows: LocationDetailLatestFeedbackRow[] = []
  let teamAccessRows: LocationDetailTeamAccessRow[] = []
  let locationControlsStatus = buildLocationControlsStatus({
    lifecycleStatus: "active",
    setupStatus: "not-started",
    liveQrCount: 0,
  })
  let locationControlsActions = locationControlsDangerActions("active")
  let lifecycleMutationPending = false
  let editDetailsPending = false
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
      editFields,
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
      editDetailsPending,
      qrRows,
      offerCards,
      loadStatus,
    }
  }

  snapshot = projectSnapshot()

  const applyDetailResponse = (response: LocationDetailApiResponse) => {
    const header = response.header
    const now = getNow()
    name = header.name
    city = cityFromHeader(header.city)
    editFields = editFieldsFromHeader(header)
    lifecycleStatus = header.lifecycleStatus
    liveQrCount = header.liveQrCount
    guestsCapturedThisMonth = header.guestsCapturedThisMonth
    setupChecklist = mapLocationDetailSetupChecklist(response.setupChecklist)
    overviewMetrics = mapLocationDetailOverviewMetrics(
      response.overviewMetrics
    )
    qrRows = mapLocationDetailQrRows(
      response.qrRows,
      options.nowMs?.() ?? Date.now()
    )
    offerCards =
      options.dashboardMode == null
        ? []
        : mapLocationDetailOfferCards(response.offerCards, {
            mode: options.dashboardMode,
            locationId,
          })
    guestActivityChecklist = mapLocationDetailGuestActivityChecklist(
      response.guestActivityChecklist ?? {}
    )
    latestFeedbackRows = mapLocationDetailLatestFeedbackRows(
      response.latestFeedbackRows ?? []
    )
    teamAccessRows = mapLocationDetailTeamAccessRows(response.teamAccessRows)
    locationControlsStatus = buildLocationControlsStatus({
      lifecycleStatus: header.lifecycleStatus,
      setupStatus: header.setupStatus,
      liveQrCount: header.liveQrCount,
      lastScanLabel: formatLocationControlsLastScanAt(
        response.locationControls.lastScanAt
      ),
      lastFeedbackLabel: formatLocationsLastActivityAt(
        response.locationControls.lastFeedbackAt,
        now
      ),
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
    requestLifecycleAction: async (action) => {
      const mutate = adapters.mutateLifecycle
      if (mutate == null || lifecycleMutationPending) {
        return
      }

      lifecycleMutationPending = true
      emit()

      try {
        await mutate(locationId, action)
        lifecycleMutationPending = false
        await load()
      } catch {
        lifecycleMutationPending = false
        loadStatus = "error"
        emit()
        throw new Error("Could not update location status.")
      }
    },
    saveEditDetails: async (input) => {
      const update = adapters.updateDetails
      if (update == null || editDetailsPending) {
        throw new Error("Edit details is not configured.")
      }

      editDetailsPending = true
      emit()

      try {
        await update(locationId, input)
        editDetailsPending = false
        await load()
      } catch (error) {
        editDetailsPending = false
        emit()
        throw error
      }
    },
  }
}

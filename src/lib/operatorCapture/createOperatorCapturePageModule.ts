import {
  buildCaptureGuestExperience,
  type OperatorCaptureGuestExperienceView,
} from "@/lib/operatorCapture/buildCaptureGuestExperience"
import {
  buildCapturePerformanceKpis,
  type CapturePerformanceFacts,
  type OperatorCaptureKpi,
} from "@/lib/operatorCapture/buildCapturePerformanceKpis"
import {
  buildCapturePlacements,
  type OperatorCapturePlacementRow,
} from "@/lib/operatorCapture/buildCapturePlacements"
import {
  buildPlacementDetailDrawer,
  PLACEMENT_INTERNAL_DESCRIPTION_MAX_LENGTH,
  type PlacementDetailDrawerView,
} from "@/lib/operatorCapture/buildPlacementDetailDrawer"
import {
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  CapturePerformanceResponse,
  CapturePlacementsResponse,
  CapturePlacementStatus,
} from "@/types/dashboard"


export type OperatorCaptureWorkspaceLocation = {
  id: number
  locationName: string
  address?: string
}


export type OperatorCaptureWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorCaptureWorkspaceLocation[]
}

export type CopyCapturePlacementLinkResult = "copied" | "failed" | "noop"

export type PlacementDetailStubbedAction =
  | "pause"
  | "activate"
  | "rotate"
  | "archive"
  | "save-description"

export type PlacementDetailDrawerSnapshot = {
  isOpen: boolean
  selectedQrCodeId: number | null
  details: PlacementDetailDrawerView | null
  lastStubbedAction: PlacementDetailStubbedAction | null
}


export type OperatorCapturePerformanceView = {
  kpis: OperatorCaptureKpi[]
  isEmpty: boolean
}


export type OperatorCapturePlacementsView = {
  rows: OperatorCapturePlacementRow[]
  isEmpty: boolean
}

export type OperatorCaptureDigitalGuestLinksView = {
  rows: never[]
  isEmpty: boolean
}

export type { OperatorCaptureGuestExperienceView }

export type OpenGuestExperiencePreviewResult =
  | "opened"
  | "picker"
  | "noop"

export type OperatorCaptureViewModel = {
  locationId: number
  locationName: string
  dateRangeLabel: string
  performance: OperatorCapturePerformanceView
  placements: OperatorCapturePlacementsView
  digitalGuestLinks: OperatorCaptureDigitalGuestLinksView
  guestExperience: OperatorCaptureGuestExperienceView
}


export type OperatorCapturePageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  performanceLoadStatus: "idle" | "loading" | "loaded" | "error"
  placementsLoadStatus: "idle" | "loading" | "loaded" | "error"
  isGuestExperiencePreviewOpen: boolean
  isGuestExperiencePreviewPickerOpen: boolean
  /** When set, guest-experience preview shows this placement label instead of the Smart Guest default. */
  guestExperiencePreviewPlacementLabel: string | null
  placementDetailDrawer: PlacementDetailDrawerSnapshot
  viewModel: OperatorCaptureViewModel | null
}


export type OperatorCapturePageAdapters = {
  getCapturePerformance: (
    locationId: number,
    from: string,
    to: string
  ) => Promise<CapturePerformanceResponse>
  getCapturePlacements: (
    locationId: number,
    from: string,
    to: string
  ) => Promise<CapturePlacementsResponse>
  pauseCapturePlacement: (
    locationId: number,
    qrCodeId: number
  ) => Promise<{ qrCodeId: number; status: CapturePlacementStatus }>
  resumeCapturePlacement: (
    locationId: number,
    qrCodeId: number
  ) => Promise<{ qrCodeId: number; status: CapturePlacementStatus }>
  copyText: (
    text: string
  ) => Promise<{ ok: true } | { ok: false; error: string }>
  getCapturePerformanceDateRange: () => HomePerformanceDateRange
  onPerformanceLoadError?: (message: string) => void
  onPlacementsLoadError?: (message: string) => void
  onPlacementActionError?: (message: string) => void
  onCopyPlacementLinkError?: (message: string) => void
  /** Optional delay seam for tests; defaults to none. */
  scheduleReady?: () => Promise<void>
  /** Optional clock for relative Last scan labels; defaults to Date.now. */
  nowMs?: () => number
}


export type OperatorCapturePageModule = {
  getSnapshot: () => OperatorCapturePageSnapshot
  subscribe: (listener: () => void) => () => void
  syncWorkspace: (input: OperatorCaptureWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  /** Re-load Capture performance + placements using the current Capture date range. */
  reloadForCapturePerformanceDateRange: () => Promise<void>
  pausePlacement: (qrCodeId: number) => Promise<"paused" | "failed" | "noop">
  resumePlacement: (qrCodeId: number) => Promise<"resumed" | "failed" | "noop">
  copyPlacementLink: (
    qrCodeId: number
  ) => Promise<CopyCapturePlacementLinkResult>
  openGuestExperiencePreview: () => OpenGuestExperiencePreviewResult
  closeGuestExperiencePreview: () => void
  closeGuestExperiencePreviewPicker: () => void
  openPlacementDetail: (qrCodeId: number) => "opened" | "noop"
  closePlacementDetail: () => void
  setPlacementDetailDescriptionDraft: (value: string) => void
  savePlacementDetailDescription: () => "stubbed" | "noop"
  requestPlacementDetailPause: () => "stubbed" | "noop"
  requestPlacementDetailActivate: () => "stubbed" | "noop"
  requestPlacementDetailRotate: () => "stubbed" | "noop"
  requestPlacementDetailArchive: () => "stubbed" | "noop"
  copyPlacementDetailLink: () => Promise<CopyCapturePlacementLinkResult>
  openPlacementDetailPreview: () => "opened" | "noop"
}


type ModuleState = {
  loadStatus: OperatorCapturePageSnapshot["loadStatus"]
  performanceLoadStatus: OperatorCapturePageSnapshot["performanceLoadStatus"]
  placementsLoadStatus: OperatorCapturePageSnapshot["placementsLoadStatus"]
  isGuestExperiencePreviewOpen: boolean
  isGuestExperiencePreviewPickerOpen: boolean
  guestExperiencePreviewPlacementLabel: string | null
  placementDetailIsOpen: boolean
  placementDetailSelectedQrCodeId: number | null
  placementDetailDescriptionDraft: string
  placementDetailLastStubbedAction: PlacementDetailStubbedAction | null
  viewModel: OperatorCaptureViewModel | null
  placementsFacts: CapturePlacementsResponse["placements"] | null
  lastJourneyUpdate: CapturePlacementsResponse["lastJourneyUpdate"] | undefined
  workspace: OperatorCaptureWorkspaceInput | null
  loadGeneration: number
  captureLoadGeneration: number
}


const FALLBACK_LOCATION_NAME = "Location"

const PERFORMANCE_LOAD_ERROR_MESSAGE =
  "Could not load Capture performance. Please try again."

const PLACEMENTS_LOAD_ERROR_MESSAGE =
  "Could not load QR placements. Please try again."


function resolveLocationName(
  input: OperatorCaptureWorkspaceInput,
  locationId: number
): string {
  return (
    input.locations.find((item) => item.id === locationId)?.locationName
    ?? FALLBACK_LOCATION_NAME
  )
}


function resolveLocationAddress(
  input: OperatorCaptureWorkspaceInput,
  locationId: number
): string {
  return input.locations.find((item) => item.id === locationId)?.address ?? ""
}


function factsFromResponse(
  response: CapturePerformanceResponse
): CapturePerformanceFacts {
  return {
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


function emptyPerformanceView(): OperatorCapturePerformanceView {
  return buildCapturePerformanceKpis({
    qrScans: 0,
    qrScansPrevious: 0,
    feedbackSubmitted: 0,
    feedbackSubmittedPrevious: 0,
    marketingOptIns: 0,
    marketingOptInsPrevious: 0,
    offerClaims: 0,
    offerClaimsHasRealData: false,
  })
}


/**
 * Operator Capture page module — adapters in, snapshot out.
 * Owns Capture performance + QR placements load and visit-scoped date range reloads.
 */
export function createOperatorCapturePageModule(
  adapters: OperatorCapturePageAdapters
): OperatorCapturePageModule {
  const scheduleReady =
    adapters.scheduleReady ?? (() => Promise.resolve())
  const nowMs = adapters.nowMs ?? (() => Date.now())

  let state: ModuleState = {
    loadStatus: "idle",
    performanceLoadStatus: "idle",
    placementsLoadStatus: "idle",
    isGuestExperiencePreviewOpen: false,
    isGuestExperiencePreviewPickerOpen: false,
    guestExperiencePreviewPlacementLabel: null,
    placementDetailIsOpen: false,
    placementDetailSelectedQrCodeId: null,
    placementDetailDescriptionDraft: "",
    placementDetailLastStubbedAction: null,
    viewModel: null,
    placementsFacts: null,
    lastJourneyUpdate: undefined,
    workspace: null,
    loadGeneration: 0,
    captureLoadGeneration: 0,
  }

  const closedPlacementDetailDrawer = (): PlacementDetailDrawerSnapshot => ({
    isOpen: false,
    selectedQrCodeId: null,
    details: null,
    lastStubbedAction: null,
  })

  const buildPlacementDetailDrawerSnapshot = (): PlacementDetailDrawerSnapshot => {
    if (
      !state.placementDetailIsOpen ||
      state.placementDetailSelectedQrCodeId == null ||
      state.placementsFacts == null ||
      state.viewModel == null
    ) {
      return {
        isOpen: state.placementDetailIsOpen,
        selectedQrCodeId: state.placementDetailSelectedQrCodeId,
        details: null,
        lastStubbedAction: state.placementDetailLastStubbedAction,
      }
    }

    const fact = state.placementsFacts.find(
      (item) => item.qrCodeId === state.placementDetailSelectedQrCodeId
    )
    if (fact == null) {
      return {
        isOpen: true,
        selectedQrCodeId: state.placementDetailSelectedQrCodeId,
        details: null,
        lastStubbedAction: state.placementDetailLastStubbedAction,
      }
    }

    return {
      isOpen: true,
      selectedQrCodeId: state.placementDetailSelectedQrCodeId,
      details: buildPlacementDetailDrawer({
        fact,
        locationName: state.viewModel.locationName,
        descriptionDraft: state.placementDetailDescriptionDraft,
        nowMs: nowMs(),
      }),
      lastStubbedAction: state.placementDetailLastStubbedAction,
    }
  }

  let snapshot: OperatorCapturePageSnapshot = {
    loadStatus: state.loadStatus,
    performanceLoadStatus: state.performanceLoadStatus,
    placementsLoadStatus: state.placementsLoadStatus,
    isGuestExperiencePreviewOpen: state.isGuestExperiencePreviewOpen,
    isGuestExperiencePreviewPickerOpen: state.isGuestExperiencePreviewPickerOpen,
    guestExperiencePreviewPlacementLabel:
      state.guestExperiencePreviewPlacementLabel,
    placementDetailDrawer: closedPlacementDetailDrawer(),
    viewModel: state.viewModel,
  }
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = {
      loadStatus: state.loadStatus,
      performanceLoadStatus: state.performanceLoadStatus,
      placementsLoadStatus: state.placementsLoadStatus,
      isGuestExperiencePreviewOpen: state.isGuestExperiencePreviewOpen,
      isGuestExperiencePreviewPickerOpen:
        state.isGuestExperiencePreviewPickerOpen,
      guestExperiencePreviewPlacementLabel:
        state.guestExperiencePreviewPlacementLabel,
      placementDetailDrawer: buildPlacementDetailDrawerSnapshot(),
      viewModel: state.viewModel,
    }
    for (const listener of listeners) {
      listener()
    }
  }

  const clearPlacementDetailState = () => ({
    placementDetailIsOpen: false as const,
    placementDetailSelectedQrCodeId: null,
    placementDetailDescriptionDraft: "",
    placementDetailLastStubbedAction: null,
  })

  const currentDateRangeLabel = () =>
    labelForHomePerformanceDateRange(
      adapters.getCapturePerformanceDateRange()
    )

  const buildBaseViewModel = (
    input: OperatorCaptureWorkspaceInput,
    locationId: number,
    performance: OperatorCapturePerformanceView,
    placementsFacts: CapturePlacementsResponse["placements"] | null,
    lastJourneyUpdate: CapturePlacementsResponse["lastJourneyUpdate"] | undefined
  ): OperatorCaptureViewModel => {
    const locationName = resolveLocationName(input, locationId)
    const locationAddress = resolveLocationAddress(input, locationId)

    return {
      locationId,
      locationName,
      dateRangeLabel: currentDateRangeLabel(),
      performance,
      placements: buildCapturePlacements(placementsFacts ?? [], nowMs()),
      digitalGuestLinks: { rows: [], isEmpty: true },
      guestExperience: buildCaptureGuestExperience({
        // Pass facts through unchanged: null (unavailable/failed) stays honest-unknown,
        // while a true empty list still yields a real 0 Active QR count.
        placements: placementsFacts,
        lastJourneyUpdate,
        locationName,
        locationAddress,
      }),
    }
  }

  const fetchCaptureData = async (options: {
    locationId: number
    workspace: OperatorCaptureWorkspaceInput
    isInitialLoad: boolean
  }): Promise<void> => {
    const generation = ++state.captureLoadGeneration
    state = {
      ...state,
      performanceLoadStatus: "loading",
      placementsLoadStatus: "loading",
      ...(options.isInitialLoad
        ? { loadStatus: "loading" as const, viewModel: null }
        : {}),
    }
    publish()

    await scheduleReady()

    const performanceWindow = resolveHomePerformanceWindow(
      adapters.getCapturePerformanceDateRange()
    )
    const from = performanceWindow.from.toISOString()
    const to = performanceWindow.to.toISOString()

    const [performanceSettled, placementsSettled] = await Promise.all([
      adapters
        .getCapturePerformance(options.locationId, from, to)
        .then((response) => ({ ok: true as const, response }))
        .catch(() => ({ ok: false as const })),
      adapters
        .getCapturePlacements(options.locationId, from, to)
        .then((response) => ({ ok: true as const, response }))
        .catch(() => ({ ok: false as const })),
    ])

    if (generation !== state.captureLoadGeneration) {
      return
    }

    const performance = performanceSettled.ok
      ? buildCapturePerformanceKpis(
          factsFromResponse(performanceSettled.response)
        )
      : emptyPerformanceView()

    // On failure keep facts as null (unavailable) rather than [] — an empty
    // array would let the Guest experience count read as a false zero.
    const placementsFacts = placementsSettled.ok
      ? placementsSettled.response.placements
      : null
    const lastJourneyUpdate = placementsSettled.ok
      ? (placementsSettled.response.lastJourneyUpdate ?? null)
      : undefined

    const selectedId = state.placementDetailSelectedQrCodeId
    const detailStillPresent =
      state.placementDetailIsOpen
      && selectedId != null
      && placementsFacts?.some((item) => item.qrCodeId === selectedId) === true

    state = {
      ...state,
      loadStatus: "loaded",
      performanceLoadStatus: performanceSettled.ok ? "loaded" : "error",
      placementsLoadStatus: placementsSettled.ok ? "loaded" : "error",
      viewModel: buildBaseViewModel(
        options.workspace,
        options.locationId,
        performance,
        placementsFacts,
        lastJourneyUpdate
      ),
      placementsFacts,
      lastJourneyUpdate,
      workspace: options.workspace,
      ...(detailStillPresent
        ? {}
        : clearPlacementDetailState()),
    }
    publish()

    if (!performanceSettled.ok) {
      adapters.onPerformanceLoadError?.(PERFORMANCE_LOAD_ERROR_MESSAGE)
    }
    if (!placementsSettled.ok) {
      adapters.onPlacementsLoadError?.(PLACEMENTS_LOAD_ERROR_MESSAGE)
    }
  }

  const loadForWorkspace = async (
    input: OperatorCaptureWorkspaceInput
  ): Promise<void> => {
    const generation = ++state.loadGeneration
    state = {
      ...state,
      loadStatus: "loading",
      isGuestExperiencePreviewOpen: false,
      isGuestExperiencePreviewPickerOpen: false,
      guestExperiencePreviewPlacementLabel: null,
      ...clearPlacementDetailState(),
      viewModel: null,
      workspace: input,
    }
    publish()

    if (input.selectedLocationId == null) {
      if (generation !== state.loadGeneration) {
        return
      }
      // Invalidate any in-flight capture fetch for a prior location.
      state.captureLoadGeneration += 1
      state = {
        ...state,
        loadStatus: "loaded",
        performanceLoadStatus: "idle",
        placementsLoadStatus: "idle",
        isGuestExperiencePreviewOpen: false,
        isGuestExperiencePreviewPickerOpen: false,
        guestExperiencePreviewPlacementLabel: null,
        ...clearPlacementDetailState(),
        viewModel: null,
        placementsFacts: null,
        lastJourneyUpdate: undefined,
        workspace: input,
      }
      publish()
      return
    }

    if (generation !== state.loadGeneration) {
      return
    }

    await fetchCaptureData({
      locationId: input.selectedLocationId,
      workspace: input,
      isInitialLoad: true,
    })
  }

  const requireOpenPlacementDetail = () => {
    const drawer = buildPlacementDetailDrawerSnapshot()
    if (!drawer.isOpen || drawer.details == null) {
      return null
    }
    return drawer.details
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
    async reloadForCapturePerformanceDateRange() {
      const workspace = state.workspace
      const locationId = workspace?.selectedLocationId
      if (workspace == null || locationId == null) {
        return
      }
      await fetchCaptureData({
        locationId,
        workspace,
        isInitialLoad: false,
      })
    },
    async pausePlacement(qrCodeId) {
      const workspace = state.workspace
      const locationId = workspace?.selectedLocationId
      const facts = state.placementsFacts
      const target = facts?.find((item) => item.qrCodeId === qrCodeId)
      if (
        workspace == null ||
        locationId == null ||
        facts == null ||
        target?.status !== "Active"
      ) {
        return "noop"
      }

      try {
        const result = await adapters.pauseCapturePlacement(locationId, qrCodeId)
        const nextFacts = facts.map((item) =>
          item.qrCodeId === result.qrCodeId
            ? { ...item, status: result.status }
            : item
        )
        state = {
          ...state,
          placementsFacts: nextFacts,
          viewModel:
            state.viewModel == null
              ? null
              : buildBaseViewModel(
                  workspace,
                  locationId,
                  state.viewModel.performance,
                  nextFacts,
                  state.lastJourneyUpdate
                ),
        }
        publish()
        return "paused"
      } catch {
        adapters.onPlacementActionError?.(
          "Could not pause QR code. Please try again."
        )
        return "failed"
      }
    },
    async resumePlacement(qrCodeId) {
      const workspace = state.workspace
      const locationId = workspace?.selectedLocationId
      const facts = state.placementsFacts
      const target = facts?.find((item) => item.qrCodeId === qrCodeId)
      if (
        workspace == null ||
        locationId == null ||
        facts == null ||
        target?.status !== "Paused"
      ) {
        return "noop"
      }

      try {
        const result = await adapters.resumeCapturePlacement(locationId, qrCodeId)
        const nextFacts = facts.map((item) =>
          item.qrCodeId === result.qrCodeId
            ? { ...item, status: result.status }
            : item
        )
        state = {
          ...state,
          placementsFacts: nextFacts,
          viewModel:
            state.viewModel == null
              ? null
              : buildBaseViewModel(
                  workspace,
                  locationId,
                  state.viewModel.performance,
                  nextFacts,
                  state.lastJourneyUpdate
                ),
        }
        publish()
        return "resumed"
      } catch {
        adapters.onPlacementActionError?.(
          "Could not resume QR code. Please try again."
        )
        return "failed"
      }
    },
    async copyPlacementLink(qrCodeId) {
      const link = state.placementsFacts?.find(
        (item) => item.qrCodeId === qrCodeId
      )?.qrLinkUrl

      if (link == null) {
        return "noop"
      }

      const result = await adapters.copyText(link)
      if (!result.ok) {
        adapters.onCopyPlacementLinkError?.(result.error)
        return "failed"
      }

      return "copied"
    },
    openGuestExperiencePreview() {
      const entry = state.viewModel?.guestExperience.previewEntry
      if (entry == null || entry.kind === "disabled") {
        return "noop"
      }

      if (entry.kind === "open-picker") {
        state = {
          ...state,
          isGuestExperiencePreviewPickerOpen: true,
          isGuestExperiencePreviewOpen: false,
          guestExperiencePreviewPlacementLabel: null,
        }
        publish()
        return "picker"
      }

      state = {
        ...state,
        isGuestExperiencePreviewOpen: true,
        isGuestExperiencePreviewPickerOpen: false,
        guestExperiencePreviewPlacementLabel:
          entry.kind === "open-preview" ? entry.placementLabel : null,
      }
      publish()
      return "opened"
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
      }
      publish()
    },
    openPlacementDetail(qrCodeId) {
      const fact = state.placementsFacts?.find(
        (item) => item.qrCodeId === qrCodeId
      )
      if (fact == null || state.viewModel == null) {
        return "noop"
      }
      state = {
        ...state,
        placementDetailIsOpen: true,
        placementDetailSelectedQrCodeId: qrCodeId,
        placementDetailDescriptionDraft: fact.internalDescription ?? "",
        placementDetailLastStubbedAction: null,
      }
      publish()
      return "opened"
    },
    closePlacementDetail() {
      if (!state.placementDetailIsOpen) {
        return
      }
      state = {
        ...state,
        ...clearPlacementDetailState(),
      }
      publish()
    },
    setPlacementDetailDescriptionDraft(value) {
      if (!state.placementDetailIsOpen) {
        return
      }
      state = {
        ...state,
        placementDetailDescriptionDraft: value.slice(
          0,
          PLACEMENT_INTERNAL_DESCRIPTION_MAX_LENGTH
        ),
      }
      publish()
    },
    savePlacementDetailDescription() {
      const details = requireOpenPlacementDetail()
      if (details == null) {
        return "noop"
      }
      state = {
        ...state,
        placementDetailLastStubbedAction: "save-description",
      }
      publish()
      return "stubbed"
    },
    requestPlacementDetailPause() {
      const details = requireOpenPlacementDetail()
      if (details == null || details.status !== "Active") {
        return "noop"
      }
      state = {
        ...state,
        placementDetailLastStubbedAction: "pause",
      }
      publish()
      return "stubbed"
    },
    requestPlacementDetailActivate() {
      const details = requireOpenPlacementDetail()
      if (details == null || details.status !== "Paused") {
        return "noop"
      }
      state = {
        ...state,
        placementDetailLastStubbedAction: "activate",
      }
      publish()
      return "stubbed"
    },
    requestPlacementDetailRotate() {
      const details = requireOpenPlacementDetail()
      if (details == null || !details.canRotate) {
        return "noop"
      }
      state = {
        ...state,
        placementDetailLastStubbedAction: "rotate",
      }
      publish()
      return "stubbed"
    },
    requestPlacementDetailArchive() {
      const details = requireOpenPlacementDetail()
      if (details == null) {
        return "noop"
      }
      state = {
        ...state,
        placementDetailLastStubbedAction: "archive",
      }
      publish()
      return "stubbed"
    },
    async copyPlacementDetailLink() {
      const details = requireOpenPlacementDetail()
      if (details == null) {
        return "noop"
      }
      const link = state.placementsFacts?.find(
        (item) => item.qrCodeId === details.qrCodeId
      )?.qrLinkUrl

      if (link == null) {
        return "noop"
      }

      const result = await adapters.copyText(link)
      if (!result.ok) {
        adapters.onCopyPlacementLinkError?.(result.error)
        return "failed"
      }

      return "copied"
    },
    openPlacementDetailPreview() {
      const details = requireOpenPlacementDetail()
      if (details == null || state.viewModel == null) {
        return "noop"
      }
      state = {
        ...state,
        isGuestExperiencePreviewOpen: true,
        guestExperiencePreviewPlacementLabel: details.title,
      }
      publish()
      return "opened"
    },
  }
}


import {
  buildCaptureDigitalGuestLinks,
  type OperatorCaptureDigitalGuestLinkRow,
} from "@/lib/operatorCapture/buildCaptureDigitalGuestLinks"
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
  buildPauseActivateConfirm,
  type PauseActivateConfirmView,
} from "@/lib/operatorCapture/buildPauseActivateConfirm"
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
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type {
  CapturePerformanceResponse,
  CapturePlacementItem,
  CapturePlacementsResponse,
  CapturePlacementStatus,
  CreateDigitalGuestLinkRequest,
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
  | "archive"
  | "save-description"

export type PlacementDetailDrawerSnapshot = {
  isOpen: boolean
  selectedQrCodeId: number | null
  details: PlacementDetailDrawerView | null
  lastStubbedAction: PlacementDetailStubbedAction | null
}

export type PlacementRotateConfirmSnapshot = {
  isOpen: boolean
  qrCodeId: number | null
  placementLabel: string
  locationName: string
  status: CapturePlacementStatus | null
  lastScanText: string
  printMaterialsAcknowledged: boolean
  canConfirm: boolean
}

export type PauseActivateConfirmSnapshot = {
  isOpen: boolean
  details: PauseActivateConfirmView | null
}

export type ConfirmPauseActivateResult =
  | { outcome: "paused" | "activated"; toastMessage: string }
  | "failed"
  | "noop"


export type OperatorCapturePerformanceView = {
  kpis: OperatorCaptureKpi[]
  isEmpty: boolean
}


export type OperatorCapturePlacementsView = {
  rows: OperatorCapturePlacementRow[]
  isEmpty: boolean
}

export type OperatorCaptureDigitalGuestLinksView = {
  rows: OperatorCaptureDigitalGuestLinkRow[]
  isEmpty: boolean
}

export type CreateDigitalGuestLinkModuleInput = CreateDigitalGuestLinkRequest

export type CreateDigitalGuestLinkAdapterResult =
  | { ok: true; qrCodeId: number }
  | {
      ok: false
      reason: "duplicate_link_name"
      message: string
    }
  | { ok: false; reason: "failed"; message: string }

export type CreateDigitalGuestLinkModuleResult =
  | "created"
  | "duplicate_link_name"
  | "failed"
  | "noop"

export type RequestDigitalGuestLinkArchiveResult = "stubbed" | "noop"

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
  rotateConfirm: PlacementRotateConfirmSnapshot
  pauseActivateConfirm: PauseActivateConfirmSnapshot
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
  rotateCapturePlacement: (
    locationId: number,
    qrCodeId: number
  ) => Promise<{
    qrCodeId: number
    status: CapturePlacementStatus
    qrLinkUrl: string
  }>
  createDigitalGuestLink: (
    locationId: number,
    input: CreateDigitalGuestLinkModuleInput
  ) => Promise<CreateDigitalGuestLinkAdapterResult>
  copyText: (
    text: string
  ) => Promise<{ ok: true } | { ok: false; error: string }>
  getCapturePerformanceDateRange: () => HomePerformanceDateRange
  onPerformanceLoadError?: (message: string) => void
  onPlacementsLoadError?: (message: string) => void
  onPlacementActionError?: (message: string) => void
  onCopyPlacementLinkError?: (message: string) => void
  onCreateDigitalGuestLinkError?: (message: string) => void
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
  createDigitalGuestLink: (
    input: CreateDigitalGuestLinkModuleInput
  ) => Promise<CreateDigitalGuestLinkModuleResult>
  requestDigitalGuestLinkArchive: (
    qrCodeId: number
  ) => RequestDigitalGuestLinkArchiveResult
  openPlacementPreview: (qrCodeId: number) => "opened" | "noop"
  openGuestExperiencePreview: () => OpenGuestExperiencePreviewResult
  closeGuestExperiencePreview: () => void
  closeGuestExperiencePreviewPicker: () => void
  openPlacementDetail: (qrCodeId: number) => "opened" | "noop"
  closePlacementDetail: () => void
  setPlacementDetailDescriptionDraft: (value: string) => void
  savePlacementDetailDescription: () => "stubbed" | "noop"
  requestPauseConfirm: (qrCodeId: number) => "opened" | "noop"
  requestActivateConfirm: (qrCodeId: number) => "opened" | "noop"
  cancelPauseActivateConfirm: () => void
  confirmPauseActivate: () => Promise<ConfirmPauseActivateResult>
  requestPlacementDetailPause: () => "opened" | "noop"
  requestPlacementDetailActivate: () => "opened" | "noop"
  requestRotate: (qrCodeId: number) => "opened" | "noop"
  requestPlacementDetailRotate: () => "opened" | "noop"
  setRotatePrintMaterialsAcknowledged: (acknowledged: boolean) => void
  cancelRotateConfirm: () => void
  confirmRotate: () => Promise<"rotated" | "failed" | "noop">
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
  rotateConfirmQrCodeId: number | null
  rotatePrintMaterialsAcknowledged: boolean
  pauseActivateConfirmIsOpen: boolean
  pauseActivateConfirmDetails: PauseActivateConfirmView | null
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

const ROTATE_ACTION_ERROR_MESSAGE =
  "Could not rotate QR code. Please try again."

function canRotatePlacement(fact: CapturePlacementItem): boolean {
  return (
    fact.qrType !== "DigitalGuestLink" &&
    (fact.status === "Active" || fact.status === "Paused")
  )
}

function closedRotateConfirm(): PlacementRotateConfirmSnapshot {
  return {
    isOpen: false,
    qrCodeId: null,
    placementLabel: "",
    locationName: "",
    status: null,
    lastScanText: "",
    printMaterialsAcknowledged: false,
    canConfirm: false,
  }
}


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
    rotateConfirmQrCodeId: null,
    rotatePrintMaterialsAcknowledged: false,
    pauseActivateConfirmIsOpen: false,
    pauseActivateConfirmDetails: null,
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

  const buildRotateConfirmSnapshot = (): PlacementRotateConfirmSnapshot => {
    const qrCodeId = state.rotateConfirmQrCodeId
    if (
      qrCodeId == null ||
      state.placementsFacts == null ||
      state.viewModel == null
    ) {
      return closedRotateConfirm()
    }

    const fact = state.placementsFacts.find((item) => item.qrCodeId === qrCodeId)
    if (fact == null || !canRotatePlacement(fact)) {
      return closedRotateConfirm()
    }

    const details = buildPlacementDetailDrawer({
      fact,
      locationName: state.viewModel.locationName,
      descriptionDraft: "",
      nowMs: nowMs(),
    })
    const acknowledged = state.rotatePrintMaterialsAcknowledged

    return {
      isOpen: true,
      qrCodeId,
      placementLabel: details.title,
      locationName: state.viewModel.locationName,
      status: fact.status,
      lastScanText:
        fact.lastScanAt == null || fact.lastScanAt === ""
          ? "—"
          : formatRelativeTime(fact.lastScanAt, nowMs()) || "—",
      printMaterialsAcknowledged: acknowledged,
      canConfirm: acknowledged,
    }
  }

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

  const closedPauseActivateConfirm = (): PauseActivateConfirmSnapshot => ({
    isOpen: false,
    details: null,
  })

  const buildPauseActivateConfirmSnapshot = (): PauseActivateConfirmSnapshot => {
    if (!state.pauseActivateConfirmIsOpen || state.pauseActivateConfirmDetails == null) {
      return closedPauseActivateConfirm()
    }
    return {
      isOpen: true,
      details: state.pauseActivateConfirmDetails,
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
    rotateConfirm: closedRotateConfirm(),
    pauseActivateConfirm: closedPauseActivateConfirm(),
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
      rotateConfirm: buildRotateConfirmSnapshot(),
      pauseActivateConfirm: buildPauseActivateConfirmSnapshot(),
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

  const clearRotateConfirmState = () => ({
    rotateConfirmQrCodeId: null as number | null,
    rotatePrintMaterialsAcknowledged: false as const,
  })

  const clearPauseActivateConfirmState = () => ({
    pauseActivateConfirmIsOpen: false as const,
    pauseActivateConfirmDetails: null,
  })

  const openRotateConfirmFor = (qrCodeId: number): "opened" | "noop" => {
    const fact = state.placementsFacts?.find((item) => item.qrCodeId === qrCodeId)
    if (
      fact == null ||
      state.viewModel == null ||
      !canRotatePlacement(fact)
    ) {
      return "noop"
    }
    state = {
      ...state,
      rotateConfirmQrCodeId: qrCodeId,
      rotatePrintMaterialsAcknowledged: false,
    }
    publish()
    return "opened"
  }

  const openPauseConfirmFor = (qrCodeId: number): "opened" | "noop" => {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    const fact = state.placementsFacts?.find(
      (item) => item.qrCodeId === qrCodeId
    )
    if (
      workspace == null
      || locationId == null
      || fact == null
      || fact.status !== "Active"
      || state.viewModel == null
    ) {
      return "noop"
    }

    state = {
      ...state,
      pauseActivateConfirmIsOpen: true,
      pauseActivateConfirmDetails: buildPauseActivateConfirm({
        fact,
        action: "pause",
        locationName: state.viewModel.locationName,
        nowMs: nowMs(),
      }),
    }
    publish()
    return "opened"
  }

  const openActivateConfirmFor = (qrCodeId: number): "opened" | "noop" => {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    const fact = state.placementsFacts?.find(
      (item) => item.qrCodeId === qrCodeId
    )
    if (
      workspace == null
      || locationId == null
      || fact == null
      || fact.status !== "Paused"
      || state.viewModel == null
    ) {
      return "noop"
    }

    state = {
      ...state,
      pauseActivateConfirmIsOpen: true,
      pauseActivateConfirmDetails: buildPauseActivateConfirm({
        fact,
        action: "activate",
        locationName: state.viewModel.locationName,
        nowMs: nowMs(),
      }),
    }
    publish()
    return "opened"
  }

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
      digitalGuestLinks: buildCaptureDigitalGuestLinks(
        placementsFacts ?? [],
        nowMs()
      ),
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
        : {
            ...clearPlacementDetailState(),
            ...clearPauseActivateConfirmState(),
          }),
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
      ...clearRotateConfirmState(),
      ...clearPauseActivateConfirmState(),
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
        ...clearRotateConfirmState(),
        ...clearPauseActivateConfirmState(),
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
      return openPauseConfirmFor(details.qrCodeId)
    },
    requestPlacementDetailActivate() {
      const details = requireOpenPlacementDetail()
      if (details == null || details.status !== "Paused") {
        return "noop"
      }
      return openActivateConfirmFor(details.qrCodeId)
    },
    requestPauseConfirm(qrCodeId) {
      return openPauseConfirmFor(qrCodeId)
    },
    requestActivateConfirm(qrCodeId) {
      return openActivateConfirmFor(qrCodeId)
    },
    cancelPauseActivateConfirm() {
      if (!state.pauseActivateConfirmIsOpen) {
        return
      }
      state = {
        ...state,
        ...clearPauseActivateConfirmState(),
      }
      publish()
    },
    async confirmPauseActivate() {
      const confirm = state.pauseActivateConfirmDetails
      const workspace = state.workspace
      const locationId = workspace?.selectedLocationId
      const facts = state.placementsFacts
      if (
        !state.pauseActivateConfirmIsOpen
        || confirm == null
        || workspace == null
        || locationId == null
        || facts == null
      ) {
        return "noop"
      }

      const target = facts.find((item) => item.qrCodeId === confirm.qrCodeId)
      if (target == null) {
        state = {
          ...state,
          ...clearPauseActivateConfirmState(),
        }
        publish()
        return "noop"
      }

      if (confirm.action === "pause" && target.status !== "Active") {
        state = {
          ...state,
          ...clearPauseActivateConfirmState(),
        }
        publish()
        return "noop"
      }
      if (confirm.action === "activate" && target.status !== "Paused") {
        state = {
          ...state,
          ...clearPauseActivateConfirmState(),
        }
        publish()
        return "noop"
      }

      const toastMessage = confirm.successToastMessage
      const action = confirm.action

      try {
        const result =
          action === "pause"
            ? await adapters.pauseCapturePlacement(locationId, confirm.qrCodeId)
            : await adapters.resumeCapturePlacement(locationId, confirm.qrCodeId)

        const nextFacts = facts.map((item) =>
          item.qrCodeId === result.qrCodeId
            ? { ...item, status: result.status }
            : item
        )
        const updatedFact = nextFacts.find(
          (item) => item.qrCodeId === result.qrCodeId
        )

        state = {
          ...state,
          ...clearPauseActivateConfirmState(),
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
          placementDetailIsOpen: true,
          placementDetailSelectedQrCodeId: result.qrCodeId,
          placementDetailDescriptionDraft:
            updatedFact?.internalDescription ?? "",
          placementDetailLastStubbedAction: null,
        }
        publish()

        return {
          outcome: action === "pause" ? "paused" : "activated",
          toastMessage,
        }
      } catch {
        state = {
          ...state,
          ...clearPauseActivateConfirmState(),
        }
        publish()
        adapters.onPlacementActionError?.(
          action === "pause"
            ? "Could not pause QR code. Please try again."
            : "Could not activate QR code. Please try again."
        )
        return "failed"
      }
    },
    requestPlacementDetailRotate() {
      const details = requireOpenPlacementDetail()
      if (details == null || !details.canRotate) {
        return "noop"
      }
      return openRotateConfirmFor(details.qrCodeId)
    },
    requestRotate(qrCodeId) {
      return openRotateConfirmFor(qrCodeId)
    },
    setRotatePrintMaterialsAcknowledged(acknowledged) {
      if (state.rotateConfirmQrCodeId == null) {
        return
      }
      state = {
        ...state,
        rotatePrintMaterialsAcknowledged: acknowledged,
      }
      publish()
    },
    cancelRotateConfirm() {
      if (state.rotateConfirmQrCodeId == null) {
        return
      }
      state = {
        ...state,
        ...clearRotateConfirmState(),
      }
      publish()
    },
    async confirmRotate() {
      const workspace = state.workspace
      const locationId = workspace?.selectedLocationId
      const facts = state.placementsFacts
      const qrCodeId = state.rotateConfirmQrCodeId
      const target = facts?.find((item) => item.qrCodeId === qrCodeId)
      if (
        workspace == null ||
        locationId == null ||
        facts == null ||
        qrCodeId == null ||
        target == null ||
        !canRotatePlacement(target) ||
        !state.rotatePrintMaterialsAcknowledged
      ) {
        return "noop"
      }

      try {
        const result = await adapters.rotateCapturePlacement(
          locationId,
          qrCodeId
        )
        const nextFacts = facts.map((item) =>
          item.qrCodeId === result.qrCodeId
            ? {
                ...item,
                status: result.status,
                qrLinkUrl: result.qrLinkUrl,
              }
            : item
        )
        const fact = nextFacts.find((item) => item.qrCodeId === result.qrCodeId)
        state = {
          ...state,
          ...clearRotateConfirmState(),
          placementsFacts: nextFacts,
          placementDetailIsOpen: true,
          placementDetailSelectedQrCodeId: result.qrCodeId,
          placementDetailDescriptionDraft:
            state.placementDetailSelectedQrCodeId === result.qrCodeId
              ? state.placementDetailDescriptionDraft
              : (fact?.internalDescription ?? ""),
          placementDetailLastStubbedAction: null,
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
        return "rotated"
      } catch {
        adapters.onPlacementActionError?.(ROTATE_ACTION_ERROR_MESSAGE)
        return "failed"
      }
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
    openPlacementPreview(qrCodeId) {
      const fact = state.placementsFacts?.find(
        (item) => item.qrCodeId === qrCodeId
      )
      if (fact == null || state.viewModel == null) {
        return "noop"
      }
      const label =
        fact.qrType === "DigitalGuestLink"
          && fact.linkName != null
          && fact.linkName.trim() !== ""
          ? fact.linkName.trim()
          : fact.qrType === "SmartGuest"
            ? "Smart Guest"
            : fact.qrType
      state = {
        ...state,
        isGuestExperiencePreviewOpen: true,
        guestExperiencePreviewPlacementLabel: label,
      }
      publish()
      return "opened"
    },
    async createDigitalGuestLink(input) {
      if (state.viewModel == null || state.workspace == null) {
        return "noop"
      }

      const locationId = state.viewModel.locationId
      const workspace = state.workspace
      let result: CreateDigitalGuestLinkAdapterResult
      try {
        result = await adapters.createDigitalGuestLink(locationId, input)
      } catch {
        adapters.onCreateDigitalGuestLinkError?.(
          "Could not create digital guest link. Please try again."
        )
        return "failed"
      }

      if (!result.ok) {
        if (result.reason === "duplicate_link_name") {
          return "duplicate_link_name"
        }
        adapters.onCreateDigitalGuestLinkError?.(result.message)
        return "failed"
      }

      const createdQrCodeId = result.qrCodeId

      await fetchCaptureData({
        locationId,
        workspace,
        isInitialLoad: false,
      })

      const fact = state.placementsFacts?.find(
        (item) => item.qrCodeId === createdQrCodeId
      )
      if (fact == null || state.viewModel == null) {
        return "failed"
      }

      state = {
        ...state,
        placementDetailIsOpen: true,
        placementDetailSelectedQrCodeId: createdQrCodeId,
        placementDetailDescriptionDraft: fact.internalDescription ?? "",
        placementDetailLastStubbedAction: null,
      }
      publish()
      return "created"
    },
    requestDigitalGuestLinkArchive(qrCodeId) {
      const fact = state.placementsFacts?.find(
        (item) =>
          item.qrCodeId === qrCodeId && item.qrType === "DigitalGuestLink"
      )
      if (fact == null) {
        return "noop"
      }
      return "stubbed"
    },
  }
}


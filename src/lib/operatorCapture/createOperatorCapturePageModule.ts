import type {
  OperatorCaptureArchiveRow,
} from "@/lib/operatorCapture/buildCaptureArchive"
import {
  createCaptureArchiveModule,
  type CaptureArchiveModule,
  type ConfirmRestoreResult,
} from "@/lib/operatorCapture/createCaptureArchiveModule"
import {
  createCapturePlacementDetailModule,
  type CapturePlacementDetailModule,
  type PlacementDetailFact,
} from "@/lib/operatorCapture/createCapturePlacementDetailModule"
import {
  buildCaptureDigitalGuestLinks,
  type OperatorCaptureDigitalGuestLinkRow,
} from "@/lib/operatorCapture/buildCaptureDigitalGuestLinks"
import {
  buildCaptureGuestExperience,
  type OperatorCaptureGuestExperienceView,
} from "@/lib/operatorCapture/buildCaptureGuestExperience"
import {
  buildGuestExperiencePreviewPicker,
  type GuestExperiencePreviewPickerView,
} from "@/lib/operatorCapture/buildGuestExperiencePreviewPicker"
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
import { buildPlacementDetailDrawer } from "@/lib/operatorCapture/buildPlacementDetailDrawer"
import {
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type {
  CaptureArchivedPlacementItem,
  CaptureArchivedPlacementsResponse,
  CaptureLocationStatus,
  CapturePerformanceResponse,
  CapturePlacementItem,
  CapturePlacementsResponse,
  CapturePlacementStatus,
  CaptureQrCodeStatus,
  CreateDigitalGuestLinkRequest,
} from "@/types/dashboard"

export type {
  ConfirmRestoreResult,
  CreateDigitalGuestLinkPrefill,
  OperatorCaptureArchiveView,
  RestoreConfirmSnapshot,
} from "@/lib/operatorCapture/createCaptureArchiveModule"

export type {
  PlacementDetailDrawerSnapshot,
  PlacementDetailFact,
} from "@/lib/operatorCapture/createCapturePlacementDetailModule"

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

export type ArchivePlacementResult =
  | { outcome: "archived"; toastMessage: string }
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

export type CreateDigitalGuestLinkModuleInput = CreateDigitalGuestLinkRequest & {
  /** When set (Archive Duplicate as new), creates at this location. */
  locationId?: number
}

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

export type { OperatorCaptureGuestExperienceView }
export type { OperatorCaptureArchiveRow }

export type OpenGuestExperiencePreviewResult =
  | "opened"
  | "picker"
  | "noop"

export type GuestExperiencePreviewPickerSnapshot =
  GuestExperiencePreviewPickerView & {
    isOpen: boolean
  }

export type OperatorCaptureViewModel = {
  locationId: number
  locationName: string
  dateRangeLabel: string
  /** Persisted Capture location status from placements load. */
  captureLocationStatus: CaptureLocationStatus
  /** True while location is Paused — per-code Pause/Activate locked. */
  perCodePauseActivateLocked: boolean
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
  guestExperiencePreviewPicker: GuestExperiencePreviewPickerSnapshot
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
  getArchivedCapturePlacements: () => Promise<CaptureArchivedPlacementsResponse>
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
  archiveCapturePlacement: (
    locationId: number,
    qrCodeId: number
  ) => Promise<{
    qrCodeId: number
    status: "Archived"
    archivedAt: string
    archivedByDisplayName: string | null
  }>
  restoreCapturePlacement: (
    locationId: number,
    qrCodeId: number
  ) => Promise<
    | { ok: true; qrCodeId: number; status: "Paused"; qrLinkUrl: string }
    | { ok: false; reason: "conflict" | "failed"; message: string }
  >
  createDigitalGuestLink: (
    locationId: number,
    input: CreateDigitalGuestLinkModuleInput
  ) => Promise<CreateDigitalGuestLinkAdapterResult>
  updatePlacementInternalDescription: (
    locationId: number,
    qrCodeId: number,
    internalDescription: string | null
  ) => Promise<{
    qrCodeId: number
    internalDescription: string | null
    updatedAt: string
    updatedByDisplayName: string | null
  }>
  copyText: (
    text: string
  ) => Promise<{ ok: true } | { ok: false; error: string }>
  getCapturePerformanceDateRange: () => HomePerformanceDateRange
  onPerformanceLoadError?: (message: string) => void
  onPlacementsLoadError?: (message: string) => void
  onArchiveLoadError?: (message: string) => void
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
  /** Internal Capture Archive module — subscribe separately; does not relay into live publish. */
  getArchiveModule: () => CaptureArchiveModule
  /** Internal Capture Placement Detail module — subscribe separately; does not relay into live publish. */
  getPlacementDetailModule: () => CapturePlacementDetailModule
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
  archivePlacement: (qrCodeId: number) => Promise<ArchivePlacementResult>
  requestDigitalGuestLinkArchive: (
    qrCodeId: number
  ) => Promise<ArchivePlacementResult>
  openPlacementPreview: (qrCodeId: number) => "opened" | "noop"
  openGuestExperiencePreview: () => OpenGuestExperiencePreviewResult
  closeGuestExperiencePreview: () => void
  closeGuestExperiencePreviewPicker: () => void
  selectGuestExperiencePreviewPickerOption: (
    qrCodeId: number | null
  ) => "selected" | "noop"
  confirmGuestExperiencePreviewPicker: () => "opened" | "noop"
  openPlacementDetail: (qrCodeId: number) => "opened" | "noop"
  closePlacementDetail: () => void
  setPlacementDetailDescriptionDraft: (value: string) => void
  savePlacementDetailDescription: () => Promise<"saved" | "failed" | "noop">
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
  requestPlacementDetailArchive: () => Promise<ArchivePlacementResult>
  copyPlacementDetailLink: () => Promise<CopyCapturePlacementLinkResult>
  openPlacementDetailPreview: () => "opened" | "noop"
  /** Opens Placement Detail from an archived row via the Detail module. */
  openArchivePlacementDetail: (qrCodeId: number) => "opened" | "noop"
  /**
   * Restore confirm on the Archive module, then opens Placement Detail when
   * restore succeeds.
   */
  confirmRestore: () => Promise<ConfirmRestoreResult>
}


type ModuleState = {
  loadStatus: OperatorCapturePageSnapshot["loadStatus"]
  performanceLoadStatus: OperatorCapturePageSnapshot["performanceLoadStatus"]
  placementsLoadStatus: OperatorCapturePageSnapshot["placementsLoadStatus"]
  isGuestExperiencePreviewOpen: boolean
  isGuestExperiencePreviewPickerOpen: boolean
  guestExperiencePreviewPlacementLabel: string | null
  guestExperiencePreviewPickerSelectedQrCodeId: number | null
  rotateConfirmQrCodeId: number | null
  rotatePrintMaterialsAcknowledged: boolean
  pauseActivateConfirmIsOpen: boolean
  pauseActivateConfirmDetails: PauseActivateConfirmView | null
  viewModel: OperatorCaptureViewModel | null
  placementsFacts: CapturePlacementsResponse["placements"] | null
  captureLocationStatus: CaptureLocationStatus
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

const ARCHIVE_ACTION_ERROR_MESSAGE =
  "Could not archive QR code. Please try again."

function placementDisplayName(fact: {
  qrType: CapturePlacementItem["qrType"]
  linkName?: string | null
}): string {
  if (
    fact.qrType === "DigitalGuestLink"
    && fact.linkName != null
    && fact.linkName.trim() !== ""
  ) {
    return fact.linkName.trim()
  }
  const labels: Record<CapturePlacementItem["qrType"], string> = {
    CounterCard: "Counter card",
    PackagingSticker: "Packaging sticker",
    DeliveryInsert: "Delivery insert",
    WindowSticker: "Window sticker",
    SmartGuest: "Smart Guest",
    DigitalGuestLink: "Digital guest link",
  }
  return labels[fact.qrType]
}

function archivedItemToDetailFact(
  fact: CaptureArchivedPlacementItem
): PlacementDetailFact {
  return {
    qrCodeId: fact.qrCodeId,
    qrType: fact.qrType,
    status: "Archived",
    linkName: fact.linkName,
    channel: fact.channel,
    internalDescription: fact.internalDescription,
    qrLinkUrl: fact.qrLinkUrl,
    qrScans: fact.qrScans,
    feedbackSubmitted: fact.feedbackSubmitted,
    marketingOptIns: 0,
    offerClaims: 0,
    lastScanAt: fact.lastScanAt,
  }
}

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

  const archiveModule = createCaptureArchiveModule({
    getArchivedCapturePlacements: adapters.getArchivedCapturePlacements,
    restoreCapturePlacement: adapters.restoreCapturePlacement,
    onArchiveLoadError: adapters.onArchiveLoadError,
    onPlacementActionError: adapters.onPlacementActionError,
    nowMs,
  })

  const detailModule = createCapturePlacementDetailModule({ nowMs })

  let state: ModuleState = {
    loadStatus: "idle",
    performanceLoadStatus: "idle",
    placementsLoadStatus: "idle",
    isGuestExperiencePreviewOpen: false,
    isGuestExperiencePreviewPickerOpen: false,
    guestExperiencePreviewPlacementLabel: null,
    guestExperiencePreviewPickerSelectedQrCodeId: null,
    rotateConfirmQrCodeId: null,
    rotatePrintMaterialsAcknowledged: false,
    pauseActivateConfirmIsOpen: false,
    pauseActivateConfirmDetails: null,
    viewModel: null,
    placementsFacts: null,
    captureLocationStatus: "Active",
    lastJourneyUpdate: undefined,
    workspace: null,
    loadGeneration: 0,
    captureLoadGeneration: 0,
  }

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
      locationCapturePaused: state.captureLocationStatus === "Paused",
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

  const closedPauseActivateConfirm = (): PauseActivateConfirmSnapshot => ({
    isOpen: false,
    details: null,
  })

  const closedGuestExperiencePreviewPicker =
    (): GuestExperiencePreviewPickerSnapshot => ({
      isOpen: false,
      groups: [],
      selectedQrCodeId: null,
      selectedLabel: null,
      canConfirm: false,
    })

  const buildGuestExperiencePreviewPickerSnapshot =
    (): GuestExperiencePreviewPickerSnapshot => {
      if (
        !state.isGuestExperiencePreviewPickerOpen ||
        state.placementsFacts == null
      ) {
        return closedGuestExperiencePreviewPicker()
      }
      return {
        isOpen: true,
        ...buildGuestExperiencePreviewPicker({
          placements: state.placementsFacts,
          selectedQrCodeId: state.guestExperiencePreviewPickerSelectedQrCodeId,
        }),
      }
    }

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
    guestExperiencePreviewPicker: closedGuestExperiencePreviewPicker(),
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
      guestExperiencePreviewPicker: buildGuestExperiencePreviewPickerSnapshot(),
      rotateConfirm: buildRotateConfirmSnapshot(),
      pauseActivateConfirm: buildPauseActivateConfirmSnapshot(),
      viewModel: state.viewModel,
    }
    for (const listener of listeners) {
      listener()
    }
  }

  const clearRotateConfirmState = () => ({
    rotateConfirmQrCodeId: null as number | null,
    rotatePrintMaterialsAcknowledged: false as const,
  })

  const clearPauseActivateConfirmState = () => ({
    pauseActivateConfirmIsOpen: false as const,
    pauseActivateConfirmDetails: null,
  })

  const resolveDetailFact = (
    qrCodeId: number
  ): PlacementDetailFact | CapturePlacementItem | null => {
    const fromLive = state.placementsFacts?.find(
      (item) => item.qrCodeId === qrCodeId
    )
    if (fromLive != null) {
      return fromLive
    }
    const open = detailModule.getOpenContext()
    if (open.fact != null && open.fact.qrCodeId === qrCodeId) {
      return open.fact
    }
    return null
  }

  const resolveDetailLocationId = (): number | null =>
    detailModule.getOpenContext().locationId
    ?? state.workspace?.selectedLocationId
    ?? null

  const resolveDetailLocationName = (locationId: number | null): string => {
    if (state.viewModel != null) {
      return state.viewModel.locationName
    }
    if (locationId == null) {
      return ""
    }
    const archived = archiveModule
      .getSnapshot()
      .archive?.locationOptions.find((location) => location.id === locationId)
    return archived?.label ?? ""
  }

  /** Opens Placement Detail. `archiveLocationId` set → archive-sourced row; null → live Capture. */
  const openPlacementDetailForFact = (
    fact: PlacementDetailFact,
    options?: {
      archiveLocationId?: number | null
      descriptionDraft?: string
      locationName?: string
    }
  ) => {
    const archiveLocationId =
      options?.archiveLocationId !== undefined
        ? options.archiveLocationId
        : detailModule.getOpenContext().locationId
    const locationName =
      options?.locationName
      ?? (archiveLocationId != null
        ? resolveDetailLocationName(archiveLocationId)
        : (state.viewModel?.locationName ?? FALLBACK_LOCATION_NAME))
    if (archiveLocationId != null) {
      detailModule.openFromArchive({
        fact,
        locationId: archiveLocationId,
        locationName: locationName || FALLBACK_LOCATION_NAME,
      })
    } else {
      detailModule.openFromLive({
        fact,
        locationName: locationName || FALLBACK_LOCATION_NAME,
        locationCapturePaused: state.captureLocationStatus === "Paused",
      })
    }
    if (options?.descriptionDraft != null) {
      detailModule.setDescriptionDraft(options.descriptionDraft)
    }
  }

  const openRotateConfirmFor = (qrCodeId: number): "opened" | "noop" => {
    const fact = resolveDetailFact(qrCodeId)
    if (fact == null || !canRotatePlacement(fact as CapturePlacementItem)) {
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
    const locationId = resolveDetailLocationId()
    const fact = resolveDetailFact(qrCodeId)
    if (
      locationId == null
      || fact == null
      || fact.status !== "Active"
      || state.captureLocationStatus === "Paused"
    ) {
      return "noop"
    }

    state = {
      ...state,
      pauseActivateConfirmIsOpen: true,
      pauseActivateConfirmDetails: buildPauseActivateConfirm({
        fact: fact as CapturePlacementItem,
        action: "pause",
        locationName: resolveDetailLocationName(locationId),
        nowMs: nowMs(),
      }),
    }
    publish()
    return "opened"
  }

  const openActivateConfirmFor = (qrCodeId: number): "opened" | "noop" => {
    const locationId = resolveDetailLocationId()
    const fact = resolveDetailFact(qrCodeId)
    if (
      locationId == null
      || fact == null
      || fact.status !== "Paused"
      || state.captureLocationStatus === "Paused"
    ) {
      return "noop"
    }

    state = {
      ...state,
      pauseActivateConfirmIsOpen: true,
      pauseActivateConfirmDetails: buildPauseActivateConfirm({
        fact: fact as CapturePlacementItem,
        action: "activate",
        locationName: resolveDetailLocationName(locationId),
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
    lastJourneyUpdate: CapturePlacementsResponse["lastJourneyUpdate"] | undefined,
    captureLocationStatus: CaptureLocationStatus
  ): OperatorCaptureViewModel => {
    const locationName = resolveLocationName(input, locationId)
    const locationAddress = resolveLocationAddress(input, locationId)

    return {
      locationId,
      locationName,
      dateRangeLabel: currentDateRangeLabel(),
      captureLocationStatus,
      perCodePauseActivateLocked: captureLocationStatus === "Paused",
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
    const captureLocationStatus = placementsSettled.ok
      ? placementsSettled.response.captureLocationStatus
      : state.captureLocationStatus

    const openDetail = detailModule.getOpenContext()
    const selectedId = openDetail.qrCodeId
    const detailStillPresent =
      openDetail.isOpen
      && selectedId != null
      && openDetail.locationId == null
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
        lastJourneyUpdate,
        captureLocationStatus
      ),
      placementsFacts,
      captureLocationStatus,
      lastJourneyUpdate,
      workspace: options.workspace,
      ...(detailStillPresent ? {} : clearPauseActivateConfirmState()),
    }

    if (detailStillPresent) {
      const fact = placementsFacts?.find((item) => item.qrCodeId === selectedId)
      if (fact != null) {
        detailModule.patchFact({
          fact,
          locationName:
            state.viewModel?.locationName ?? FALLBACK_LOCATION_NAME,
          locationCapturePaused: captureLocationStatus === "Paused",
        })
      }
    } else if (openDetail.locationId == null) {
      detailModule.reset()
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
      guestExperiencePreviewPickerSelectedQrCodeId: null,
      ...clearRotateConfirmState(),
      ...clearPauseActivateConfirmState(),
      viewModel: null,
      workspace: input,
    }
    detailModule.reset()
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
        guestExperiencePreviewPickerSelectedQrCodeId: null,
        ...clearRotateConfirmState(),
        ...clearPauseActivateConfirmState(),
        viewModel: null,
        placementsFacts: null,
        captureLocationStatus: "Active",
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
    const drawer = detailModule.getSnapshot()
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
    getArchiveModule() {
      return archiveModule
    },
    getPlacementDetailModule() {
      return detailModule
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
                  state.lastJourneyUpdate,
                  state.captureLocationStatus
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
                  state.lastJourneyUpdate,
                  state.captureLocationStatus
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
          guestExperiencePreviewPickerSelectedQrCodeId: null,
        }
        publish()
        return "picker"
      }

      state = {
        ...state,
        isGuestExperiencePreviewOpen: true,
        isGuestExperiencePreviewPickerOpen: false,
        guestExperiencePreviewPickerSelectedQrCodeId: null,
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
        guestExperiencePreviewPickerSelectedQrCodeId: null,
      }
      publish()
    },
    selectGuestExperiencePreviewPickerOption(qrCodeId) {
      if (!state.isGuestExperiencePreviewPickerOpen) {
        return "noop"
      }
      const picker = buildGuestExperiencePreviewPickerSnapshot()
      if (
        qrCodeId != null &&
        !picker.groups.some((group) =>
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
      const picker = buildGuestExperiencePreviewPickerSnapshot()
      if (!picker.isOpen || !picker.canConfirm || picker.selectedQrCodeId == null) {
        return "noop"
      }
      state = {
        ...state,
        isGuestExperiencePreviewPickerOpen: false,
        guestExperiencePreviewPickerSelectedQrCodeId: null,
        isGuestExperiencePreviewOpen: true,
        guestExperiencePreviewPlacementLabel: picker.selectedLabel,
      }
      publish()
      return "opened"
    },
    openPlacementDetail(qrCodeId) {
      const fact = state.placementsFacts?.find(
        (item) => item.qrCodeId === qrCodeId
      )
      if (fact == null || state.viewModel == null) {
        return "noop"
      }
      openPlacementDetailForFact(fact, { archiveLocationId: null })
      return "opened"
    },
    closePlacementDetail() {
      detailModule.close()
    },
    setPlacementDetailDescriptionDraft(value) {
      detailModule.setDescriptionDraft(value)
    },
    async savePlacementDetailDescription() {
      const details = requireOpenPlacementDetail()
      const locationId = resolveDetailLocationId()
      if (details == null || locationId == null) {
        return "noop"
      }
      if (
        details.status !== "Active"
        && details.status !== "Paused"
      ) {
        return "noop"
      }

      const draft = detailModule.getOpenContext().descriptionDraft
      const internalDescription =
        draft.trim().length > 0 ? draft.trim() : null

      try {
        const result = await adapters.updatePlacementInternalDescription(
          locationId,
          details.qrCodeId,
          internalDescription
        )

        const patchFact = <T extends CapturePlacementItem | PlacementDetailFact>(
          fact: T
        ): T => ({
          ...fact,
          internalDescription: result.internalDescription,
          updatedAt: result.updatedAt,
          updatedByDisplayName: result.updatedByDisplayName,
        })

        const nextFacts =
          state.placementsFacts?.map((fact) =>
            fact.qrCodeId === result.qrCodeId ? patchFact(fact) : fact
          ) ?? null

        const open = detailModule.getOpenContext()
        const nextDetailFact =
          open.fact?.qrCodeId === result.qrCodeId
            ? patchFact(open.fact)
            : nextFacts?.find((fact) => fact.qrCodeId === result.qrCodeId)
              ?? null

        state = {
          ...state,
          placementsFacts: nextFacts,
          viewModel:
            state.viewModel != null && state.workspace != null
              ? buildBaseViewModel(
                  state.workspace,
                  state.viewModel.locationId,
                  state.viewModel.performance,
                  nextFacts,
                  state.lastJourneyUpdate,
                  state.captureLocationStatus
                )
              : state.viewModel,
        }
        publish()
        if (nextDetailFact != null) {
          openPlacementDetailForFact(nextDetailFact, {
            descriptionDraft: result.internalDescription ?? "",
          })
        }
        return "saved"
      } catch {
        adapters.onPlacementActionError?.(
          "Could not save description. Please try again."
        )
        return "failed"
      }
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
      const locationId = resolveDetailLocationId()
      const target = confirm == null ? null : resolveDetailFact(confirm.qrCodeId)
      if (
        !state.pauseActivateConfirmIsOpen
        || confirm == null
        || locationId == null
        || target == null
      ) {
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

        const facts = state.placementsFacts
        const nextFacts =
          facts == null
            ? null
            : facts.map((item) =>
                item.qrCodeId === result.qrCodeId
                  ? { ...item, status: result.status }
                  : item
              )
        const updatedFromLive = nextFacts?.find(
          (item) => item.qrCodeId === result.qrCodeId
        )
        const open = detailModule.getOpenContext()
        const updatedOverride: PlacementDetailFact | null =
          open.fact?.qrCodeId === result.qrCodeId && open.locationId != null
            ? {
                ...open.fact,
                status: result.status,
              }
            : null
        const updatedFact = updatedFromLive ?? updatedOverride ?? (
          open.fact?.qrCodeId === result.qrCodeId
            ? { ...open.fact, status: result.status }
            : null
        )
        const workspace = state.workspace

        state = {
          ...state,
          ...clearPauseActivateConfirmState(),
          placementsFacts: nextFacts ?? state.placementsFacts,
          viewModel:
            state.viewModel == null
            || workspace == null
            || nextFacts == null
              ? state.viewModel
              : buildBaseViewModel(
                  workspace,
                  locationId,
                  state.viewModel.performance,
                  nextFacts,
                  state.lastJourneyUpdate,
                  state.captureLocationStatus
                ),
        }
        publish()
        if (updatedFact != null) {
          openPlacementDetailForFact(updatedFact, {
            archiveLocationId: open.locationId,
            descriptionDraft: updatedFact.internalDescription ?? "",
          })
        }

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
        const open = detailModule.getOpenContext()
        const preserveDraft =
          open.isOpen && open.qrCodeId === result.qrCodeId
            ? open.descriptionDraft
            : (fact?.internalDescription ?? "")
        state = {
          ...state,
          ...clearRotateConfirmState(),
          placementsFacts: nextFacts,
          viewModel:
            state.viewModel == null
              ? null
              : buildBaseViewModel(
                  workspace,
                  locationId,
                  state.viewModel.performance,
                  nextFacts,
                  state.lastJourneyUpdate,
                  state.captureLocationStatus
                ),
        }
        publish()
        if (fact != null) {
          openPlacementDetailForFact(fact, {
            archiveLocationId: null,
            descriptionDraft: preserveDraft,
          })
        }
        return "rotated"
      } catch {
        adapters.onPlacementActionError?.(ROTATE_ACTION_ERROR_MESSAGE)
        return "failed"
      }
    },
    requestPlacementDetailArchive() {
      const details = requireOpenPlacementDetail()
      if (details == null) {
        return Promise.resolve("noop" as const)
      }
      return archivePlacementById(details.qrCodeId)
    },
    async copyPlacementDetailLink() {
      const details = requireOpenPlacementDetail()
      if (details == null) {
        return "noop"
      }
      const open = detailModule.getOpenContext()
      const link =
        open.fact?.qrCodeId === details.qrCodeId
          ? open.fact.qrLinkUrl
          : state.placementsFacts?.find(
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
      if (details == null || !details.previewGuestExperienceEnabled) {
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
        buildGuestExperiencePreviewPicker({
          placements: [fact],
          selectedQrCodeId: fact.qrCodeId,
        }).selectedLabel ?? null
      state = {
        ...state,
        isGuestExperiencePreviewOpen: true,
        isGuestExperiencePreviewPickerOpen: false,
        guestExperiencePreviewPickerSelectedQrCodeId: null,
        guestExperiencePreviewPlacementLabel: label,
      }
      publish()
      return "opened"
    },
    async createDigitalGuestLink(input) {
      const locationId =
        input.locationId
        ?? state.viewModel?.locationId
        ?? state.workspace?.selectedLocationId
      if (locationId == null) {
        return "noop"
      }

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
      archiveModule.clearCreatePrefill()

      if (
        workspace != null
        && state.viewModel != null
        && state.viewModel.locationId === locationId
      ) {
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

        publish()
        openPlacementDetailForFact(fact, { archiveLocationId: null })
      } else {
        publish()
      }
      return "created"
    },
    archivePlacement(qrCodeId) {
      return archivePlacementById(qrCodeId)
    },
    requestDigitalGuestLinkArchive(qrCodeId) {
      const fact = state.placementsFacts?.find(
        (item) =>
          item.qrCodeId === qrCodeId && item.qrType === "DigitalGuestLink"
      )
      if (fact == null) {
        return Promise.resolve("noop" as const)
      }
      return archivePlacementById(qrCodeId)
    },
    openArchivePlacementDetail(qrCodeId) {
      const fact = archiveModule.getArchivedPlacement(qrCodeId)
      if (fact == null) {
        return "noop"
      }
      openPlacementDetailForFact(archivedItemToDetailFact(fact), {
        archiveLocationId: fact.locationId,
        locationName: fact.locationName,
      })
      return "opened"
    },
    async confirmRestore() {
      const result = await archiveModule.confirmRestore()
      if (result === "conflict" || result === "failed" || result === "noop") {
        return result
      }

      openPlacementDetailForFact(result.restoredFact, {
        archiveLocationId: result.locationId,
        locationName: resolveDetailLocationName(result.locationId),
      })
      return result
    },
  }

  async function archivePlacementById(
    qrCodeId: number
  ): Promise<ArchivePlacementResult> {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    const facts = state.placementsFacts
    const target = facts?.find((item) => item.qrCodeId === qrCodeId)
    if (
      workspace == null
      || locationId == null
      || facts == null
      || target == null
      || (target.status !== "Active" && target.status !== "Paused")
    ) {
      return "noop"
    }

    try {
      const result = await adapters.archiveCapturePlacement(locationId, qrCodeId)
      const nextFacts = facts.filter((item) => item.qrCodeId !== result.qrCodeId)
      const archivedFact: PlacementDetailFact = {
        ...target,
        status: "Archived",
      }
      const name = placementDisplayName(target)
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
                state.lastJourneyUpdate,
                  state.captureLocationStatus
                ),
      }
      publish()
      openPlacementDetailForFact(archivedFact, {
        archiveLocationId: null,
        descriptionDraft: target.internalDescription ?? "",
      })
      return {
        outcome: "archived",
        toastMessage: `${name} archived.`,
      }
    } catch {
      adapters.onPlacementActionError?.(ARCHIVE_ACTION_ERROR_MESSAGE)
      return "failed"
    }
  }
}


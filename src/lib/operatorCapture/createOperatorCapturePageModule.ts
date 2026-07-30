import {
  buildCaptureArchiveList,
  DEFAULT_CAPTURE_ARCHIVE_FILTERS,
  duplicateDigitalGuestLinkName,
  type CaptureArchiveFilters,
  type CaptureArchiveListResult,
  type CaptureArchiveSortId,
  type OperatorCaptureArchiveRow,
} from "@/lib/operatorCapture/buildCaptureArchive"
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
import {
  buildPlacementDetailDrawer,
  PLACEMENT_INTERNAL_DESCRIPTION_MAX_LENGTH,
  type PlacementDetailDrawerView,
} from "@/lib/operatorCapture/buildPlacementDetailDrawer"
import {
  buildRestoreConfirm,
  type RestoreConfirmView,
} from "@/lib/operatorCapture/buildRestoreConfirm"
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

export type PlacementDetailStubbedAction = "save-description"

export type PlacementDetailFact = Omit<CapturePlacementItem, "status"> & {
  status: CaptureQrCodeStatus
}

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

export type RestoreConfirmSnapshot = {
  isOpen: boolean
  details: RestoreConfirmView | null
}

export type ConfirmPauseActivateResult =
  | { outcome: "paused" | "activated"; toastMessage: string }
  | "failed"
  | "noop"

export type ConfirmRestoreResult =
  | { outcome: "restored"; toastMessage: string }
  | "conflict"
  | "failed"
  | "noop"

export type ArchivePlacementResult =
  | { outcome: "archived"; toastMessage: string }
  | "failed"
  | "noop"

export type CreateDigitalGuestLinkPrefill = {
  linkName: string
  channel: NonNullable<CapturePlacementItem["channel"]>
  status: CapturePlacementStatus
  locationId: number
}

export type OperatorCaptureArchiveView = CaptureArchiveListResult & {
  searchQuery: string
  filters: CaptureArchiveFilters
  sort: CaptureArchiveSortId
  returnPath: string | null
  showLocationFilter: boolean
  archiverOptions: readonly string[]
  locationOptions: readonly { id: number; label: string }[]
  createPrefill: CreateDigitalGuestLinkPrefill | null
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
  archiveLoadStatus: "idle" | "loading" | "loaded" | "error"
  isGuestExperiencePreviewOpen: boolean
  isGuestExperiencePreviewPickerOpen: boolean
  /** When set, guest-experience preview shows this placement label instead of the Smart Guest default. */
  guestExperiencePreviewPlacementLabel: string | null
  guestExperiencePreviewPicker: GuestExperiencePreviewPickerSnapshot
  placementDetailDrawer: PlacementDetailDrawerSnapshot
  rotateConfirm: PlacementRotateConfirmSnapshot
  pauseActivateConfirm: PauseActivateConfirmSnapshot
  restoreConfirm: RestoreConfirmSnapshot
  archive: OperatorCaptureArchiveView | null
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
  requestPlacementDetailArchive: () => Promise<ArchivePlacementResult>
  copyPlacementDetailLink: () => Promise<CopyCapturePlacementLinkResult>
  openPlacementDetailPreview: () => "opened" | "noop"
  enterArchive: (input: {
    returnPath: string
    preselectedLocationId?: number | null
    showLocationFilter: boolean
    locations: readonly OperatorCaptureWorkspaceLocation[]
  }) => Promise<void>
  reloadArchive: () => Promise<void>
  setArchiveSearchQuery: (query: string) => void
  setArchiveFilters: (filters: CaptureArchiveFilters) => void
  setArchiveSort: (sort: CaptureArchiveSortId) => void
  clearArchiveSearchAndFilters: () => void
  requestRestore: (qrCodeId: number) => "opened" | "noop"
  cancelRestoreConfirm: () => void
  confirmRestore: () => Promise<ConfirmRestoreResult>
  requestDuplicateAsNew: (qrCodeId: number) => "opened" | "noop"
  clearCreatePrefill: () => void
  openArchivePlacementDetail: (qrCodeId: number) => "opened" | "noop"
}


type ModuleState = {
  loadStatus: OperatorCapturePageSnapshot["loadStatus"]
  performanceLoadStatus: OperatorCapturePageSnapshot["performanceLoadStatus"]
  placementsLoadStatus: OperatorCapturePageSnapshot["placementsLoadStatus"]
  archiveLoadStatus: OperatorCapturePageSnapshot["archiveLoadStatus"]
  isGuestExperiencePreviewOpen: boolean
  isGuestExperiencePreviewPickerOpen: boolean
  guestExperiencePreviewPlacementLabel: string | null
  guestExperiencePreviewPickerSelectedQrCodeId: number | null
  placementDetailIsOpen: boolean
  placementDetailSelectedQrCodeId: number | null
  placementDetailDescriptionDraft: string
  placementDetailLastStubbedAction: PlacementDetailStubbedAction | null
  /** When set, Detail drawer uses this fact instead of live placementsFacts. */
  placementDetailFactOverride: PlacementDetailFact | null
  /** Location id for drawer actions when opened from Archive (override path). */
  placementDetailLocationId: number | null
  rotateConfirmQrCodeId: number | null
  rotatePrintMaterialsAcknowledged: boolean
  pauseActivateConfirmIsOpen: boolean
  pauseActivateConfirmDetails: PauseActivateConfirmView | null
  restoreConfirmIsOpen: boolean
  restoreConfirmDetails: RestoreConfirmView | null
  viewModel: OperatorCaptureViewModel | null
  placementsFacts: CapturePlacementsResponse["placements"] | null
  captureLocationStatus: CaptureLocationStatus
  lastJourneyUpdate: CapturePlacementsResponse["lastJourneyUpdate"] | undefined
  workspace: OperatorCaptureWorkspaceInput | null
  archivedFacts: CaptureArchivedPlacementItem[] | null
  archiveSearchQuery: string
  archiveFilters: CaptureArchiveFilters
  archiveSort: CaptureArchiveSortId
  archiveReturnPath: string | null
  archiveShowLocationFilter: boolean
  archiveLocations: readonly OperatorCaptureWorkspaceLocation[]
  createPrefill: CreateDigitalGuestLinkPrefill | null
  loadGeneration: number
  captureLoadGeneration: number
  archiveLoadGeneration: number
}


const FALLBACK_LOCATION_NAME = "Location"

const PERFORMANCE_LOAD_ERROR_MESSAGE =
  "Could not load Capture performance. Please try again."

const PLACEMENTS_LOAD_ERROR_MESSAGE =
  "Could not load QR placements. Please try again."

const ARCHIVE_LOAD_ERROR_MESSAGE =
  "Could not load archived placements. Please try again."

const ROTATE_ACTION_ERROR_MESSAGE =
  "Could not rotate QR code. Please try again."

const ARCHIVE_ACTION_ERROR_MESSAGE =
  "Could not archive QR code. Please try again."

const RESTORE_ACTION_ERROR_MESSAGE =
  "Could not restore QR code. Please try again."

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

  let state: ModuleState = {
    loadStatus: "idle",
    performanceLoadStatus: "idle",
    placementsLoadStatus: "idle",
    archiveLoadStatus: "idle",
    isGuestExperiencePreviewOpen: false,
    isGuestExperiencePreviewPickerOpen: false,
    guestExperiencePreviewPlacementLabel: null,
    guestExperiencePreviewPickerSelectedQrCodeId: null,
    placementDetailIsOpen: false,
    placementDetailSelectedQrCodeId: null,
    placementDetailDescriptionDraft: "",
    placementDetailLastStubbedAction: null,
    placementDetailFactOverride: null,
    placementDetailLocationId: null,
    rotateConfirmQrCodeId: null,
    rotatePrintMaterialsAcknowledged: false,
    pauseActivateConfirmIsOpen: false,
    pauseActivateConfirmDetails: null,
    restoreConfirmIsOpen: false,
    restoreConfirmDetails: null,
    viewModel: null,
    placementsFacts: null,
    captureLocationStatus: "Active",
    lastJourneyUpdate: undefined,
    workspace: null,
    archivedFacts: null,
    archiveSearchQuery: "",
    archiveFilters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
    archiveSort: "recently-archived",
    archiveReturnPath: null,
    archiveShowLocationFilter: false,
    archiveLocations: [],
    createPrefill: null,
    loadGeneration: 0,
    captureLoadGeneration: 0,
    archiveLoadGeneration: 0,
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

  const buildPlacementDetailDrawerSnapshot = (): PlacementDetailDrawerSnapshot => {
    if (
      !state.placementDetailIsOpen
      || state.placementDetailSelectedQrCodeId == null
    ) {
      return {
        isOpen: state.placementDetailIsOpen,
        selectedQrCodeId: state.placementDetailSelectedQrCodeId,
        details: null,
        lastStubbedAction: state.placementDetailLastStubbedAction,
      }
    }

    const override = state.placementDetailFactOverride
    const liveFact = state.placementsFacts?.find(
      (item) => item.qrCodeId === state.placementDetailSelectedQrCodeId
    )
    const fact: PlacementDetailFact | null =
      override?.qrCodeId === state.placementDetailSelectedQrCodeId
        ? override
        : liveFact != null
          ? liveFact
          : null

    if (fact == null) {
      return {
        isOpen: true,
        selectedQrCodeId: state.placementDetailSelectedQrCodeId,
        details: null,
        lastStubbedAction: state.placementDetailLastStubbedAction,
      }
    }

    const locationName =
      override != null
        && state.archivedFacts?.some(
          (item) => item.qrCodeId === override.qrCodeId
        )
        ? (state.archivedFacts.find(
            (item) => item.qrCodeId === override.qrCodeId
          )?.locationName
          ?? state.viewModel?.locationName
          ?? FALLBACK_LOCATION_NAME)
        : (state.viewModel?.locationName ?? FALLBACK_LOCATION_NAME)

    return {
      isOpen: true,
      selectedQrCodeId: state.placementDetailSelectedQrCodeId,
      details: buildPlacementDetailDrawer({
        fact,
        locationName,
        descriptionDraft: state.placementDetailDescriptionDraft,
        locationCapturePaused: state.captureLocationStatus === "Paused",
        nowMs: nowMs(),
      }),
      lastStubbedAction: state.placementDetailLastStubbedAction,
    }
  }

  const closedPauseActivateConfirm = (): PauseActivateConfirmSnapshot => ({
    isOpen: false,
    details: null,
  })

  const closedRestoreConfirm = (): RestoreConfirmSnapshot => ({
    isOpen: false,
    details: null,
  })

  const buildRestoreConfirmSnapshot = (): RestoreConfirmSnapshot => {
    if (!state.restoreConfirmIsOpen || state.restoreConfirmDetails == null) {
      return closedRestoreConfirm()
    }
    return {
      isOpen: true,
      details: state.restoreConfirmDetails,
    }
  }

  const buildArchiveView = (): OperatorCaptureArchiveView | null => {
    if (state.archivedFacts == null && state.archiveLoadStatus === "idle") {
      return null
    }
    const facts = state.archivedFacts ?? []
    const list = buildCaptureArchiveList({
      facts,
      searchQuery: state.archiveSearchQuery,
      filters: state.archiveFilters,
      sort: state.archiveSort,
      nowMs: nowMs(),
      showLocationFilter: state.archiveShowLocationFilter,
    })
    const archiverOptions = [
      ...new Set(
        facts
          .map((f) => f.archivedByDisplayName?.trim())
          .filter((name): name is string => name != null && name !== "")
      ),
    ].sort((a, b) => a.localeCompare(b))

    return {
      ...list,
      searchQuery: state.archiveSearchQuery,
      filters: state.archiveFilters,
      sort: state.archiveSort,
      returnPath: state.archiveReturnPath,
      showLocationFilter: state.archiveShowLocationFilter,
      archiverOptions,
      locationOptions: state.archiveLocations.map((l) => ({
        id: l.id,
        label: l.locationName,
      })),
      createPrefill: state.createPrefill,
    }
  }

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
    archiveLoadStatus: state.archiveLoadStatus,
    isGuestExperiencePreviewOpen: state.isGuestExperiencePreviewOpen,
    isGuestExperiencePreviewPickerOpen: state.isGuestExperiencePreviewPickerOpen,
    guestExperiencePreviewPlacementLabel:
      state.guestExperiencePreviewPlacementLabel,
    guestExperiencePreviewPicker: closedGuestExperiencePreviewPicker(),
    placementDetailDrawer: closedPlacementDetailDrawer(),
    rotateConfirm: closedRotateConfirm(),
    pauseActivateConfirm: closedPauseActivateConfirm(),
    restoreConfirm: closedRestoreConfirm(),
    archive: null,
    viewModel: state.viewModel,
  }
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = {
      loadStatus: state.loadStatus,
      performanceLoadStatus: state.performanceLoadStatus,
      placementsLoadStatus: state.placementsLoadStatus,
      archiveLoadStatus: state.archiveLoadStatus,
      isGuestExperiencePreviewOpen: state.isGuestExperiencePreviewOpen,
      isGuestExperiencePreviewPickerOpen:
        state.isGuestExperiencePreviewPickerOpen,
      guestExperiencePreviewPlacementLabel:
        state.guestExperiencePreviewPlacementLabel,
      guestExperiencePreviewPicker: buildGuestExperiencePreviewPickerSnapshot(),
      placementDetailDrawer: buildPlacementDetailDrawerSnapshot(),
      rotateConfirm: buildRotateConfirmSnapshot(),
      pauseActivateConfirm: buildPauseActivateConfirmSnapshot(),
      restoreConfirm: buildRestoreConfirmSnapshot(),
      archive: buildArchiveView(),
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
    placementDetailFactOverride: null,
    placementDetailLocationId: null,
  })

  const clearRotateConfirmState = () => ({
    rotateConfirmQrCodeId: null as number | null,
    rotatePrintMaterialsAcknowledged: false as const,
  })

  const clearPauseActivateConfirmState = () => ({
    pauseActivateConfirmIsOpen: false as const,
    pauseActivateConfirmDetails: null,
  })

  const clearRestoreConfirmState = () => ({
    restoreConfirmIsOpen: false as const,
    restoreConfirmDetails: null,
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
    if (
      state.placementDetailFactOverride != null
      && state.placementDetailFactOverride.qrCodeId === qrCodeId
    ) {
      return state.placementDetailFactOverride
    }
    return null
  }

  const resolveDetailLocationId = (): number | null =>
    state.placementDetailLocationId
    ?? state.workspace?.selectedLocationId
    ?? null

  const resolveDetailLocationName = (locationId: number | null): string => {
    if (state.viewModel != null) {
      return state.viewModel.locationName
    }
    if (locationId == null) {
      return ""
    }
    return (
      state.archiveLocations.find((location) => location.id === locationId)
        ?.locationName
      ?? ""
    )
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
        lastJourneyUpdate,
        captureLocationStatus
      ),
      placementsFacts,
      captureLocationStatus,
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
      guestExperiencePreviewPickerSelectedQrCodeId: null,
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
        guestExperiencePreviewPickerSelectedQrCodeId: null,
        ...clearPlacementDetailState(),
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
      state = {
        ...state,
        placementDetailIsOpen: true,
        placementDetailSelectedQrCodeId: qrCodeId,
        placementDetailDescriptionDraft: fact.internalDescription ?? "",
        placementDetailLastStubbedAction: null,
        placementDetailFactOverride: null,
        placementDetailLocationId: null,
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
        const updatedOverride: PlacementDetailFact | null =
          state.placementDetailFactOverride?.qrCodeId === result.qrCodeId
            ? {
                ...state.placementDetailFactOverride,
                status: result.status,
              }
            : state.placementDetailFactOverride
        const updatedFact = updatedFromLive ?? updatedOverride
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
          placementDetailIsOpen: true,
          placementDetailSelectedQrCodeId: result.qrCodeId,
          placementDetailDescriptionDraft:
            updatedFact?.internalDescription ?? "",
          placementDetailLastStubbedAction: null,
          placementDetailFactOverride: updatedOverride,
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
                  state.lastJourneyUpdate,
                  state.captureLocationStatus
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
        return Promise.resolve("noop" as const)
      }
      return archivePlacementById(details.qrCodeId)
    },
    async copyPlacementDetailLink() {
      const details = requireOpenPlacementDetail()
      if (details == null) {
        return "noop"
      }
      const link =
        state.placementDetailFactOverride?.qrCodeId === details.qrCodeId
          ? state.placementDetailFactOverride.qrLinkUrl
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
      state = {
        ...state,
        createPrefill: null,
      }

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

        state = {
          ...state,
          placementDetailIsOpen: true,
          placementDetailSelectedQrCodeId: createdQrCodeId,
          placementDetailDescriptionDraft: fact.internalDescription ?? "",
          placementDetailLastStubbedAction: null,
          placementDetailFactOverride: null,
        }
        publish()
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
    async enterArchive(input) {
      const generation = ++state.archiveLoadGeneration
      const preselected = input.preselectedLocationId
      state = {
        ...state,
        archiveLoadStatus: "loading",
        archiveReturnPath: input.returnPath,
        archiveShowLocationFilter: input.showLocationFilter,
        archiveLocations: input.locations,
        archiveSearchQuery: "",
        archiveSort: "recently-archived",
        archiveFilters: {
          ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
          locationIds:
            input.showLocationFilter
            && preselected != null
              ? [preselected]
              : [],
        },
        createPrefill: null,
        ...clearRestoreConfirmState(),
      }
      publish()

      try {
        const response = await adapters.getArchivedCapturePlacements()
        if (generation !== state.archiveLoadGeneration) {
          return
        }
        state = {
          ...state,
          archiveLoadStatus: "loaded",
          archivedFacts: response.placements,
        }
        publish()
      } catch {
        if (generation !== state.archiveLoadGeneration) {
          return
        }
        state = {
          ...state,
          archiveLoadStatus: "error",
          archivedFacts: [],
        }
        publish()
        adapters.onArchiveLoadError?.(ARCHIVE_LOAD_ERROR_MESSAGE)
      }
    },
    async reloadArchive() {
      const generation = ++state.archiveLoadGeneration
      state = {
        ...state,
        archiveLoadStatus: "loading",
      }
      publish()
      try {
        const response = await adapters.getArchivedCapturePlacements()
        if (generation !== state.archiveLoadGeneration) {
          return
        }
        state = {
          ...state,
          archiveLoadStatus: "loaded",
          archivedFacts: response.placements,
        }
        publish()
      } catch {
        if (generation !== state.archiveLoadGeneration) {
          return
        }
        state = {
          ...state,
          archiveLoadStatus: "error",
        }
        publish()
        adapters.onArchiveLoadError?.(ARCHIVE_LOAD_ERROR_MESSAGE)
      }
    },
    setArchiveSearchQuery(query) {
      state = {
        ...state,
        archiveSearchQuery: query,
      }
      publish()
    },
    setArchiveFilters(filters) {
      state = {
        ...state,
        archiveFilters: filters,
      }
      publish()
    },
    setArchiveSort(sort) {
      state = {
        ...state,
        archiveSort: sort,
      }
      publish()
    },
    clearArchiveSearchAndFilters() {
      state = {
        ...state,
        archiveSearchQuery: "",
        archiveFilters: {
          ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
          locationIds: [],
        },
      }
      publish()
    },
    requestRestore(qrCodeId) {
      const fact = state.archivedFacts?.find((item) => item.qrCodeId === qrCodeId)
      if (fact == null || !fact.canRestore) {
        return "noop"
      }
      state = {
        ...state,
        restoreConfirmIsOpen: true,
        restoreConfirmDetails: buildRestoreConfirm(fact),
      }
      publish()
      return "opened"
    },
    cancelRestoreConfirm() {
      state = {
        ...state,
        ...clearRestoreConfirmState(),
      }
      publish()
    },
    async confirmRestore() {
      const details = state.restoreConfirmDetails
      if (!state.restoreConfirmIsOpen || details == null) {
        return "noop"
      }

      const result = await adapters.restoreCapturePlacement(
        details.locationId,
        details.qrCodeId
      )
      if (!result.ok) {
        if (result.reason === "conflict") {
          adapters.onPlacementActionError?.(result.message)
          return "conflict"
        }
        adapters.onPlacementActionError?.(
          result.message || RESTORE_ACTION_ERROR_MESSAGE
        )
        return "failed"
      }

      const nextArchived =
        state.archivedFacts?.filter((item) => item.qrCodeId !== details.qrCodeId)
        ?? []

      const restoredFact: PlacementDetailFact = {
        qrCodeId: result.qrCodeId,
        qrType:
          state.archivedFacts?.find((item) => item.qrCodeId === details.qrCodeId)
            ?.qrType
          ?? "CounterCard",
        status: "Paused",
        linkName:
          state.archivedFacts?.find((item) => item.qrCodeId === details.qrCodeId)
            ?.linkName
          ?? null,
        channel:
          state.archivedFacts?.find((item) => item.qrCodeId === details.qrCodeId)
            ?.channel
          ?? null,
        internalDescription:
          state.archivedFacts?.find((item) => item.qrCodeId === details.qrCodeId)
            ?.internalDescription
          ?? null,
        qrLinkUrl: result.qrLinkUrl,
        qrScans:
          state.archivedFacts?.find((item) => item.qrCodeId === details.qrCodeId)
            ?.qrScans
          ?? 0,
        feedbackSubmitted:
          state.archivedFacts?.find((item) => item.qrCodeId === details.qrCodeId)
            ?.feedbackSubmitted
          ?? 0,
        marketingOptIns: 0,
        offerClaims: 0,
        lastScanAt:
          state.archivedFacts?.find((item) => item.qrCodeId === details.qrCodeId)
            ?.lastScanAt
          ?? null,
      }

      // Capture archived fact fields before filtering for drawer.
      const prior = state.archivedFacts?.find(
        (item) => item.qrCodeId === details.qrCodeId
      )
      if (prior != null) {
        restoredFact.qrType = prior.qrType
        restoredFact.linkName = prior.linkName
        restoredFact.channel = prior.channel
        restoredFact.internalDescription = prior.internalDescription
        restoredFact.qrScans = prior.qrScans
        restoredFact.feedbackSubmitted = prior.feedbackSubmitted
        restoredFact.lastScanAt = prior.lastScanAt
      }

      state = {
        ...state,
        archivedFacts: nextArchived,
        ...clearRestoreConfirmState(),
        placementDetailIsOpen: true,
        placementDetailSelectedQrCodeId: result.qrCodeId,
        placementDetailDescriptionDraft:
          restoredFact.internalDescription ?? "",
        placementDetailLastStubbedAction: null,
        placementDetailFactOverride: restoredFact,
        placementDetailLocationId: details.locationId,
      }
      publish()
      return {
        outcome: "restored",
        toastMessage: details.successToastMessage,
      }
    },
    requestDuplicateAsNew(qrCodeId) {
      const fact = state.archivedFacts?.find((item) => item.qrCodeId === qrCodeId)
      if (
        fact == null
        || fact.qrType !== "DigitalGuestLink"
        || fact.channel == null
      ) {
        return "noop"
      }
      state = {
        ...state,
        createPrefill: {
          linkName: duplicateDigitalGuestLinkName(fact.linkName ?? ""),
          channel: fact.channel,
          status: "Active",
          locationId: fact.locationId,
        },
      }
      publish()
      return "opened"
    },
    clearCreatePrefill() {
      state = {
        ...state,
        createPrefill: null,
      }
      publish()
    },
    openArchivePlacementDetail(qrCodeId) {
      const fact = state.archivedFacts?.find((item) => item.qrCodeId === qrCodeId)
      if (fact == null) {
        return "noop"
      }
      const detailFact = archivedItemToDetailFact(fact)
      state = {
        ...state,
        placementDetailIsOpen: true,
        placementDetailSelectedQrCodeId: qrCodeId,
        placementDetailDescriptionDraft: fact.internalDescription ?? "",
        placementDetailLastStubbedAction: null,
        placementDetailFactOverride: detailFact,
        placementDetailLocationId: fact.locationId,
      }
      publish()
      return "opened"
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
        placementDetailIsOpen: true,
        placementDetailSelectedQrCodeId: result.qrCodeId,
        placementDetailDescriptionDraft: target.internalDescription ?? "",
        placementDetailLastStubbedAction: null,
        placementDetailFactOverride: archivedFact,
      }
      publish()
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


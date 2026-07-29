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


export type OperatorCapturePerformanceView = {
  kpis: OperatorCaptureKpi[]
  isEmpty: boolean
}


export type OperatorCapturePlacementsView = {
  rows: OperatorCapturePlacementRow[]
  isEmpty: boolean
}

export type { OperatorCaptureGuestExperienceView }


export type OperatorCaptureViewModel = {
  locationId: number
  locationName: string
  dateRangeLabel: string
  performance: OperatorCapturePerformanceView
  placements: OperatorCapturePlacementsView
  guestExperience: OperatorCaptureGuestExperienceView
}


export type OperatorCapturePageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  performanceLoadStatus: "idle" | "loading" | "loaded" | "error"
  placementsLoadStatus: "idle" | "loading" | "loaded" | "error"
  isGuestExperiencePreviewOpen: boolean
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
  openGuestExperiencePreview: () => void
  closeGuestExperiencePreview: () => void
}


type ModuleState = {
  loadStatus: OperatorCapturePageSnapshot["loadStatus"]
  performanceLoadStatus: OperatorCapturePageSnapshot["performanceLoadStatus"]
  placementsLoadStatus: OperatorCapturePageSnapshot["placementsLoadStatus"]
  isGuestExperiencePreviewOpen: boolean
  viewModel: OperatorCaptureViewModel | null
  placementsFacts: CapturePlacementsResponse["placements"] | null
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
    viewModel: null,
    placementsFacts: null,
    workspace: null,
    loadGeneration: 0,
    captureLoadGeneration: 0,
  }
  let snapshot: OperatorCapturePageSnapshot = {
    loadStatus: state.loadStatus,
    performanceLoadStatus: state.performanceLoadStatus,
    placementsLoadStatus: state.placementsLoadStatus,
    isGuestExperiencePreviewOpen: state.isGuestExperiencePreviewOpen,
    viewModel: state.viewModel,
  }
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = {
      loadStatus: state.loadStatus,
      performanceLoadStatus: state.performanceLoadStatus,
      placementsLoadStatus: state.placementsLoadStatus,
      isGuestExperiencePreviewOpen: state.isGuestExperiencePreviewOpen,
      viewModel: state.viewModel,
    }
    for (const listener of listeners) {
      listener()
    }
  }

  const currentDateRangeLabel = () =>
    labelForHomePerformanceDateRange(
      adapters.getCapturePerformanceDateRange()
    )

  const buildBaseViewModel = (
    input: OperatorCaptureWorkspaceInput,
    locationId: number,
    performance: OperatorCapturePerformanceView,
    placementsFacts: CapturePlacementsResponse["placements"] | null
  ): OperatorCaptureViewModel => {
    const locationName = resolveLocationName(input, locationId)
    const locationAddress = resolveLocationAddress(input, locationId)

    return {
      locationId,
      locationName,
      dateRangeLabel: currentDateRangeLabel(),
      performance,
      placements: buildCapturePlacements(placementsFacts ?? [], nowMs()),
      guestExperience: buildCaptureGuestExperience({
        // Pass facts through unchanged: null (unavailable/failed) stays honest-unknown,
        // while a true empty list still yields a real 0 Active QR count.
        placements: placementsFacts,
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

    state = {
      ...state,
      loadStatus: "loaded",
      performanceLoadStatus: performanceSettled.ok ? "loaded" : "error",
      placementsLoadStatus: placementsSettled.ok ? "loaded" : "error",
      viewModel: buildBaseViewModel(
        options.workspace,
        options.locationId,
        performance,
        placementsFacts
      ),
      placementsFacts,
      workspace: options.workspace,
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
        viewModel: null,
        placementsFacts: null,
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
                  nextFacts
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
                  nextFacts
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
      const link = state.viewModel?.placements.rows.find(
        (row) => row.qrCodeId === qrCodeId
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
      if (state.viewModel == null) {
        return
      }
      state = {
        ...state,
        isGuestExperiencePreviewOpen: true,
      }
      publish()
    },
    closeGuestExperiencePreview() {
      if (!state.isGuestExperiencePreviewOpen) {
        return
      }
      state = {
        ...state,
        isGuestExperiencePreviewOpen: false,
      }
      publish()
    },
  }
}


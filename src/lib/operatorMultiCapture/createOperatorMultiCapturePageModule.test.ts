import { describe, expect, it, vi } from "vitest"

import { createOperatorMultiCapturePageModule } from "./createOperatorMultiCapturePageModule"
import type {
  CaptureLocationsResponse,
  CaptureOverviewResponse,
} from "@/types/dashboard"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import { emptySelection } from "@/lib/operatorFilterSheet"
import { captureLocationsFilterSheetSchema } from "./captureLocationsFilterSheetSchema"

const DEFAULT_RANGE: HomePerformanceDateRange = {
  kind: "preset",
  presetId: "last7",
}

const FILTER_SCHEMA = captureLocationsFilterSheetSchema()

function emptyOverviewResponse(
  overrides: Partial<CaptureOverviewResponse> = {}
): CaptureOverviewResponse {
  return {
    success: true,
    activeLocations: 2,
    totalLocations: 2,
    activeQrPlacements: 3,
    qrScans: 0,
    qrScansPrevious: 0,
    feedbackSubmitted: 0,
    feedbackSubmittedPrevious: 0,
    marketingOptIns: 0,
    marketingOptInsPrevious: 0,
    offerClaims: 0,
    offerClaimsHasRealData: false,
    ...overrides,
  }
}

function emptyLocationsResponse(
  overrides: Partial<CaptureLocationsResponse> = {}
): CaptureLocationsResponse {
  return {
    success: true,
    items: [
      {
        locationId: 1,
        locationName: "Camden",
        status: "Active",
        activePlacementsCount: 2,
        qrScans: 10,
        feedbackSubmitted: 4,
        marketingOptIns: 2,
        offerClaims: 0,
        lastActivityAt: "2026-07-16T11:00:00.000Z",
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    ...overrides,
  }
}

function createModule(options?: {
  overview?: CaptureOverviewResponse | (() => Promise<CaptureOverviewResponse>)
  locations?:
    | CaptureLocationsResponse
    | (() => Promise<CaptureLocationsResponse>)
  range?: HomePerformanceDateRange
  onOverviewLoadError?: (message: string) => void
  onLocationsLoadError?: (message: string) => void
  syncSelectedLocation?: (locationId: number) => void
  navigateToCaptureLocation?: (locationId: number) => void
  failOverview?: boolean
  failLocations?: boolean
  scheduleReady?: () => Promise<void>
  debounceMs?: number
}) {
  const getCaptureOverview = vi.fn(
    async (): Promise<CaptureOverviewResponse> => {
      if (options?.failOverview) {
        throw new Error("network")
      }
      if (typeof options?.overview === "function") {
        return options.overview()
      }
      return options?.overview ?? emptyOverviewResponse()
    }
  )
  const getCaptureLocations = vi.fn(
    async (): Promise<CaptureLocationsResponse> => {
      if (options?.failLocations) {
        throw new Error("network")
      }
      if (typeof options?.locations === "function") {
        return options.locations()
      }
      return options?.locations ?? emptyLocationsResponse()
    }
  )
  const onOverviewLoadError = vi.fn(options?.onOverviewLoadError)
  const onLocationsLoadError = vi.fn(options?.onLocationsLoadError)
  const syncSelectedLocation = vi.fn(options?.syncSelectedLocation)
  const navigateToCaptureLocation = vi.fn(options?.navigateToCaptureLocation)
  const scheduleReady =
    options?.scheduleReady ?? vi.fn(() => Promise.resolve())

  const pageModule = createOperatorMultiCapturePageModule({
    getCaptureOverview,
    getCaptureLocations,
    getMultiCaptureOverviewDateRange: () => options?.range ?? DEFAULT_RANGE,
    syncSelectedLocation,
    navigateToCaptureLocation,
    onOverviewLoadError,
    onLocationsLoadError,
    scheduleReady,
    debounceMs: options?.debounceMs ?? 0,
  })

  return {
    pageModule,
    getCaptureOverview,
    getCaptureLocations,
    syncSelectedLocation,
    navigateToCaptureLocation,
    onOverviewLoadError,
    onLocationsLoadError,
    scheduleReady,
  }
}

describe("createOperatorMultiCapturePageModule", () => {
  it("starts idle with no view model before workspace sync", () => {
    const { pageModule } = createModule()

    expect(pageModule.getSnapshot()).toMatchObject({
      loadStatus: "idle",
      overviewLoadStatus: "idle",
      listLoadStatus: "idle",
      viewModel: null,
    })
  })

  it("loads overview KPIs and location rows when workspace has locations", async () => {
    const { pageModule, getCaptureOverview, getCaptureLocations } =
      createModule({
        overview: emptyOverviewResponse({
          qrScans: 5,
          qrScansPrevious: 2,
          feedbackSubmitted: 3,
          feedbackSubmittedPrevious: 1,
          marketingOptIns: 2,
          marketingOptInsPrevious: 0,
        }),
      })

    await pageModule.syncWorkspace({
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })

    expect(getCaptureOverview).toHaveBeenCalledOnce()
    expect(getCaptureLocations).toHaveBeenCalledOnce()
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("ready")
    expect(snapshot.overviewLoadStatus).toBe("loaded")
    expect(snapshot.listLoadStatus).toBe("loaded")
    expect(snapshot.viewModel?.overview.isNoLocations).toBe(false)
    expect(snapshot.viewModel?.overview.kpis).toHaveLength(6)
    expect(snapshot.viewModel?.locationPerformance.rows).toHaveLength(1)
    expect(snapshot.viewModel?.locationPerformance.rows[0]).toMatchObject({
      locationName: "Camden",
      submissionRateText: "40%",
      qrScansText: "10 scans",
    })
    expect(snapshot.viewModel?.locationPerformance.showToolbar).toBe(true)
    expect(snapshot.viewModel?.locationPerformance.showPagination).toBe(true)
  })

  it("reloads overview and list when the Multi Capture overview date range changes", async () => {
    let range: HomePerformanceDateRange = DEFAULT_RANGE
    const getCaptureOverview = vi.fn(async () => emptyOverviewResponse())
    const getCaptureLocations = vi.fn(async () => emptyLocationsResponse())
    const pageModule = createOperatorMultiCapturePageModule({
      getCaptureOverview,
      getCaptureLocations,
      getMultiCaptureOverviewDateRange: () => range,
      syncSelectedLocation: vi.fn(),
      navigateToCaptureLocation: vi.fn(),
      debounceMs: 0,
    })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })
    expect(getCaptureOverview).toHaveBeenCalledOnce()
    expect(getCaptureLocations).toHaveBeenCalledOnce()

    range = { kind: "preset", presetId: "last30" }
    await pageModule.reloadForMultiCaptureOverviewDateRange()

    expect(getCaptureOverview).toHaveBeenCalledTimes(2)
    expect(getCaptureLocations).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().viewModel?.dateRangeLabel).toBe(
      "Last 30 days"
    )
  })

  it("navigateToLocationCapture syncs selected location then navigates", () => {
    const { pageModule, syncSelectedLocation, navigateToCaptureLocation } =
      createModule()

    pageModule.navigateToLocationCapture(42)

    expect(syncSelectedLocation).toHaveBeenCalledExactlyOnceWith(42)
    expect(navigateToCaptureLocation).toHaveBeenCalledExactlyOnceWith(42)
    expect(syncSelectedLocation.mock.invocationCallOrder[0]).toBeLessThan(
      navigateToCaptureLocation.mock.invocationCallOrder[0]!
    )
  })

  it("exposes View location capture enabled and remaining row actions stubbed", () => {
    const { pageModule } = createModule()

    expect(pageModule.getLocationRowActions()).toEqual([
      {
        id: "view-location-capture",
        label: "View location capture",
        enabled: true,
      },
      {
        id: "add-qr-placement",
        label: "Add QR placement",
        enabled: false,
      },
      {
        id: "preview-guest-experience",
        label: "Preview guest experience",
        enabled: false,
      },
      {
        id: "order-print-materials",
        label: "Order print materials",
        enabled: false,
      },
    ])
  })

  it("refetches list only when search, filters, sort, or page change", async () => {
    const { pageModule, getCaptureOverview, getCaptureLocations } =
      createModule({
        locations: emptyLocationsResponse({
          totalCount: 40,
          items: Array.from({ length: 20 }, (_, index) => ({
            locationId: index + 1,
            locationName: `Loc ${index + 1}`,
            status: "Active" as const,
            activePlacementsCount: 0,
            qrScans: index,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastActivityAt: null,
          })),
        }),
      })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })
    expect(getCaptureOverview).toHaveBeenCalledOnce()
    expect(getCaptureLocations).toHaveBeenCalledOnce()

    pageModule.setSortId("location-name-az")
    await vi.waitFor(() =>
      expect(getCaptureLocations).toHaveBeenCalledTimes(2)
    )
    expect(getCaptureOverview).toHaveBeenCalledOnce()

    pageModule.goToNextPage()
    await vi.waitFor(() =>
      expect(getCaptureLocations).toHaveBeenCalledTimes(3)
    )
    expect(getCaptureOverview).toHaveBeenCalledOnce()

    pageModule.setSearchQuery("cam")
    await vi.waitFor(() =>
      expect(getCaptureLocations).toHaveBeenCalledTimes(4)
    )
    expect(getCaptureOverview).toHaveBeenCalledOnce()
    expect(getCaptureLocations).toHaveBeenLastCalledWith(
      expect.objectContaining({ q: "cam", page: 1 })
    )
  })

  it("shows no-locations empty view without calling APIs when locations are empty", async () => {
    const { pageModule, getCaptureOverview, getCaptureLocations } =
      createModule()

    await pageModule.syncWorkspace({ locations: [] })

    expect(getCaptureOverview).not.toHaveBeenCalled()
    expect(getCaptureLocations).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot()).toMatchObject({
      loadStatus: "ready",
      overviewLoadStatus: "loaded",
      listLoadStatus: "loaded",
      viewModel: {
        overview: {
          kpis: [],
          isNoLocations: true,
          isLoadError: false,
        },
        locationPerformance: {
          emptyKind: "no-locations",
          showToolbar: false,
          showPagination: false,
          rows: [],
        },
      },
    })
  })

  it("shows no-results empty when filters match nothing", async () => {
    const { pageModule } = createModule({
      locations: emptyLocationsResponse({
        items: [],
        totalCount: 0,
      }),
    })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })

    const locationPerformance =
      pageModule.getSnapshot().viewModel?.locationPerformance
    expect(locationPerformance?.emptyKind).toBe("no-results")
    expect(locationPerformance?.showToolbar).toBe(true)
    expect(locationPerformance?.showPagination).toBe(false)
  })

  it("keeps zero-metric rows as normal rows (not section empty)", async () => {
    const { pageModule } = createModule({
      locations: emptyLocationsResponse({
        items: [
          {
            locationId: 1,
            locationName: "Camden",
            status: "Active",
            activePlacementsCount: 0,
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastActivityAt: null,
          },
        ],
        totalCount: 1,
      }),
    })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })

    const locationPerformance =
      pageModule.getSnapshot().viewModel?.locationPerformance
    expect(locationPerformance?.emptyKind).toBeNull()
    expect(locationPerformance?.rows[0]).toMatchObject({
      qrScansText: "0 scans",
      submissionRateText: "—",
      lastActivityText: "—",
    })
  })

  it("surfaces overview and list load errors with empty chrome + toasts", async () => {
    const { pageModule, onOverviewLoadError, onLocationsLoadError } =
      createModule({
        failOverview: true,
        failLocations: true,
      })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot()).toMatchObject({
      loadStatus: "ready",
      overviewLoadStatus: "error",
      listLoadStatus: "error",
      viewModel: {
        overview: {
          isLoadError: true,
        },
        locationPerformance: {
          emptyKind: "load-error",
          showToolbar: false,
        },
      },
    })
    expect(onOverviewLoadError).toHaveBeenCalledWith(
      "Could not load Capture overview. Please try again."
    )
    expect(onLocationsLoadError).toHaveBeenCalledWith(
      "Could not load location performance. Please try again."
    )
  })

  it("clearSearchAndFilters clears search and filters but keeps sort", async () => {
    const { pageModule, getCaptureLocations } = createModule({
      locations: emptyLocationsResponse({ items: [], totalCount: 0 }),
    })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })

    pageModule.setSortId("location-name-az")
    await vi.waitFor(() =>
      expect(getCaptureLocations).toHaveBeenCalledTimes(2)
    )

    pageModule.setSearchQuery("cam")
    await vi.waitFor(() =>
      expect(getCaptureLocations).toHaveBeenCalledTimes(3)
    )

    pageModule.applyFilters({
      ...emptySelection(FILTER_SCHEMA),
      status: { kind: "multi-select", ids: ["Paused"] },
    })
    await vi.waitFor(() =>
      expect(getCaptureLocations).toHaveBeenCalledTimes(4)
    )

    pageModule.clearSearchAndFilters()
    await vi.waitFor(() =>
      expect(getCaptureLocations).toHaveBeenCalledTimes(5)
    )

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.searchQuery).toBe("")
    expect(snapshot.filterChipCount).toBe(0)
    expect(snapshot.sortId).toBe("location-name-az")
  })

  it("retryLoad re-runs overview and list load from a ready snapshot", async () => {
    const { pageModule, getCaptureOverview, getCaptureLocations } =
      createModule()

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })
    expect(getCaptureOverview).toHaveBeenCalledOnce()
    expect(getCaptureLocations).toHaveBeenCalledOnce()

    await pageModule.retryLoad()

    expect(getCaptureOverview).toHaveBeenCalledTimes(2)
    expect(getCaptureLocations).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().loadStatus).toBe("ready")
  })
})

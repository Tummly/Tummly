import { describe, expect, it, vi } from "vitest"

import { createOperatorCapturePageModule } from "./createOperatorCapturePageModule"
import type {
  CapturePerformanceResponse,
  CapturePlacementsResponse,
} from "@/types/dashboard"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

const DEFAULT_RANGE: HomePerformanceDateRange = {
  kind: "preset",
  presetId: "last7",
}

function emptyPerformanceResponse(
  overrides: Partial<CapturePerformanceResponse> = {}
): CapturePerformanceResponse {
  return {
    success: true,
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

function emptyPlacementsResponse(
  overrides: Partial<CapturePlacementsResponse> = {}
): CapturePlacementsResponse {
  return {
    success: true,
    placements: [],
    ...overrides,
  }
}

function createModule(options?: {
  performance?: CapturePerformanceResponse | (() => Promise<CapturePerformanceResponse>)
  placements?: CapturePlacementsResponse | (() => Promise<CapturePlacementsResponse>)
  range?: HomePerformanceDateRange
  onPerformanceLoadError?: (message: string) => void
  onPlacementsLoadError?: (message: string) => void
  onPlacementActionError?: (message: string) => void
  onCopyPlacementLinkError?: (message: string) => void
  failPerformance?: boolean
  failPlacements?: boolean
  failPause?: boolean
  failResume?: boolean
  failCopy?: boolean
  nowMs?: number
}) {
  const getCapturePerformance = vi.fn(
    async (): Promise<CapturePerformanceResponse> => {
      if (options?.failPerformance) {
        throw new Error("network")
      }
      if (typeof options?.performance === "function") {
        return options.performance()
      }
      return options?.performance ?? emptyPerformanceResponse()
    }
  )
  const getCapturePlacements = vi.fn(
    async (): Promise<CapturePlacementsResponse> => {
      if (options?.failPlacements) {
        throw new Error("network")
      }
      if (typeof options?.placements === "function") {
        return options.placements()
      }
      return options?.placements ?? emptyPlacementsResponse()
    }
  )
  const copyText = vi.fn(async () => {
    if (options?.failCopy) {
      return {
        ok: false as const,
        error: "Could not copy link. Please try again.",
      }
    }
    return { ok: true as const }
  })
  const pauseCapturePlacement = vi.fn(async (_locationId: number, qrCodeId: number) => {
    if (options?.failPause) {
      throw new Error("pause failed")
    }
    return { qrCodeId, status: "Paused" as const }
  })
  const resumeCapturePlacement = vi.fn(async (_locationId: number, qrCodeId: number) => {
    if (options?.failResume) {
      throw new Error("resume failed")
    }
    return { qrCodeId, status: "Active" as const }
  })
  let range = options?.range ?? DEFAULT_RANGE

  const pageModule = createOperatorCapturePageModule({
    getCapturePerformance,
    getCapturePlacements,
    pauseCapturePlacement,
    resumeCapturePlacement,
    copyText,
    getCapturePerformanceDateRange: () => range,
    onPerformanceLoadError: options?.onPerformanceLoadError,
    onPlacementsLoadError: options?.onPlacementsLoadError,
    onPlacementActionError: options?.onPlacementActionError,
    onCopyPlacementLinkError: options?.onCopyPlacementLinkError,
    nowMs: () => options?.nowMs ?? Date.parse("2026-07-16T12:00:00.000Z"),
  })

  return {
    pageModule,
    getCapturePerformance,
    getCapturePlacements,
    pauseCapturePlacement,
    resumeCapturePlacement,
    copyText,
    setRange: (next: HomePerformanceDateRange) => {
      range = next
    },
  }
}

describe("createOperatorCapturePageModule", () => {
  it("starts idle with no view model before workspace sync", () => {
    const { pageModule } = createModule()
    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "idle",
      performanceLoadStatus: "idle",
      placementsLoadStatus: "idle",
      isGuestExperiencePreviewOpen: false,
      viewModel: null,
    })
  })

  it("derives Guest experience Active QR count including Smart Guest", async () => {
    const { pageModule } = createModule({
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 1,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/a",
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
          {
            qrCodeId: 2,
            qrType: "SmartGuest",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/b",
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
          {
            qrCodeId: 3,
            qrType: "WindowSticker",
            status: "Paused",
            qrLinkUrl: "https://tummly.example/scan/c",
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [
        { id: 42, locationName: "Camden", address: "12 High St" },
      ],
    })

    expect(pageModule.getSnapshot().viewModel?.guestExperience).toEqual({
      activeQrCount: 2,
      connectedOffersText: "No active offers",
      previewPlacementLabel: "Smart Guest",
      locationName: "Camden",
      locationAddress: "12 High St",
    })
    expect(pageModule.getSnapshot().isGuestExperiencePreviewOpen).toBe(false)
  })

  it("keeps Active QR count consistent after Pause and Resume", async () => {
    const { pageModule } = createModule({
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/counter-token",
            qrScans: 4,
            feedbackSubmitted: 2,
            marketingOptIns: 1,
            offerClaims: 0,
            lastScanAt: null,
          },
          {
            qrCodeId: 10,
            qrType: "SmartGuest",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/smart-guest-token",
            qrScans: 1,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(
      pageModule.getSnapshot().viewModel?.guestExperience.activeQrCount
    ).toBe(2)

    await expect(pageModule.pausePlacement(9)).resolves.toBe("paused")
    expect(
      pageModule.getSnapshot().viewModel?.guestExperience.activeQrCount
    ).toBe(1)

    await expect(pageModule.resumePlacement(9)).resolves.toBe("resumed")
    expect(
      pageModule.getSnapshot().viewModel?.guestExperience.activeQrCount
    ).toBe(2)
  })

  it("opens and closes the Guest experience preview overlay", async () => {
    const { pageModule } = createModule({
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 10,
            qrType: "SmartGuest",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/smart-guest-token",
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.openGuestExperiencePreview()
    expect(pageModule.getSnapshot().isGuestExperiencePreviewOpen).toBe(true)

    pageModule.closeGuestExperiencePreview()
    expect(pageModule.getSnapshot().isGuestExperiencePreviewOpen).toBe(false)
  })

  it("no-ops Guest experience preview open when Capture is not loaded", () => {
    const { pageModule } = createModule()

    pageModule.openGuestExperiencePreview()
    expect(pageModule.getSnapshot().isGuestExperiencePreviewOpen).toBe(false)
  })

  it("loads Capture performance KPIs for the selected location", async () => {
    const { pageModule, getCapturePerformance } = createModule({
      performance: emptyPerformanceResponse({
        qrScans: 10,
        qrScansPrevious: 5,
        feedbackSubmitted: 4,
        feedbackSubmittedPrevious: 2,
        marketingOptIns: 3,
        marketingOptInsPrevious: 1,
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [
        { id: 42, locationName: "Camden" },
        { id: 7, locationName: "Soho" },
      ],
    })

    expect(getCapturePerformance).toHaveBeenCalledOnce()
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.performanceLoadStatus).toBe("loaded")
    expect(snapshot.viewModel).toMatchObject({
      locationId: 42,
      locationName: "Camden",
      dateRangeLabel: "Last 7 days",
      performance: {
        isEmpty: false,
      },
    })
    expect(snapshot.viewModel?.performance.kpis).toHaveLength(5)
    expect(
      snapshot.viewModel?.performance.kpis.find((kpi) => kpi.id === "qr-scans")
    ).toMatchObject({
      primaryText: "10",
      trendPercent: 100,
    })
    expect(
      snapshot.viewModel?.performance.kpis.find(
        (kpi) => kpi.id === "form-starts"
      )
    ).toMatchObject({
      primaryText: "40%",
    })
  })

  it("loads QR placements rows for the same Capture date window", async () => {
    const { pageModule, getCapturePlacements, getCapturePerformance } =
      createModule({
        placements: emptyPlacementsResponse({
          placements: [
            {
              qrCodeId: 9,
              qrType: "CounterCard",
              status: "Active",
              qrLinkUrl: "https://tummly.example/scan/abc",
              qrScans: 4,
              feedbackSubmitted: 2,
              marketingOptIns: 1,
              offerClaims: 0,
              lastScanAt: "2026-07-16T11:00:00.000Z",
            },
            {
              qrCodeId: 10,
              qrType: "PackagingSticker",
              status: "Paused",
              qrLinkUrl: "https://tummly.example/scan/def",
              qrScans: 1,
              feedbackSubmitted: 0,
              marketingOptIns: 0,
              offerClaims: 0,
              lastScanAt: null,
            },
          ],
        }),
      })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(getCapturePlacements).toHaveBeenCalledOnce()
    expect(getCapturePerformance).toHaveBeenCalledOnce()
    expect(getCapturePlacements).toHaveBeenCalledWith(
      42,
      expect.any(String),
      expect.any(String)
    )
    const performanceArgs = getCapturePerformance.mock.calls.at(0)
    const placementsArgs = getCapturePlacements.mock.calls.at(0)
    expect(performanceArgs).toEqual(placementsArgs)

    const placements = pageModule.getSnapshot().viewModel?.placements
    expect(placements?.isEmpty).toBe(false)
    expect(placements?.rows).toHaveLength(2)
    expect(placements?.rows[0]).toMatchObject({
      placementLabel: "Counter card",
      status: "Active",
      qrScansText: "4 scans",
      lastScanText: "1 hour ago",
    })
    expect(placements?.rows[1]).toMatchObject({
      placementLabel: "Packaging sticker",
      status: "Paused",
      lastScanText: "—",
    })
  })

  it("maps placement windowed counts that sum to Capture performance KPI primaries", async () => {
    const { pageModule } = createModule({
      performance: emptyPerformanceResponse({
        qrScans: 5,
        feedbackSubmitted: 2,
        marketingOptIns: 1,
      }),
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 1,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/a",
            qrScans: 4,
            feedbackSubmitted: 2,
            marketingOptIns: 1,
            offerClaims: 0,
            lastScanAt: null,
          },
          {
            qrCodeId: 2,
            qrType: "SmartGuest",
            status: "Paused",
            qrLinkUrl: "https://tummly.example/scan/b",
            qrScans: 1,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const viewModel = pageModule.getSnapshot().viewModel
    const rows = viewModel?.placements.rows ?? []
    const sumScans = rows.reduce(
      (sum, row) => sum + Number.parseInt(row.qrScansText, 10),
      0
    )
    const sumFeedback = rows.reduce(
      (sum, row) => sum + Number.parseInt(row.feedbackSubmittedText, 10),
      0
    )
    const sumOptIns = rows.reduce(
      (sum, row) => sum + Number.parseInt(row.marketingOptInsText, 10),
      0
    )
    const sumClaims = rows.reduce(
      (sum, row) => sum + Number.parseInt(row.offerClaimsText, 10),
      0
    )

    expect(
      viewModel?.performance.kpis.find((kpi) => kpi.id === "qr-scans")
        ?.primaryText
    ).toBe(String(sumScans))
    expect(
      viewModel?.performance.kpis.find((kpi) => kpi.id === "feedback-submitted")
        ?.primaryText
    ).toBe(String(sumFeedback))
    expect(
      viewModel?.performance.kpis.find((kpi) => kpi.id === "marketing-opt-ins")
        ?.primaryText
    ).toBe(String(sumOptIns))
    expect(
      viewModel?.performance.kpis.find((kpi) => kpi.id === "offer-claims")
        ?.primaryText
    ).toBe(String(sumClaims))
  })

  it("shows empty Capture performance when the window has no activity", async () => {
    const { pageModule } = createModule()

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot().viewModel?.performance.isEmpty).toBe(true)
    expect(pageModule.getSnapshot().viewModel?.placements.isEmpty).toBe(true)
  })

  it("reloads performance and placements when the Capture date range changes", async () => {
    const { pageModule, getCapturePerformance, getCapturePlacements, setRange } =
      createModule({
        performance: emptyPerformanceResponse({ qrScans: 1 }),
        placements: emptyPlacementsResponse({
          placements: [
            {
              qrCodeId: 1,
              qrType: "SmartGuest",
              status: "Active",
              qrLinkUrl: "https://tummly.example/scan/sg",
              qrScans: 1,
              feedbackSubmitted: 0,
              marketingOptIns: 0,
              offerClaims: 0,
              lastScanAt: null,
            },
          ],
        }),
      })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    expect(getCapturePerformance).toHaveBeenCalledOnce()
    expect(getCapturePlacements).toHaveBeenCalledOnce()

    setRange({ kind: "preset", presetId: "last30" })
    await pageModule.reloadForCapturePerformanceDateRange()

    expect(getCapturePerformance).toHaveBeenCalledTimes(2)
    expect(getCapturePlacements).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().viewModel?.dateRangeLabel).toBe(
      "Last 30 days"
    )
  })

  it("pauses an Active placement and updates the row without changing its link", async () => {
    const { pageModule, pauseCapturePlacement } = createModule({
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/counter-token",
            qrScans: 4,
            feedbackSubmitted: 2,
            marketingOptIns: 1,
            offerClaims: 0,
            lastScanAt: "2026-07-16T11:00:00.000Z",
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    await expect(pageModule.pausePlacement(9)).resolves.toBe("paused")
    expect(pauseCapturePlacement).toHaveBeenCalledWith(42, 9)
    expect(pageModule.getSnapshot().viewModel?.placements.rows[0]).toMatchObject({
      qrCodeId: 9,
      status: "Paused",
      qrLinkUrl: "https://tummly.example/scan/counter-token",
    })
  })

  it("resumes a Paused placement and updates the row without changing its link", async () => {
    const { pageModule, resumeCapturePlacement } = createModule({
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 10,
            qrType: "SmartGuest",
            status: "Paused",
            qrLinkUrl: "https://tummly.example/scan/smart-guest-token",
            qrScans: 1,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    await expect(pageModule.resumePlacement(10)).resolves.toBe("resumed")
    expect(resumeCapturePlacement).toHaveBeenCalledWith(42, 10)
    expect(pageModule.getSnapshot().viewModel?.placements.rows[0]).toMatchObject({
      qrCodeId: 10,
      status: "Active",
      qrLinkUrl: "https://tummly.example/scan/smart-guest-token",
    })
  })

  it("surfaces pause failures without changing the row", async () => {
    const onPlacementActionError = vi.fn()
    const { pageModule, pauseCapturePlacement } = createModule({
      failPause: true,
      onPlacementActionError,
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/counter-token",
            qrScans: 4,
            feedbackSubmitted: 2,
            marketingOptIns: 1,
            offerClaims: 0,
            lastScanAt: "2026-07-16T11:00:00.000Z",
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const beforeRows = pageModule.getSnapshot().viewModel?.placements.rows
    await expect(pageModule.pausePlacement(9)).resolves.toBe("failed")

    expect(pauseCapturePlacement).toHaveBeenCalledWith(42, 9)
    expect(onPlacementActionError).toHaveBeenCalledWith(
      "Could not pause QR code. Please try again."
    )
    expect(pageModule.getSnapshot().viewModel?.placements.rows).toEqual(beforeRows)
  })

  it("surfaces resume failures without changing the row", async () => {
    const onPlacementActionError = vi.fn()
    const { pageModule, resumeCapturePlacement } = createModule({
      failResume: true,
      onPlacementActionError,
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 10,
            qrType: "SmartGuest",
            status: "Paused",
            qrLinkUrl: "https://tummly.example/scan/smart-guest-token",
            qrScans: 1,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const beforeRows = pageModule.getSnapshot().viewModel?.placements.rows
    await expect(pageModule.resumePlacement(10)).resolves.toBe("failed")

    expect(resumeCapturePlacement).toHaveBeenCalledWith(42, 10)
    expect(onPlacementActionError).toHaveBeenCalledWith(
      "Could not resume QR code. Please try again."
    )
    expect(pageModule.getSnapshot().viewModel?.placements.rows).toEqual(beforeRows)
  })

  it("copies a non-Smart Guest placement link", async () => {
    const { pageModule, copyText } = createModule({
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/counter-token",
            qrScans: 4,
            feedbackSubmitted: 2,
            marketingOptIns: 1,
            offerClaims: 0,
            lastScanAt: "2026-07-16T11:00:00.000Z",
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    await expect(pageModule.copyPlacementLink(9)).resolves.toBe("copied")
    expect(copyText).toHaveBeenCalledWith(
      "https://tummly.example/scan/counter-token"
    )
  })

  it("copies a Smart Guest placement link with the same generic success path", async () => {
    const { pageModule, copyText } = createModule({
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 11,
            qrType: "SmartGuest",
            status: "Paused",
            qrLinkUrl: "https://tummly.example/scan/smart-guest-token",
            qrScans: 1,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    await expect(pageModule.copyPlacementLink(11)).resolves.toBe("copied")
    expect(copyText).toHaveBeenCalledWith(
      "https://tummly.example/scan/smart-guest-token"
    )
  })

  it("surfaces copy failures without changing the placements rows", async () => {
    const onCopyPlacementLinkError = vi.fn()
    const { pageModule, copyText } = createModule({
      failCopy: true,
      onCopyPlacementLinkError,
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 10,
            qrType: "PackagingSticker",
            status: "Paused",
            qrLinkUrl: "https://tummly.example/scan/paused-token",
            qrScans: 1,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const beforeRows = pageModule.getSnapshot().viewModel?.placements.rows

    await expect(pageModule.copyPlacementLink(10)).resolves.toBe("failed")

    expect(copyText).toHaveBeenCalledWith(
      "https://tummly.example/scan/paused-token"
    )
    expect(onCopyPlacementLinkError).toHaveBeenCalledWith(
      "Could not copy link. Please try again."
    )
    expect(pageModule.getSnapshot().viewModel?.placements.rows).toEqual(beforeRows)
  })

  it("surfaces Smart Guest copy failures without changing the placements rows", async () => {
    const onCopyPlacementLinkError = vi.fn()
    const { pageModule, copyText } = createModule({
      failCopy: true,
      onCopyPlacementLinkError,
      placements: emptyPlacementsResponse({
        placements: [
          {
            qrCodeId: 11,
            qrType: "SmartGuest",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/smart-guest-token",
            qrScans: 2,
            feedbackSubmitted: 1,
            marketingOptIns: 1,
            offerClaims: 0,
            lastScanAt: "2026-07-16T12:00:00.000Z",
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const beforeRows = pageModule.getSnapshot().viewModel?.placements.rows

    await expect(pageModule.copyPlacementLink(11)).resolves.toBe("failed")

    expect(copyText).toHaveBeenCalledWith(
      "https://tummly.example/scan/smart-guest-token"
    )
    expect(onCopyPlacementLinkError).toHaveBeenCalledWith(
      "Could not copy link. Please try again."
    )
    expect(pageModule.getSnapshot().viewModel?.placements.rows).toEqual(beforeRows)
  })

  it("shows empty performance and toasts on load failure", async () => {
    const onPerformanceLoadError = vi.fn()
    const { pageModule } = createModule({
      failPerformance: true,
      onPerformanceLoadError,
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.performanceLoadStatus).toBe("error")
    expect(snapshot.placementsLoadStatus).toBe("loaded")
    expect(snapshot.viewModel?.performance.isEmpty).toBe(true)
    expect(onPerformanceLoadError).toHaveBeenCalledWith(
      "Could not load Capture performance. Please try again."
    )
  })

  it("shows empty placements chrome and toasts on placements load failure, without claiming zero Active QR codes", async () => {
    const onPlacementsLoadError = vi.fn()
    const { pageModule } = createModule({
      failPlacements: true,
      onPlacementsLoadError,
      performance: emptyPerformanceResponse({ qrScans: 2 }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.placementsLoadStatus).toBe("error")
    expect(snapshot.performanceLoadStatus).toBe("loaded")
    expect(snapshot.viewModel?.placements.isEmpty).toBe(true)
    expect(snapshot.viewModel?.performance.isEmpty).toBe(false)
    // Load failure is honestly unknown, not a real zero — must not equal a true empty list's 0.
    expect(snapshot.viewModel?.guestExperience.activeQrCount).toBe(null)
    expect(onPlacementsLoadError).toHaveBeenCalledWith(
      "Could not load QR placements. Please try again."
    )
  })

  it("shows Active QR count of zero for a true empty placements list (not a load failure)", async () => {
    const { pageModule } = createModule({
      placements: emptyPlacementsResponse({ placements: [] }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.placementsLoadStatus).toBe("loaded")
    expect(snapshot.viewModel?.placements.isEmpty).toBe(true)
    expect(snapshot.viewModel?.guestExperience.activeQrCount).toBe(0)
  })

  it("uses a fallback location name when the selected id is unknown", async () => {
    const { pageModule } = createModule()

    await pageModule.syncWorkspace({
      selectedLocationId: 99,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot().viewModel).toMatchObject({
      locationId: 99,
      locationName: "Location",
    })
  })

  it("clears to empty when selected location is null", async () => {
    const { pageModule } = createModule({
      performance: emptyPerformanceResponse({ qrScans: 3 }),
    })
    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    await pageModule.syncWorkspace({
      selectedLocationId: null,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot()).toEqual({
      loadStatus: "loaded",
      performanceLoadStatus: "idle",
      placementsLoadStatus: "idle",
      isGuestExperiencePreviewOpen: false,
      viewModel: null,
    })
  })
})

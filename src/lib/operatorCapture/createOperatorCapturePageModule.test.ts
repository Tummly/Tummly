import { describe, expect, it, vi } from "vitest"

import { createOperatorCapturePageModule } from "./createOperatorCapturePageModule"
import type {
  CaptureLocationSnapshotResponse,
} from "@/types/dashboard"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

const DEFAULT_RANGE: HomePerformanceDateRange = {
  kind: "preset",
  presetId: "last7",
}

function emptySnapshotResponse(
  overrides: Partial<CaptureLocationSnapshotResponse> = {}
): CaptureLocationSnapshotResponse {
  return {
    success: true,
    captureLocationStatus: "Active",
    qrScans: 0,
    qrScansPrevious: 0,
    feedbackSubmitted: 0,
    feedbackSubmittedPrevious: 0,
    marketingOptIns: 0,
    marketingOptInsPrevious: 0,
    offerClaims: 0,
    offerClaimsHasRealData: false,
    placements: [],
    lastJourneyUpdate: null,
    ...overrides,
  }
}

function createModule(options?: {
  snapshot?: CaptureLocationSnapshotResponse | (() => Promise<CaptureLocationSnapshotResponse>)
  range?: HomePerformanceDateRange
  onCaptureLoadError?: (message: string) => void
  onPlacementActionError?: (message: string) => void
  onCopyPlacementLinkError?: (message: string) => void
  onCreateDigitalGuestLinkError?: (message: string) => void
  failSnapshot?: boolean
  failPause?: boolean
  failResume?: boolean
  failRotate?: boolean
  failCopy?: boolean
  failArchive?: boolean
  archived?: import("@/types/dashboard").CaptureArchivedPlacementsResponse
  archiveCapturePlacement?: (
    locationId: number,
    qrCodeId: number
  ) => Promise<{
    qrCodeId: number
    status: "Archived"
    archivedAt: string
    archivedByDisplayName: string | null
  }>
  restoreCapturePlacement?: (
    locationId: number,
    qrCodeId: number
  ) => Promise<
    | { ok: true; qrCodeId: number; status: "Paused"; qrLinkUrl: string }
    | { ok: false; reason: "conflict" | "failed"; message: string }
  >
  createDigitalGuestLink?: (
    locationId: number,
    input: {
      linkName: string
      internalDescription?: string | null
      channel: string
      status: string
      locationId?: number
    }
  ) => Promise<
    | { ok: true; qrCodeId: number }
    | { ok: false; reason: "duplicate_link_name"; message: string }
    | { ok: false; reason: "failed"; message: string }
  >
  updatePlacementInternalDescription?: (
    locationId: number,
    qrCodeId: number,
    internalDescription: string | null
  ) => Promise<{
    qrCodeId: number
    internalDescription: string | null
    updatedAt: string
    updatedByDisplayName: string | null
  }>
  nowMs?: number
}) {
  const getCaptureLocationSnapshot = vi.fn(
    async (): Promise<CaptureLocationSnapshotResponse> => {
      if (options?.failSnapshot) {
        throw new Error("network")
      }
      if (typeof options?.snapshot === "function") {
        return options.snapshot()
      }
      return options?.snapshot ?? emptySnapshotResponse()
    }
  )
  const getArchivedCapturePlacements = vi.fn(async () => {
    return (
      options?.archived ?? {
        success: true as const,
        placements: [],
        totalCount: 0,
        page: 1,
        pageSize: 25,
        archiverOptions: [] as string[],
      }
    )
  })
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
  const rotateCapturePlacement = vi.fn(
    async (
      _locationId: number,
      qrCodeId: number
    ): Promise<{
      qrCodeId: number
      status: "Active" | "Paused"
      qrLinkUrl: string
    }> => {
      if (options?.failRotate) {
        throw new Error("rotate failed")
      }
      return {
        qrCodeId,
        status: "Active",
        qrLinkUrl: `https://example.test/scan/rotated-${qrCodeId}`,
      }
    }
  )
  const archiveCapturePlacement = vi.fn(
    async (locationId: number, qrCodeId: number) => {
      if (options?.failArchive) {
        throw new Error("archive failed")
      }
      if (options?.archiveCapturePlacement) {
        return options.archiveCapturePlacement(locationId, qrCodeId)
      }
      return {
        qrCodeId,
        status: "Archived" as const,
        archivedAt: "2026-07-30T12:00:00.000Z",
        archivedByDisplayName: "Test Operator",
      }
    }
  )
  const restoreCapturePlacement = vi.fn(
    async (locationId: number, qrCodeId: number) => {
      if (options?.restoreCapturePlacement) {
        return options.restoreCapturePlacement(locationId, qrCodeId)
      }
      return {
        ok: true as const,
        qrCodeId,
        status: "Paused" as const,
        qrLinkUrl: `https://example.test/scan/restored-${qrCodeId}`,
      }
    }
  )
  const createDigitalGuestLink = vi.fn(
    async (
      locationId: number,
      input: {
        linkName: string
        internalDescription?: string | null
        channel: string
        status: string
        locationId?: number
      }
    ) => {
      if (options?.createDigitalGuestLink) {
        return options.createDigitalGuestLink(locationId, input)
      }
      return { ok: true as const, qrCodeId: 99 }
    }
  )
  const updatePlacementInternalDescription = vi.fn(
    async (
      _locationId: number,
      qrCodeId: number,
      internalDescription: string | null
    ) => {
      if (options?.updatePlacementInternalDescription) {
        return options.updatePlacementInternalDescription(
          _locationId,
          qrCodeId,
          internalDescription
        )
      }
      return {
        qrCodeId,
        internalDescription,
        updatedAt: "2026-07-30T15:00:00.000Z",
        updatedByDisplayName: "Test Operator",
      }
    }
  )
  let range = options?.range ?? DEFAULT_RANGE

  const pageModule = createOperatorCapturePageModule({
    getCaptureLocationSnapshot,
    getArchivedCapturePlacements,
    pauseCapturePlacement,
    resumeCapturePlacement,
    rotateCapturePlacement,
    archiveCapturePlacement,
    restoreCapturePlacement,
    createDigitalGuestLink,
    updatePlacementInternalDescription,
    copyText,
    getCapturePerformanceDateRange: () => range,
    onCaptureLoadError: options?.onCaptureLoadError,
    onPlacementActionError: options?.onPlacementActionError,
    onCopyPlacementLinkError: options?.onCopyPlacementLinkError,
    onCreateDigitalGuestLinkError: options?.onCreateDigitalGuestLinkError,
    nowMs: () => options?.nowMs ?? Date.parse("2026-07-16T12:00:00.000Z"),
  })

  return {
    pageModule,
    getCaptureLocationSnapshot,
    getArchivedCapturePlacements,
    createDigitalGuestLink,
    pauseCapturePlacement,
    resumeCapturePlacement,
    rotateCapturePlacement,
    archiveCapturePlacement,
    restoreCapturePlacement,
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
      isGuestExperiencePreviewOpen: false,
      isGuestExperiencePreviewPickerOpen: false,
      guestExperiencePreviewPlacementLabel: null,
      guestExperiencePreviewPicker: {
        isOpen: false,
        groups: [],
        selectedQrCodeId: null,
        selectedLabel: null,
        canConfirm: false,
      },
      rotateConfirm: {
        isOpen: false,
        qrCodeId: null,
        placementLabel: "",
        locationName: "",
        status: null,
        lastScanText: "",
        printMaterialsAcknowledged: false,
        canConfirm: false,
      },
      pauseActivateConfirm: {
        isOpen: false,
        details: null,
      },
      viewModel: null,
    })
  })

  it("derives Guest experience Figma rows including Smart Guest and Needs attention", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        lastJourneyUpdate: {
          createdAt: "2026-07-14T13:00:00.000Z",
          guestName: "Jane Doe",
        },
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
      guestFormsText: "1 published form · Used by 2 of 2 active placements",
      qrPlacementsText: "2 of 3 placements active",
      connectedOffersText: "No active offers",
      needsAttentionText: "1 placements require action",
      lastJourneyUpdateText: "14 Jul 2026 by Jane Doe",
      previewEntry: { kind: "open-picker" },
      previewPlacementLabel: "Smart Guest",
      locationName: "Camden",
      locationAddress: "12 High St",
    })
    expect(pageModule.getSnapshot().viewModel?.digitalGuestLinks).toEqual({
      rows: [],
      isEmpty: true,
    })
    expect(
      pageModule.getSnapshot().viewModel?.performance.kpis.find(
        (kpi) => kpi.id === "qr-scans"
      )?.label
    ).toBe("Guest form opens")
    expect(pageModule.getSnapshot().isGuestExperiencePreviewOpen).toBe(false)
    expect(pageModule.getSnapshot().isGuestExperiencePreviewPickerOpen).toBe(
      false
    )
  })

  it("keeps Guest experience Active count consistent after Pause and Resume", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
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
      pageModule.getSnapshot().viewModel?.guestExperience.qrPlacementsText
    ).toBe("2 of 2 placements active")

    await expect(pageModule.pausePlacement(9)).resolves.toBe("paused")
    expect(
      pageModule.getSnapshot().viewModel?.guestExperience.qrPlacementsText
    ).toBe("1 of 2 placements active")
    expect(
      pageModule.getSnapshot().viewModel?.guestExperience.needsAttentionText
    ).toBe("1 placements require action")

    await expect(pageModule.resumePlacement(9)).resolves.toBe("resumed")
    expect(
      pageModule.getSnapshot().viewModel?.guestExperience.qrPlacementsText
    ).toBe("2 of 2 placements active")
  })

  it("opens Guest experience preview for a single Active/Paused code", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
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

    expect(pageModule.openGuestExperiencePreview()).toBe("opened")
    expect(pageModule.getSnapshot().isGuestExperiencePreviewOpen).toBe(true)
    expect(pageModule.getSnapshot().isGuestExperiencePreviewPickerOpen).toBe(
      false
    )

    pageModule.closeGuestExperiencePreview()
    expect(pageModule.getSnapshot().isGuestExperiencePreviewOpen).toBe(false)
  })

  it("opens Preview picker state when 2+ Active/Paused codes exist", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
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
            status: "Paused",
            qrLinkUrl: "https://tummly.example/scan/b",
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

    expect(pageModule.openGuestExperiencePreview()).toBe("picker")
    expect(pageModule.getSnapshot().isGuestExperiencePreviewPickerOpen).toBe(
      true
    )
    expect(pageModule.getSnapshot().isGuestExperiencePreviewOpen).toBe(false)
    expect(pageModule.getSnapshot().guestExperiencePreviewPicker).toEqual({
      isOpen: true,
      selectedQrCodeId: null,
      selectedLabel: null,
      canConfirm: false,
      groups: [
        {
          id: "qr-placements",
          label: "QR placements",
          options: [
            { qrCodeId: 1, label: "Counter card" },
            { qrCodeId: 2, label: "Smart Guest" },
          ],
        },
      ],
    })

    pageModule.closeGuestExperiencePreviewPicker()
    expect(pageModule.getSnapshot().isGuestExperiencePreviewPickerOpen).toBe(
      false
    )
    expect(pageModule.getSnapshot().guestExperiencePreviewPicker.isOpen).toBe(
      false
    )
  })

  it("confirms Preview picker selection into guest experience preview with grouped digital options", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 30,
            qrType: "DigitalGuestLink",
            status: "Active",
            linkName: "Zulu social",
            qrLinkUrl: "https://tummly.example/scan/zulu",
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
          {
            qrCodeId: 10,
            qrType: "WindowSticker",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/window",
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
          {
            qrCodeId: 31,
            qrType: "DigitalGuestLink",
            status: "Paused",
            linkName: "Alpha email",
            qrLinkUrl: "https://tummly.example/scan/alpha",
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

    expect(pageModule.openGuestExperiencePreview()).toBe("picker")
    expect(pageModule.confirmGuestExperiencePreviewPicker()).toBe("noop")

    pageModule.selectGuestExperiencePreviewPickerOption(31)
    expect(pageModule.getSnapshot().guestExperiencePreviewPicker).toMatchObject(
      {
        isOpen: true,
        selectedQrCodeId: 31,
        selectedLabel: "Alpha email",
        canConfirm: true,
        groups: [
          {
            id: "qr-placements",
            label: "QR placements",
            options: [{ qrCodeId: 10, label: "Window sticker" }],
          },
          {
            id: "digital-guest-links",
            label: "Digital guest links",
            options: [
              { qrCodeId: 31, label: "Alpha email" },
              { qrCodeId: 30, label: "Zulu social" },
            ],
          },
        ],
      }
    )

    expect(pageModule.confirmGuestExperiencePreviewPicker()).toBe("opened")
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.isGuestExperiencePreviewPickerOpen).toBe(false)
    expect(snapshot.guestExperiencePreviewPicker.isOpen).toBe(false)
    expect(snapshot.isGuestExperiencePreviewOpen).toBe(true)
    expect(snapshot.guestExperiencePreviewPlacementLabel).toBe("Alpha email")
  })

  it("row Preview skips the picker and opens that code only", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
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
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.openPlacementPreview(1)).toBe("opened")
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.isGuestExperiencePreviewPickerOpen).toBe(false)
    expect(snapshot.isGuestExperiencePreviewOpen).toBe(true)
    expect(snapshot.guestExperiencePreviewPlacementLabel).toBe("Counter card")
  })

  it("no-ops Guest experience preview open when Capture is not loaded", () => {
    const { pageModule } = createModule()

    expect(pageModule.openGuestExperiencePreview()).toBe("noop")
    expect(pageModule.getSnapshot().isGuestExperiencePreviewOpen).toBe(false)
  })

  it("no-ops Guest experience preview when zero Active/Paused codes", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({ placements: [] }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.openGuestExperiencePreview()).toBe("noop")
    expect(pageModule.getSnapshot().isGuestExperiencePreviewOpen).toBe(false)
  })

  it("labels Capture performance KPI and placement cells as Guest form opens", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        qrScans: 10,
        qrScansPrevious: 5,
        feedbackSubmitted: 4,
        feedbackSubmittedPrevious: 2,
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
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(
      pageModule.getSnapshot().viewModel?.performance.kpis.find(
        (kpi) => kpi.id === "qr-scans"
      )
    ).toMatchObject({
      label: "Guest form opens",
      primaryText: "10",
    })
    expect(
      pageModule.getSnapshot().viewModel?.placements.rows[0]?.qrScansText
    ).toBe("4 opens")
  })

  it("loads Capture performance KPIs for the selected location", async () => {
    const { pageModule, getCaptureLocationSnapshot } = createModule({
      snapshot: emptySnapshotResponse({
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

    expect(getCaptureLocationSnapshot).toHaveBeenCalledOnce()
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
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
      secondaryText: null,
    })
    expect(
      snapshot.viewModel?.performance.kpis.find(
        (kpi) => kpi.id === "form-starts"
      )
    ).toMatchObject({
      primaryText: "40%",
      secondaryText: "40% of scans",
    })
  })

  it("loads QR placements rows for the same Capture date window", async () => {
    const { pageModule, getCaptureLocationSnapshot } =
      createModule({
        snapshot: emptySnapshotResponse({
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

    expect(getCaptureLocationSnapshot).toHaveBeenCalledOnce()
    expect(getCaptureLocationSnapshot).toHaveBeenCalledWith(
      42,
      expect.any(String),
      expect.any(String)
    )
const placements = pageModule.getSnapshot().viewModel?.placements
    expect(placements?.isEmpty).toBe(false)
    expect(placements?.rows).toHaveLength(2)
    expect(placements?.rows[0]).toMatchObject({
      placementLabel: "Counter card",
      status: "Active",
      qrScansText: "4 opens",
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
      snapshot: emptySnapshotResponse({
        qrScans: 5,
        feedbackSubmitted: 2,
        marketingOptIns: 1,
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
    const { pageModule, getCaptureLocationSnapshot, setRange } =
      createModule({
        snapshot: emptySnapshotResponse({
        qrScans: 1,
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
    expect(getCaptureLocationSnapshot).toHaveBeenCalledOnce()

    setRange({ kind: "preset", presetId: "last30" })
    await pageModule.reloadForCapturePerformanceDateRange()

    expect(getCaptureLocationSnapshot).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().viewModel?.dateRangeLabel).toBe(
      "Last 30 days"
    )
  })

  it("pauses an Active placement and updates the row without changing its link", async () => {
    const { pageModule, pauseCapturePlacement } = createModule({
      snapshot: emptySnapshotResponse({
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
      snapshot: emptySnapshotResponse({
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
      snapshot: emptySnapshotResponse({
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
      snapshot: emptySnapshotResponse({
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
      snapshot: emptySnapshotResponse({
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
      snapshot: emptySnapshotResponse({
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
      snapshot: emptySnapshotResponse({
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
      snapshot: emptySnapshotResponse({
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

  it("sets body loadStatus to error and toasts on Capture location snapshot failure", async () => {
    const onCaptureLoadError = vi.fn()
    const { pageModule } = createModule({
      failSnapshot: true,
      onCaptureLoadError,
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("error")
    expect(snapshot.viewModel).toBeNull()
    expect(onCaptureLoadError).toHaveBeenCalledWith(
      "Could not load Capture. Please try again."
    )
  })

  it("retryLoad refetches the Capture location snapshot after failure", async () => {
    let shouldFail = true
    const onCaptureLoadError = vi.fn()
    const { pageModule, getCaptureLocationSnapshot } = createModule({
      snapshot: async () => {
        if (shouldFail) {
          throw new Error("network")
        }
        return emptySnapshotResponse({ qrScans: 4 })
      },
      onCaptureLoadError,
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    expect(pageModule.getSnapshot().loadStatus).toBe("error")
    expect(getCaptureLocationSnapshot).toHaveBeenCalledOnce()

    shouldFail = false
    await pageModule.retryLoad()

    expect(getCaptureLocationSnapshot).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(
      pageModule.getSnapshot().viewModel?.performance.kpis.find(
        (kpi) => kpi.id === "qr-scans"
      )?.primaryText
    ).toBe("4")
  })

  it("shows zero Active placements for a true empty placements list (not a load failure)", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({ placements: [] }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel?.placements.isEmpty).toBe(true)
    expect(snapshot.viewModel?.guestExperience.qrPlacementsText).toBe(
      "0 of 0 placements active"
    )
    expect(snapshot.viewModel?.guestExperience.previewEntry).toEqual({
      kind: "disabled",
    })
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
      snapshot: emptySnapshotResponse({ qrScans: 3 }),
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
      isGuestExperiencePreviewOpen: false,
      isGuestExperiencePreviewPickerOpen: false,
      guestExperiencePreviewPlacementLabel: null,
      guestExperiencePreviewPicker: {
        isOpen: false,
        groups: [],
        selectedQrCodeId: null,
        selectedLabel: null,
        canConfirm: false,
      },
      rotateConfirm: {
        isOpen: false,
        qrCodeId: null,
        placementLabel: "",
        locationName: "",
        status: null,
        lastScanText: "",
        printMaterialsAcknowledged: false,
        canConfirm: false,
      },
      pauseActivateConfirm: {
        isOpen: false,
        details: null,
      },
      viewModel: null,
    })
  })

  it("opens and closes the Placement Detail drawer for a catalog placement", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "DeliveryInsert",
            status: "Active",
            qrLinkUrl: "https://example.test/scan/delivery",
            qrScans: 10,
            feedbackSubmitted: 4,
            marketingOptIns: 2,
            offerClaims: 0,
            lastScanAt: "2026-07-01T12:00:00.000Z",
          },
        ],
      }),
      nowMs: Date.parse("2026-07-01T15:00:00.000Z"),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.openPlacementDetail(9)).toBe("opened")
    const openSnapshot = pageModule.getPlacementDetailModule().getSnapshot()
    expect(openSnapshot.isOpen).toBe(true)
    expect(openSnapshot.selectedQrCodeId).toBe(9)
    expect(openSnapshot.details).toMatchObject({
      kind: "catalog",
      title: "Delivery insert",
      status: "Active",
      locationName: "Camden",
      editGuestFormEnabled: false,
      previewGuestExperienceEnabled: true,
      canRotate: true,
      canArchive: true,
      detailsSectionTitle: "Placement details",
      typeFieldLabel: "Placement type",
      typeValue: "Delivery insert",
      channelLabel: null,
      connectedGuestForm: "Default guest feedback form",
      connectedOfferText: "No offers",
      assetsSectionTitle: "QR assets",
      showOrderPrintMaterials: true,
      orderPrintMaterialsEnabled: false,
      pauseActivateLabel: "Pause placement",
      guestFormOpensText: "10",
      feedbackSubmittedText: "4",
      submissionRateText: "40%",
    })
    expect(openSnapshot.details?.channelLabel).toBeNull()

    pageModule.closePlacementDetail()
    expect(pageModule.getPlacementDetailModule().getSnapshot()).toEqual({
      isOpen: false,
      selectedQrCodeId: null,
      details: null,
    })
  })

  it("exposes Smart Guest Detail drawer chrome with Rotate enabled", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 10,
            qrType: "SmartGuest",
            status: "Paused",
            qrLinkUrl: "https://example.test/scan/smart",
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

    expect(pageModule.openPlacementDetail(10)).toBe("opened")
    expect(pageModule.getPlacementDetailModule().getSnapshot().details).toMatchObject({
      kind: "smartGuest",
      title: "Smart Guest",
      canRotate: true,
      detailsSectionTitle: "Placement details",
      assetsSectionTitle: "QR assets",
      showOrderPrintMaterials: true,
      pauseActivateLabel: "Activate placement",
      submissionRateText: "—",
    })
  })

  it("exposes Digital guest link Detail drawer chrome without Rotate", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 11,
            qrType: "DigitalGuestLink",
            status: "Active",
            qrLinkUrl: "https://example.test/scan/digital",
            qrScans: 5,
            feedbackSubmitted: 1,
            marketingOptIns: 1,
            offerClaims: 0,
            lastScanAt: null,
            linkName: "Instagram bio",
            channelLabel: "Social media",
            internalDescription: "Promo post",
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.openPlacementDetail(11)).toBe("opened")
    const details = pageModule.getPlacementDetailModule().getSnapshot().details
    expect(details).toMatchObject({
      kind: "digital",
      title: "Instagram bio",
      canRotate: false,
      detailsSectionTitle: "Link details",
      typeFieldLabel: "Link type",
      typeValue: "Digital guest link",
      channelLabel: "Social media",
      assetsSectionTitle: "Link assets",
      showOrderPrintMaterials: false,
      descriptionDraft: "Promo post",
    })

    expect(pageModule.requestPlacementDetailRotate()).toBe("noop")
  })

  it("opens Pause confirm from the Detail drawer instead of stubbing", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://example.test/scan/counter",
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
    pageModule.openPlacementDetail(9)

    expect(pageModule.requestPlacementDetailActivate()).toBe("noop")
    expect(pageModule.requestPlacementDetailPause()).toBe("opened")
    expect(pageModule.getSnapshot().pauseActivateConfirm).toMatchObject({
      isOpen: true,
      details: {
        action: "pause",
        title: "Pause QR placement?",
        primaryLabel: "Pause placement",
        warningText:
          "Any printed materials using this QR code will remain in circulation but will not work while the placement is paused.",
      },
    })

    expect(pageModule.requestPlacementDetailRotate()).toBe("opened")
    expect(pageModule.getSnapshot().rotateConfirm.isOpen).toBe(true)

    pageModule.setPlacementDetailDescriptionDraft("Follow up note")
    expect(await pageModule.savePlacementDetailDescription()).toBe("saved")
    expect(
      pageModule.getPlacementDetailModule().getSnapshot().details?.descriptionDraft
    ).toBe("Follow up note")
    expect(
      pageModule.getPlacementDetailModule().getSnapshot().details?.lastUpdatedDisplay
    ).toContain("Test Operator")

    const archiveResult = await pageModule.requestPlacementDetailArchive()
    expect(archiveResult).toMatchObject({ outcome: "archived" })
    expect(pageModule.getPlacementDetailModule().getSnapshot().details?.status).toBe(
      "Archived"
    )
    expect(pageModule.getSnapshot().viewModel?.placements.rows).toEqual([])
  })

  it("opens Preview from the Detail drawer for the selected code", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "WindowSticker",
            status: "Active",
            qrLinkUrl: "https://example.test/scan/window",
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
    pageModule.openPlacementDetail(9)

    expect(pageModule.openPlacementDetailPreview()).toBe("opened")
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.isGuestExperiencePreviewOpen).toBe(true)
    expect(snapshot.guestExperiencePreviewPlacementLabel).toBe("Window sticker")
  })

  it("lists Digital guest links separately from QR placements and omits Smart Guest", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 1,
            qrType: "SmartGuest",
            status: "Active",
            qrLinkUrl: "https://tummly.example/scan/sg",
            qrScans: 2,
            feedbackSubmitted: 1,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
          {
            qrCodeId: 11,
            qrType: "DigitalGuestLink",
            status: "Active",
            linkName: "Instagram bio",
            channel: "SocialMedia",
            qrLinkUrl: "https://tummly.example/scan/dgl",
            qrScans: 45,
            feedbackSubmitted: 12,
            marketingOptIns: 8,
            offerClaims: 0,
            lastScanAt: "2026-07-07T12:00:00.000Z",
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const viewModel = pageModule.getSnapshot().viewModel
    expect(viewModel?.placements.rows.map((row) => row.qrCodeId)).toEqual([1])
    expect(viewModel?.digitalGuestLinks).toEqual({
      isEmpty: false,
      rows: [
        {
          qrCodeId: 11,
          guestLinkLabel: "Instagram bio",
          status: "Active",
          qrLinkUrl: "https://tummly.example/scan/dgl",
          qrScansText: "45 opens",
          feedbackSubmittedText: "12 feedback",
          marketingOptInsText: "8 opt-ins",
          offerClaimsText: "0 claims",
          lastScanText: "9 days ago",
        },
      ],
    })
  })

  it("create Digital guest link success refreshes body and opens Detail drawer", async () => {
    let placements = emptySnapshotResponse({
      placements: [
        {
          qrCodeId: 1,
          qrType: "SmartGuest",
          status: "Active",
          qrLinkUrl: "https://tummly.example/scan/sg",
          qrScans: 0,
          feedbackSubmitted: 0,
          marketingOptIns: 0,
          offerClaims: 0,
          lastScanAt: null,
        },
      ],
    })

    const { pageModule, createDigitalGuestLink } = createModule({
      snapshot: async () => placements,
      createDigitalGuestLink: async () => ({ ok: true, qrCodeId: 55 }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    placements = emptySnapshotResponse({
      placements: [
        {
          qrCodeId: 1,
          qrType: "SmartGuest",
          status: "Active",
          qrLinkUrl: "https://tummly.example/scan/sg",
          qrScans: 0,
          feedbackSubmitted: 0,
          marketingOptIns: 0,
          offerClaims: 0,
          lastScanAt: null,
        },
        {
          qrCodeId: 55,
          qrType: "DigitalGuestLink",
          status: "Active",
          linkName: "WhatsApp promo",
          channel: "WhatsApp",
          internalDescription: "July blast",
          qrLinkUrl: "https://tummly.example/scan/new",
          qrScans: 0,
          feedbackSubmitted: 0,
          marketingOptIns: 0,
          offerClaims: 0,
          lastScanAt: null,
        },
      ],
    })

    const result = await pageModule.createDigitalGuestLink({
      linkName: "WhatsApp promo",
      internalDescription: "July blast",
      channel: "WhatsApp",
      status: "Active",
    })

    expect(result).toBe("created")
    expect(createDigitalGuestLink).toHaveBeenCalledWith(42, {
      linkName: "WhatsApp promo",
      internalDescription: "July blast",
      channel: "WhatsApp",
      status: "Active",
    })
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.viewModel?.digitalGuestLinks.rows).toHaveLength(1)
    expect(pageModule.getPlacementDetailModule().getSnapshot().isOpen).toBe(true)
    expect(pageModule.getPlacementDetailModule().getSnapshot().selectedQrCodeId).toBe(55)
    expect(pageModule.getPlacementDetailModule().getSnapshot().details?.title).toBe("WhatsApp promo")
  })

  it("create Digital guest link duplicate Link name keeps dialog signal without opening drawer", async () => {
    const onCreateDigitalGuestLinkError = vi.fn()
    const { pageModule } = createModule({
      createDigitalGuestLink: async () => ({
        ok: false,
        reason: "duplicate_link_name",
        message:
          "A digital guest link with this name already exists at this location.",
      }),
      onCreateDigitalGuestLinkError,
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const result = await pageModule.createDigitalGuestLink({
      linkName: "Summer Promo",
      channel: "SocialMedia",
      status: "Active",
    })

    expect(result).toBe("duplicate_link_name")
    expect(onCreateDigitalGuestLinkError).not.toHaveBeenCalled()
    expect(pageModule.getPlacementDetailModule().getSnapshot().isOpen).toBe(false)
  })

  it("opens Rotate confirm for catalog and Smart Guest; omits digital; requires acknowledgment to remint", async () => {
    const onPlacementActionError = vi.fn()
    const { pageModule, rotateCapturePlacement } = createModule({
      onPlacementActionError,
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://example.test/scan/counter-old",
            qrScans: 4,
            feedbackSubmitted: 1,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: "2026-07-01T12:00:00.000Z",
          },
          {
            qrCodeId: 10,
            qrType: "SmartGuest",
            status: "Paused",
            qrLinkUrl: "https://example.test/scan/smart-old",
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
          {
            qrCodeId: 11,
            qrType: "DigitalGuestLink",
            status: "Active",
            linkName: "Instagram bio",
            channel: "SocialMedia",
            qrLinkUrl: "https://example.test/scan/digital",
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
        ],
      }),
      nowMs: Date.parse("2026-07-01T15:00:00.000Z"),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.requestRotate(11)).toBe("noop")
    expect(pageModule.getSnapshot().rotateConfirm.isOpen).toBe(false)

    expect(pageModule.requestRotate(9)).toBe("opened")
    expect(pageModule.getSnapshot().rotateConfirm).toMatchObject({
      isOpen: true,
      qrCodeId: 9,
      placementLabel: "Counter card",
      locationName: "Camden",
      status: "Active",
      lastScanText: "3 hours ago",
      printMaterialsAcknowledged: false,
      canConfirm: false,
    })

    await expect(pageModule.confirmRotate()).resolves.toBe("noop")
    expect(rotateCapturePlacement).not.toHaveBeenCalled()

    pageModule.setRotatePrintMaterialsAcknowledged(true)
    expect(pageModule.getSnapshot().rotateConfirm.canConfirm).toBe(true)

    await expect(pageModule.confirmRotate()).resolves.toBe("rotated")
    expect(rotateCapturePlacement).toHaveBeenCalledWith(42, 9)
    expect(pageModule.getSnapshot().rotateConfirm.isOpen).toBe(false)
    expect(pageModule.getPlacementDetailModule().getSnapshot()).toMatchObject({
      isOpen: true,
      selectedQrCodeId: 9,
    })
    expect(
      pageModule.getSnapshot().viewModel?.placements.rows.find(
        (row) => row.qrCodeId === 9
      )
    ).toBeDefined()
    const counterFact = pageModule.getPlacementDetailModule().getSnapshot()
      .details
    expect(counterFact?.status).toBe("Active")

    pageModule.closePlacementDetail()
    expect(pageModule.requestRotate(10)).toBe("opened")
    pageModule.setRotatePrintMaterialsAcknowledged(true)
    rotateCapturePlacement.mockResolvedValueOnce({
      qrCodeId: 10,
      status: "Paused",
      qrLinkUrl: "https://example.test/scan/smart-rotated",
    })
    await expect(pageModule.confirmRotate()).resolves.toBe("rotated")
    expect(pageModule.getPlacementDetailModule().getSnapshot().isOpen).toBe(true)
    expect(pageModule.getPlacementDetailModule().getSnapshot().selectedQrCodeId).toBe(
      10
    )

    pageModule.openPlacementDetail(11)
    expect(pageModule.requestPlacementDetailRotate()).toBe("noop")
  })

  it("keeps Detail drawer open and refreshes after Rotate from drawer; surfaces failures without remint", async () => {
    const onPlacementActionError = vi.fn()
    const { pageModule, rotateCapturePlacement } = createModule({
      failRotate: true,
      onPlacementActionError,
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "DeliveryInsert",
            status: "Active",
            qrLinkUrl: "https://example.test/scan/delivery-old",
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
    pageModule.openPlacementDetail(9)
    expect(pageModule.requestPlacementDetailRotate()).toBe("opened")
    expect(pageModule.getSnapshot().rotateConfirm.isOpen).toBe(true)
    expect(pageModule.getPlacementDetailModule().getSnapshot().isOpen).toBe(true)

    pageModule.setRotatePrintMaterialsAcknowledged(true)
    await expect(pageModule.confirmRotate()).resolves.toBe("failed")
    expect(onPlacementActionError).toHaveBeenCalledWith(
      "Could not rotate QR code. Please try again."
    )
    expect(pageModule.getSnapshot().rotateConfirm.isOpen).toBe(true)
    expect(pageModule.getPlacementDetailModule().getSnapshot().isOpen).toBe(true)
    expect(
      pageModule.getSnapshot().viewModel?.placements.rows[0]
    ).toBeDefined()

    pageModule.cancelRotateConfirm()
    expect(pageModule.getSnapshot().rotateConfirm.isOpen).toBe(false)

    rotateCapturePlacement.mockImplementationOnce(
      async (_locationId: number, qrCodeId: number) => ({
        qrCodeId,
        status: "Active" as const,
        qrLinkUrl: "https://example.test/scan/delivery-new",
      })
    )
    expect(pageModule.requestPlacementDetailRotate()).toBe("opened")
    pageModule.setRotatePrintMaterialsAcknowledged(true)
    await expect(pageModule.confirmRotate()).resolves.toBe("rotated")
    expect(pageModule.getPlacementDetailModule().getSnapshot().isOpen).toBe(true)
    expect(pageModule.getPlacementDetailModule().getSnapshot().selectedQrCodeId).toBe(
      9
    )
  })

  it("confirms Pause from the table: updates status, opens Detail drawer, returns toast", async () => {
    const { pageModule, pauseCapturePlacement } = createModule({
      snapshot: emptySnapshotResponse({
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

    expect(pageModule.requestPauseConfirm(9)).toBe("opened")
    expect(pageModule.getSnapshot().pauseActivateConfirm.isOpen).toBe(true)
    expect(pageModule.getPlacementDetailModule().getSnapshot().isOpen).toBe(false)

    await expect(pageModule.confirmPauseActivate()).resolves.toEqual({
      outcome: "paused",
      toastMessage:
        "Counter card is now paused. You can activate it again at any time.",
    })

    expect(pauseCapturePlacement).toHaveBeenCalledWith(42, 9)
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.pauseActivateConfirm.isOpen).toBe(false)
    expect(snapshot.viewModel?.placements.rows[0]?.status).toBe("Paused")
    expect(pageModule.getPlacementDetailModule().getSnapshot()).toMatchObject({
      isOpen: true,
      selectedQrCodeId: 9,
      details: { status: "Paused", title: "Counter card" },
    })
  })

  it("rejects per-code Pause/Activate while Capture location status is Paused", async () => {
    const { pageModule, pauseCapturePlacement, resumeCapturePlacement } =
      createModule({
        snapshot: emptySnapshotResponse({
          captureLocationStatus: "Paused",
          placements: [
            {
              qrCodeId: 9,
              qrType: "CounterCard",
              status: "Paused",
              qrLinkUrl: "https://tummly.example/scan/counter-token",
              qrScans: 0,
              feedbackSubmitted: 0,
              marketingOptIns: 0,
              offerClaims: 0,
              lastScanAt: null,
            },
            {
              qrCodeId: 10,
              qrType: "SmartGuest",
              status: "Active",
              qrLinkUrl: "https://tummly.example/scan/sg-token",
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

    expect(pageModule.getSnapshot().viewModel?.captureLocationStatus).toBe(
      "Paused"
    )
    expect(pageModule.getSnapshot().viewModel?.perCodePauseActivateLocked).toBe(
      true
    )
    expect(pageModule.requestPauseConfirm(10)).toBe("noop")
    expect(pageModule.requestActivateConfirm(9)).toBe("noop")
    expect(pageModule.openPlacementDetail(9)).toBe("opened")
    expect(
      pageModule.getPlacementDetailModule().getSnapshot().details?.canPauseOrActivate
    ).toBe(false)
    expect(pageModule.requestPlacementDetailActivate()).toBe("noop")
    expect(pauseCapturePlacement).not.toHaveBeenCalled()
    expect(resumeCapturePlacement).not.toHaveBeenCalled()
  })

  it("cancels Pause confirm without changing status", async () => {
    const { pageModule, pauseCapturePlacement } = createModule({
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://example.test/scan/counter",
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

    pageModule.requestPauseConfirm(9)
    pageModule.cancelPauseActivateConfirm()

    expect(pageModule.getSnapshot().pauseActivateConfirm.isOpen).toBe(false)
    expect(pauseCapturePlacement).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().viewModel?.placements.rows[0]?.status).toBe(
      "Active"
    )
  })

  it("confirms Activate from an open Detail drawer and refreshes it in place", async () => {
    const { pageModule, resumeCapturePlacement } = createModule({
      snapshot: emptySnapshotResponse({
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

    expect(pageModule.openPlacementDetail(10)).toBe("opened")
    expect(pageModule.requestPlacementDetailActivate()).toBe("opened")
    expect(pageModule.getSnapshot().pauseActivateConfirm.details).toMatchObject({
      action: "activate",
      title: "Activate QR placement?",
      primaryLabel: "Activate placement",
    })

    await expect(pageModule.confirmPauseActivate()).resolves.toEqual({
      outcome: "activated",
      toastMessage: "Smart Guest is now active. Guests can use it again.",
    })

    expect(resumeCapturePlacement).toHaveBeenCalledWith(42, 10)
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.pauseActivateConfirm.isOpen).toBe(false)
    expect(pageModule.getPlacementDetailModule().getSnapshot()).toMatchObject({
      isOpen: true,
      selectedQrCodeId: 10,
      details: { status: "Active", title: "Smart Guest" },
    })
  })

  it("surfaces confirm Pause failures without changing status or opening the drawer", async () => {
    const onPlacementActionError = vi.fn()
    const { pageModule } = createModule({
      failPause: true,
      onPlacementActionError,
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "DigitalGuestLink",
            status: "Active",
            qrLinkUrl: "https://example.test/scan/digital",
            qrScans: 1,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
            linkName: "Instagram bio",
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.requestPauseConfirm(9)).toBe("opened")
    expect(pageModule.getSnapshot().pauseActivateConfirm.details).toMatchObject({
      title: "Pause digital guest link?",
      warningText: null,
      primaryLabel: "Pause link",
    })

    const beforeRows = pageModule.getSnapshot().viewModel?.placements.rows
    await expect(pageModule.confirmPauseActivate()).resolves.toBe("failed")

    expect(onPlacementActionError).toHaveBeenCalledWith(
      "Could not pause QR code. Please try again."
    )
    expect(pageModule.getSnapshot().viewModel?.placements.rows).toEqual(beforeRows)
    expect(pageModule.getPlacementDetailModule().getSnapshot().isOpen).toBe(false)
    expect(pageModule.getSnapshot().pauseActivateConfirm.isOpen).toBe(false)
  })

  it("no-ops Pause confirm for non-Active codes and Activate for non-Paused", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://example.test/scan/counter",
            qrScans: 1,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastScanAt: null,
          },
          {
            qrCodeId: 10,
            qrType: "SmartGuest",
            status: "Paused",
            qrLinkUrl: "https://example.test/scan/smart",
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

    expect(pageModule.requestPauseConfirm(10)).toBe("noop")
    expect(pageModule.requestActivateConfirm(9)).toBe("noop")
    expect(pageModule.getSnapshot().pauseActivateConfirm.isOpen).toBe(false)
  })

  it("restores from Archive via page orchestration and opens Placement Detail without folding archive into the live snapshot", async () => {
    const { pageModule, restoreCapturePlacement } = createModule({
      archived: {
        success: true,
        placements: [
          {
            qrCodeId: 7,
            locationId: 42,
            locationName: "Camden",
            qrType: "DigitalGuestLink",
            status: "Archived",
            linkName: "Summer promo",
            channel: "SocialMedia",
            internalDescription: null,
            qrLinkUrl: "https://example.test/scan/summer",
            archivedAt: "2026-07-24T10:00:00.000Z",
            archivedByDisplayName: "Mohamed Mahmoud",
            qrScans: 12,
            feedbackSubmitted: 4,
            lastScanAt: null,
            canRestore: true,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 25,
        archiverOptions: ["Mohamed Mahmoud"],
      },
    })

    const archive = pageModule.getArchiveModule()
    const liveListener = vi.fn()
    pageModule.subscribe(liveListener)

    await archive.enter({
      returnPath: "/multi-dashboard/capture/locations/42",
      preselectedLocationId: 42,
      showLocationFilter: true,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    // Archive load must not notify live Capture subscribers.
    expect(liveListener).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot()).not.toHaveProperty("archive")
    expect(pageModule.getSnapshot()).not.toHaveProperty("restoreConfirm")
    expect(pageModule.getSnapshot()).not.toHaveProperty("placementDetailDrawer")

    liveListener.mockClear()
    archive.setSearchQuery("summer")
    expect(liveListener).not.toHaveBeenCalled()

    expect(archive.requestRestore(7)).toBe("opened")
    expect(liveListener).not.toHaveBeenCalled()

    const restored = await pageModule.confirmRestore()
    expect(restored).toMatchObject({ outcome: "restored" })
    expect(restoreCapturePlacement).toHaveBeenCalledWith(42, 7)
    expect(archive.getSnapshot().archive?.rows).toEqual([])
    expect(pageModule.getPlacementDetailModule().getSnapshot()).toMatchObject({
      isOpen: true,
      details: { status: "Paused", title: "Summer promo" },
    })
    // Restore opens Detail only — must not relay into live Capture publish.
    expect(liveListener).not.toHaveBeenCalled()
  })

  it("description draft keystrokes notify Detail subscribers only, not live Capture", async () => {
    const { pageModule } = createModule({
      snapshot: emptySnapshotResponse({
        placements: [
          {
            qrCodeId: 9,
            qrType: "CounterCard",
            status: "Active",
            qrLinkUrl: "https://example.test/scan/counter",
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
    pageModule.openPlacementDetail(9)

    const liveListener = vi.fn()
    const detailListener = vi.fn()
    pageModule.subscribe(liveListener)
    pageModule.getPlacementDetailModule().subscribe(detailListener)

    pageModule.setPlacementDetailDescriptionDraft("Typing…")
    expect(detailListener).toHaveBeenCalledTimes(1)
    expect(liveListener).not.toHaveBeenCalled()
    expect(
      pageModule.getPlacementDetailModule().getSnapshot().details
        ?.descriptionDraft
    ).toBe("Typing…")
  })

  it("opens Placement Detail from an archived row through page orchestration", async () => {
    const { pageModule } = createModule({
      archived: {
        success: true,
        placements: [
          {
            qrCodeId: 8,
            locationId: 42,
            locationName: "Camden",
            qrType: "CounterCard",
            status: "Archived",
            linkName: null,
            channel: null,
            internalDescription: "Front counter",
            qrLinkUrl: "https://example.test/scan/counter",
            archivedAt: "2026-07-10T10:00:00.000Z",
            archivedByDisplayName: "Ada",
            qrScans: 2,
            feedbackSubmitted: 1,
            lastScanAt: null,
            canRestore: false,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 25,
        archiverOptions: ["Ada"],
      },
    })

    await pageModule.getArchiveModule().enter({
      returnPath: "/single-dashboard/capture",
      showLocationFilter: false,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.openArchivePlacementDetail(8)).toBe("opened")
    expect(pageModule.getPlacementDetailModule().getSnapshot()).toMatchObject({
      isOpen: true,
      details: {
        status: "Archived",
        title: "Counter card",
        descriptionDraft: "Front counter",
      },
    })
  })
})

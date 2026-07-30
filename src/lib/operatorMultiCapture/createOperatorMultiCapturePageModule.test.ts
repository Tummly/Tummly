import { describe, expect, it, vi } from "vitest"

import { createOperatorMultiCapturePageModule } from "./createOperatorMultiCapturePageModule"
import type {
  CaptureLocationsResponse,
  CaptureOverviewResponse,
  CapturePreviewOptionsResponse,
} from "@/types/dashboard"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import { emptySelection } from "@/lib/operatorFilterSheet"
import { captureLocationsFilterSheetSchema } from "./captureLocationsFilterSheetSchema"
import type {
  CreateDigitalGuestLinkAdapterResult,
  CreateDigitalGuestLinkModuleInput,
} from "@/lib/operatorCapture/createOperatorCapturePageModule"

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
        pauseRestoreQrCodeCount: 0,
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

function emptyPreviewOptionsResponse(
  overrides: Partial<CapturePreviewOptionsResponse> = {}
): CapturePreviewOptionsResponse {
  return {
    items: [],
    ...overrides,
  }
}

function createModule(options?: {
  overview?: CaptureOverviewResponse | (() => Promise<CaptureOverviewResponse>)
  locations?:
    | CaptureLocationsResponse
    | (() => Promise<CaptureLocationsResponse>)
  previewOptions?:
    | CapturePreviewOptionsResponse
    | ((locationId: number) => Promise<CapturePreviewOptionsResponse>)
  createDigitalGuestLink?: (
    locationId: number,
    input: CreateDigitalGuestLinkModuleInput
  ) => Promise<CreateDigitalGuestLinkAdapterResult>
  pauseLocationCapture?: (
    locationId: number
  ) => Promise<
    | { ok: true; status: "Active" | "Paused"; pauseRestoreQrCodeCount: number }
    | { ok: false; message: string }
  >
  activateLocationCapture?: (
    locationId: number
  ) => Promise<
    | { ok: true; status: "Active" | "Paused"; pauseRestoreQrCodeCount: number }
    | { ok: false; message: string }
  >
  range?: HomePerformanceDateRange
  onOverviewLoadError?: (message: string) => void
  onLocationsLoadError?: (message: string) => void
  onCreateDigitalGuestLinkError?: (message: string) => void
  onDigitalGuestLinkCreated?: (message: string) => void
  onLocationCaptureError?: (message: string) => void
  syncSelectedLocation?: (locationId: number) => void
  navigateToCaptureLocation?: (
    locationId: number,
    navOptions?: { openPlacementDetailQrCodeId?: number }
  ) => void
  canManageLocationCapture?: () => boolean
  failOverview?: boolean
  failLocations?: boolean
  failPreviewOptions?: boolean
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
  const getCapturePreviewOptions = vi.fn(
    async (_locationId: number): Promise<CapturePreviewOptionsResponse> => {
      if (options?.failPreviewOptions) {
        throw new Error("network")
      }
      if (typeof options?.previewOptions === "function") {
        return options.previewOptions(_locationId)
      }
      return options?.previewOptions ?? emptyPreviewOptionsResponse()
    }
  )
  const createDigitalGuestLink = vi.fn(
    async (
      locationId: number,
      input: CreateDigitalGuestLinkModuleInput
    ): Promise<CreateDigitalGuestLinkAdapterResult> => {
      if (options?.createDigitalGuestLink) {
        return options.createDigitalGuestLink(locationId, input)
      }
      return { ok: true, qrCodeId: 99 }
    }
  )
  const pauseLocationCapture = vi.fn(
    async (locationId: number) => {
      if (options?.pauseLocationCapture) {
        return options.pauseLocationCapture(locationId)
      }
      return {
        ok: true as const,
        status: "Paused" as const,
        pauseRestoreQrCodeCount: 2,
      }
    }
  )
  const activateLocationCapture = vi.fn(
    async (locationId: number) => {
      if (options?.activateLocationCapture) {
        return options.activateLocationCapture(locationId)
      }
      return {
        ok: true as const,
        status: "Active" as const,
        pauseRestoreQrCodeCount: 0,
      }
    }
  )
  const onOverviewLoadError = vi.fn(options?.onOverviewLoadError)
  const onLocationsLoadError = vi.fn(options?.onLocationsLoadError)
  const onCreateDigitalGuestLinkError = vi.fn(
    options?.onCreateDigitalGuestLinkError
  )
  const onDigitalGuestLinkCreated = vi.fn(options?.onDigitalGuestLinkCreated)
  const onLocationCaptureError = vi.fn(options?.onLocationCaptureError)
  const syncSelectedLocation = vi.fn(options?.syncSelectedLocation)
  const navigateToCaptureLocation = vi.fn(options?.navigateToCaptureLocation)
  const canManageLocationCapture = vi.fn(
    options?.canManageLocationCapture ?? (() => true)
  )
  const scheduleReady =
    options?.scheduleReady ?? vi.fn(() => Promise.resolve())

  const pageModule = createOperatorMultiCapturePageModule({
    getCaptureOverview,
    getCaptureLocations,
    getCapturePreviewOptions,
    createDigitalGuestLink,
    pauseLocationCapture,
    activateLocationCapture,
    getMultiCaptureOverviewDateRange: () => options?.range ?? DEFAULT_RANGE,
    syncSelectedLocation,
    navigateToCaptureLocation,
    canManageLocationCapture,
    onOverviewLoadError,
    onLocationsLoadError,
    onCreateDigitalGuestLinkError,
    onDigitalGuestLinkCreated,
    onLocationCaptureError,
    scheduleReady,
    debounceMs: options?.debounceMs ?? 0,
  })

  return {
    pageModule,
    getCaptureOverview,
    getCaptureLocations,
    getCapturePreviewOptions,
    createDigitalGuestLink,
    pauseLocationCapture,
    activateLocationCapture,
    syncSelectedLocation,
    navigateToCaptureLocation,
    canManageLocationCapture,
    onOverviewLoadError,
    onLocationsLoadError,
    onCreateDigitalGuestLinkError,
    onDigitalGuestLinkCreated,
    onLocationCaptureError,
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
      canCreateDigitalGuestLink: false,
      createDialog: { isOpen: false },
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
    expect(snapshot.canCreateDigitalGuestLink).toBe(true)
    expect(snapshot.viewModel?.overview.isNoLocations).toBe(false)
    expect(snapshot.viewModel?.overview.kpis).toHaveLength(6)
    expect(snapshot.viewModel?.locationPerformance.rows).toHaveLength(1)
    expect(snapshot.viewModel?.locationPerformance.rows[0]).toMatchObject({
      locationName: "Camden",
      submissionRateText: "40%",
      qrScansText: "10 opens",
    })
    expect(snapshot.viewModel?.locationPerformance.showToolbar).toBe(true)
    expect(snapshot.viewModel?.locationPerformance.showPagination).toBe(true)
  })

  it("reloads overview and list when the Multi Capture overview date range changes", async () => {
    let range: HomePerformanceDateRange = DEFAULT_RANGE
    const getCaptureOverview = vi.fn(async () => emptyOverviewResponse())
    const getCaptureLocations = vi.fn(async () => emptyLocationsResponse())
    const getCapturePreviewOptions = vi.fn(async () =>
      emptyPreviewOptionsResponse()
    )
    const createDigitalGuestLink = vi.fn(async () => ({
      ok: true as const,
      qrCodeId: 1,
    }))
    const pageModule = createOperatorMultiCapturePageModule({
      getCaptureOverview,
      getCaptureLocations,
      getCapturePreviewOptions,
      createDigitalGuestLink,
      pauseLocationCapture: async () => ({
        ok: true,
        status: "Paused",
        pauseRestoreQrCodeCount: 0,
      }),
      activateLocationCapture: async () => ({
        ok: true,
        status: "Active",
        pauseRestoreQrCodeCount: 0,
      }),
      getMultiCaptureOverviewDateRange: () => range,
      syncSelectedLocation: vi.fn(),
      navigateToCaptureLocation: vi.fn(),
      canManageLocationCapture: () => true,
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

  it("exposes per-row actions with Create live, Preview from active count, and Pause for authorized Active rows", async () => {
    const { pageModule } = createModule({
      locations: emptyLocationsResponse({
        items: [
          {
            locationId: 1,
            locationName: "Camden",
            status: "Active",
            activePlacementsCount: 2,
        pauseRestoreQrCodeCount: 0,
        qrScans: 10,
            feedbackSubmitted: 4,
            marketingOptIns: 2,
            offerClaims: 0,
            lastActivityAt: "2026-07-16T11:00:00.000Z",
          },
          {
            locationId: 2,
            locationName: "Soho",
            status: "Paused",
            activePlacementsCount: 0,
      pauseRestoreQrCodeCount: 0,
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastActivityAt: null,
          },
        ],
        totalCount: 2,
      }),
    })

    await pageModule.syncWorkspace({
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })

    expect(pageModule.getLocationRowActions(1)).toEqual([
      {
        id: "view-location-capture",
        label: "View location capture",
        enabled: true,
      },
      {
        id: "create-digital-guest-link",
        label: "Create digital guest link",
        enabled: true,
      },
      {
        id: "preview-guest-experience",
        label: "Preview guest experience",
        enabled: true,
      },
      {
        id: "order-print-materials",
        label: "Order print materials",
        enabled: false,
      },
      {
        id: "pause-location-capture",
        label: "Pause location capture",
        enabled: true,
      },
    ])

    expect(pageModule.getLocationRowActions(2)).toEqual([
      {
        id: "view-location-capture",
        label: "View location capture",
        enabled: true,
      },
      {
        id: "create-digital-guest-link",
        label: "Create digital guest link",
        enabled: true,
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
      {
        id: "activate-location-capture",
        label: "Activate location capture",
        enabled: true,
      },
    ])
  })

  it("hides Pause and Activate location capture when unauthorized", async () => {
    const { pageModule } = createModule({
      canManageLocationCapture: () => false,
    })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })

    const ids = pageModule.getLocationRowActions(1).map((action) => action.id)
    expect(ids).not.toContain("pause-location-capture")
    expect(ids).not.toContain("activate-location-capture")
  })

  it("header Create requires Locations selection; create navigates and signals Detail drawer", async () => {
    const {
      pageModule,
      createDigitalGuestLink,
      syncSelectedLocation,
      navigateToCaptureLocation,
      onDigitalGuestLinkCreated,
    } = createModule({
      createDigitalGuestLink: async () => ({ ok: true, qrCodeId: 77 }),
    })

    await pageModule.syncWorkspace({
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })

    pageModule.openCreateDialog()
    expect(pageModule.getSnapshot().createDialog).toMatchObject({
      isOpen: true,
      locationBound: false,
      selectedLocationId: null,
    })

    expect(
      await pageModule.createDigitalGuestLink({
        linkName: "Instagram",
        internalDescription: null,
        channel: "SocialMedia",
        status: "Active",
      })
    ).toBe("noop")
    expect(createDigitalGuestLink).not.toHaveBeenCalled()

    pageModule.setCreateDialogLocationId(2)
    expect(pageModule.getSnapshot().createDialog.selectedLocationId).toBe(2)

    const result = await pageModule.createDigitalGuestLink({
      linkName: "Instagram",
      internalDescription: null,
      channel: "SocialMedia",
      status: "Active",
    })

    expect(result).toBe("created")
    expect(createDigitalGuestLink).toHaveBeenCalledWith(2, {
      linkName: "Instagram",
      internalDescription: null,
      channel: "SocialMedia",
      status: "Active",
      locationId: 2,
    })
    expect(onDigitalGuestLinkCreated).toHaveBeenCalledWith(
      "Digital guest link created"
    )
    expect(pageModule.getSnapshot().createDialog.isOpen).toBe(false)
    expect(syncSelectedLocation).toHaveBeenCalledWith(2)
    expect(navigateToCaptureLocation).toHaveBeenCalledWith(2, {
      openPlacementDetailQrCodeId: 77,
    })
  })

  it("row Create opens pre-bound dialog and creates without Locations picker", async () => {
    const { pageModule, createDigitalGuestLink, navigateToCaptureLocation } =
      createModule({
        createDigitalGuestLink: async () => ({ ok: true, qrCodeId: 55 }),
      })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })

    pageModule.openCreateDialog({ locationId: 1 })
    expect(pageModule.getSnapshot().createDialog).toMatchObject({
      isOpen: true,
      locationBound: true,
      selectedLocationId: 1,
    })

    pageModule.setCreateDialogLocationId(99)
    expect(pageModule.getSnapshot().createDialog.selectedLocationId).toBe(1)

    const result = await pageModule.createDigitalGuestLink({
      linkName: "Email blast",
      internalDescription: "ops",
      channel: "Email",
      status: "Paused",
    })

    expect(result).toBe("created")
    expect(createDigitalGuestLink).toHaveBeenCalledWith(1, expect.any(Object))
    expect(navigateToCaptureLocation).toHaveBeenCalledWith(1, {
      openPlacementDetailQrCodeId: 55,
    })
  })

  it("disables header Create when there are zero Owned locations", async () => {
    const { pageModule } = createModule()

    await pageModule.syncWorkspace({ locations: [] })

    expect(pageModule.getSnapshot().canCreateDigitalGuestLink).toBe(false)
    pageModule.openCreateDialog()
    expect(pageModule.getSnapshot().createDialog.isOpen).toBe(false)
  })

  it("Preview uses preview-options, skips picker for one code, and clears cache on close", async () => {
    const { pageModule, getCapturePreviewOptions } = createModule({
      previewOptions: async (locationId) => {
        if (locationId === 1) {
          return emptyPreviewOptionsResponse({
            items: [
              {
                qrCodeId: 10,
                qrType: "CounterCard",
                status: "Active",
              },
            ],
          })
        }
        return emptyPreviewOptionsResponse({
          items: [
            {
              qrCodeId: 20,
              qrType: "CounterCard",
              status: "Active",
            },
            {
              qrCodeId: 21,
              qrType: "DigitalGuestLink",
              status: "Paused",
              linkName: "WhatsApp",
            },
          ],
        })
      },
      locations: emptyLocationsResponse({
        items: [
          {
            locationId: 1,
            locationName: "Camden",
            status: "Active",
            activePlacementsCount: 1,
            pauseRestoreQrCodeCount: 0,
            qrScans: 1,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastActivityAt: null,
          },
          {
            locationId: 2,
            locationName: "Soho",
            status: "Active",
            activePlacementsCount: 2,
            pauseRestoreQrCodeCount: 0,
            qrScans: 1,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastActivityAt: null,
          },
        ],
        totalCount: 2,
      }),
    })

    await pageModule.syncWorkspace({
      locations: [
        { id: 1, locationName: "Camden", address: "1 High St" },
        { id: 2, locationName: "Soho", address: "2 High St" },
      ],
    })

    expect(await pageModule.openLocationPreview(1)).toBe("opened")
    expect(getCapturePreviewOptions).toHaveBeenCalledWith(1)
    expect(getCapturePreviewOptions).toHaveBeenCalledTimes(1)
    expect(pageModule.getSnapshot().guestExperiencePreview).toMatchObject({
      isOpen: true,
      placementLabel: "Counter card",
      locationName: "Camden",
    })
    expect(pageModule.getSnapshot().guestExperiencePreviewPicker.isOpen).toBe(
      false
    )

    pageModule.closeGuestExperiencePreview()
    expect(pageModule.getSnapshot().guestExperiencePreview.isOpen).toBe(false)

    // After clear-on-close, Preview enablement falls back to activePlacementsCount.
    expect(
      pageModule
        .getLocationRowActions(1)
        .find((action) => action.id === "preview-guest-experience")?.enabled
    ).toBe(true)

    expect(await pageModule.openLocationPreview(1)).toBe("opened")
    expect(getCapturePreviewOptions).toHaveBeenCalledTimes(2)

    pageModule.closeGuestExperiencePreview()
    expect(await pageModule.openLocationPreview(2)).toBe("picker")
    expect(getCapturePreviewOptions).toHaveBeenCalledWith(2)
    expect(pageModule.getSnapshot().guestExperiencePreviewPicker.isOpen).toBe(
      true
    )
    expect(pageModule.getSnapshot().guestExperiencePreview.isOpen).toBe(false)

    pageModule.selectGuestExperiencePreviewPickerOption(21)
    expect(pageModule.confirmGuestExperiencePreviewPicker()).toBe("opened")
    expect(pageModule.getSnapshot().guestExperiencePreview).toMatchObject({
      isOpen: true,
      placementLabel: "WhatsApp",
      locationName: "Soho",
    })
  })

  it("Preview no-ops on load failure without opening picker or overlay", async () => {
    const { pageModule, getCapturePreviewOptions } = createModule({
      failPreviewOptions: true,
      locations: emptyLocationsResponse({
        items: [
          {
            locationId: 1,
            locationName: "Camden",
            status: "Active",
            activePlacementsCount: 2,
            pauseRestoreQrCodeCount: 0,
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastActivityAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })

    expect(await pageModule.openLocationPreview(1)).toBe("noop")
    expect(getCapturePreviewOptions).toHaveBeenCalledWith(1)
    expect(pageModule.getSnapshot().guestExperiencePreview.isOpen).toBe(false)
    expect(pageModule.getSnapshot().guestExperiencePreviewPicker.isOpen).toBe(
      false
    )
  })

  it("empty Preview options no-ops without caching so enablement stays on activePlacementsCount", async () => {
    const { pageModule } = createModule({
      previewOptions: emptyPreviewOptionsResponse({ items: [] }),
      locations: emptyLocationsResponse({
        items: [
          {
            locationId: 1,
            locationName: "Camden",
            status: "Active",
            activePlacementsCount: 2,
            pauseRestoreQrCodeCount: 0,
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastActivityAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })

    expect(await pageModule.openLocationPreview(1)).toBe("noop")
    expect(
      pageModule
        .getLocationRowActions(1)
        .find((action) => action.id === "preview-guest-experience")?.enabled
    ).toBe(true)
  })

  it("closing the Preview picker clears cache so the next open refetches", async () => {
    const { pageModule, getCapturePreviewOptions } = createModule({
      previewOptions: emptyPreviewOptionsResponse({
        items: [
          {
            qrCodeId: 20,
            qrType: "CounterCard",
            status: "Active",
          },
          {
            qrCodeId: 21,
            qrType: "WindowSticker",
            status: "Active",
          },
        ],
      }),
      locations: emptyLocationsResponse({
        items: [
          {
            locationId: 1,
            locationName: "Camden",
            status: "Active",
            activePlacementsCount: 2,
            pauseRestoreQrCodeCount: 0,
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastActivityAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })

    expect(await pageModule.openLocationPreview(1)).toBe("picker")
    expect(getCapturePreviewOptions).toHaveBeenCalledTimes(1)

    pageModule.closeGuestExperiencePreviewPicker()
    expect(pageModule.getSnapshot().guestExperiencePreviewPicker.isOpen).toBe(
      false
    )

    expect(await pageModule.openLocationPreview(1)).toBe("picker")
    expect(getCapturePreviewOptions).toHaveBeenCalledTimes(2)
  })

  it("confirms Pause location capture, toasts, and refreshes list without navigating", async () => {
    let locationsCall = 0
    const { pageModule, pauseLocationCapture, navigateToCaptureLocation } =
      createModule({
        locations: async () => {
          locationsCall += 1
          if (locationsCall === 1) {
            return emptyLocationsResponse({
              items: [
                {
                  locationId: 1,
                  locationName: "Camden",
                  status: "Active",
                  activePlacementsCount: 3,
                  pauseRestoreQrCodeCount: 0,
                  qrScans: 0,
                  feedbackSubmitted: 0,
                  marketingOptIns: 0,
                  offerClaims: 0,
                  lastActivityAt: null,
                },
              ],
            })
          }
          return emptyLocationsResponse({
            items: [
              {
                locationId: 1,
                locationName: "Camden",
                status: "Paused",
                activePlacementsCount: 0,
                pauseRestoreQrCodeCount: 3,
                qrScans: 0,
                feedbackSubmitted: 0,
                marketingOptIns: 0,
                offerClaims: 0,
                lastActivityAt: null,
              },
            ],
          })
        },
      })

    await pageModule.syncWorkspace({
      locations: [{ id: 1, locationName: "Camden" }],
    })

    expect(pageModule.requestPauseLocationCapture(1)).toBe("opened")
    expect(pageModule.getSnapshot().locationCaptureConfirm).toMatchObject({
      isOpen: true,
      details: {
        action: "pause",
        locationName: "Camden",
        codesCount: 3,
        successToastMessage: "Camden capture is now paused.",
      },
    })

    await expect(pageModule.confirmLocationCapture()).resolves.toEqual({
      outcome: "paused",
      toastMessage: "Camden capture is now paused.",
    })
    expect(pauseLocationCapture).toHaveBeenCalledWith(1)
    expect(navigateToCaptureLocation).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().locationCaptureConfirm.isOpen).toBe(false)
    expect(
      pageModule.getSnapshot().viewModel?.locationPerformance.rows[0]?.status
    ).toBe("Paused")
  })

  it("confirms Activate location capture using restore-set count", async () => {
    const { pageModule, activateLocationCapture } = createModule({
      locations: emptyLocationsResponse({
        items: [
          {
            locationId: 2,
            locationName: "Soho",
            status: "Paused",
            activePlacementsCount: 0,
            pauseRestoreQrCodeCount: 2,
            qrScans: 0,
            feedbackSubmitted: 0,
            marketingOptIns: 0,
            offerClaims: 0,
            lastActivityAt: null,
          },
        ],
      }),
    })

    await pageModule.syncWorkspace({
      locations: [{ id: 2, locationName: "Soho" }],
    })

    expect(pageModule.requestActivateLocationCapture(2)).toBe("opened")
    expect(pageModule.getSnapshot().locationCaptureConfirm.details).toMatchObject(
      {
        action: "activate",
        codesCount: 2,
        successToastMessage: "Soho capture is now active.",
      }
    )

    await expect(pageModule.confirmLocationCapture()).resolves.toEqual({
      outcome: "activated",
      toastMessage: "Soho capture is now active.",
    })
    expect(activateLocationCapture).toHaveBeenCalledWith(2)
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
      pauseRestoreQrCodeCount: 0,
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
      pauseRestoreQrCodeCount: 0,
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
      qrScansText: "0 opens",
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

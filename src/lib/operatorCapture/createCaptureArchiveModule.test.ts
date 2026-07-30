import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"

import { createCaptureArchiveModule } from "./createCaptureArchiveModule"
import type { CaptureArchivedPlacementsResponse } from "@/types/dashboard"
import type { CaptureArchiveListQueryParams } from "@/lib/operatorCapture/captureArchiveListQueryParams"

function emptyPaged(
  overrides?: Partial<CaptureArchivedPlacementsResponse>
): CaptureArchivedPlacementsResponse {
  return {
    success: true,
    placements: [],
    totalCount: 0,
    page: 1,
    pageSize: 25,
    archiverOptions: [],
    ...overrides,
  }
}

function createArchiveModule(options?: {
  archived?: CaptureArchivedPlacementsResponse
  getArchivedCapturePlacements?: (
    params: CaptureArchiveListQueryParams
  ) => Promise<CaptureArchivedPlacementsResponse>
  restoreCapturePlacement?: (
    locationId: number,
    qrCodeId: number
  ) => Promise<
    | { ok: true; qrCodeId: number; status: "Paused"; qrLinkUrl: string }
    | { ok: false; reason: "conflict" | "failed"; message: string }
  >
  onArchiveLoadError?: (message: string) => void
  onPlacementActionError?: (message: string) => void
  nowMs?: number
  debounceMs?: number
}) {
  const getArchivedCapturePlacements = vi.fn(
    async (params: CaptureArchiveListQueryParams) => {
      if (options?.getArchivedCapturePlacements) {
        return options.getArchivedCapturePlacements(params)
      }
      return (
        options?.archived ?? emptyPaged()
      )
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

  const archive = createCaptureArchiveModule({
    getArchivedCapturePlacements,
    restoreCapturePlacement,
    onArchiveLoadError: options?.onArchiveLoadError,
    onPlacementActionError: options?.onPlacementActionError,
    nowMs: () => options?.nowMs ?? Date.parse("2026-07-16T12:00:00.000Z"),
    debounceMs: options?.debounceMs ?? 0,
  })

  return {
    archive,
    getArchivedCapturePlacements,
    restoreCapturePlacement,
  }
}

describe("createCaptureArchiveModule", () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it("starts idle with no archive view", () => {
    const { archive } = createArchiveModule()
    expect(archive.getSnapshot()).toEqual({
      loadStatus: "idle",
      restoreConfirm: {
        isOpen: false,
        details: null,
      },
      archive: null,
    })
  })

  it("loads page 1 with location preselect, archiverOptions, restore, and digital duplicate", async () => {
    const { archive, restoreCapturePlacement, getArchivedCapturePlacements } =
      createArchiveModule({
        archived: emptyPaged({
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
          archiverOptions: ["Ada", "Mohamed Mahmoud"],
        }),
      })

    await archive.enter({
      returnPath: "/multi-dashboard/capture/locations/42",
      preselectedLocationId: 42,
      showLocationFilter: true,
      locations: [
        { id: 42, locationName: "Camden" },
        { id: 99, locationName: "Shoreditch" },
      ],
    })

    expect(getArchivedCapturePlacements).toHaveBeenCalledOnce()
    expect(getArchivedCapturePlacements).toHaveBeenCalledWith(
      expect.objectContaining({
        locationIds: [42],
        page: 1,
        pageSize: 25,
        sort: "recently-archived",
      })
    )
    const view = archive.getSnapshot().archive
    expect(view?.returnPath).toBe("/multi-dashboard/capture/locations/42")
    expect(view?.filters.locationIds).toEqual([42])
    expect(view?.activeFilterCount).toBe(1)
    expect(view?.archiverOptions).toEqual(["Ada", "Mohamed Mahmoud"])
    expect(view?.totalCount).toBe(1)
    expect(view?.pageRangeLabel).toBe(
      "Showing 1–1 of 1 archived placements"
    )
    expect(view?.rows.map((r) => r.qrCodeId)).toEqual([7])
    expect(view?.rows[0]).toMatchObject({
      placementLabel: "Summer promo",
      canDuplicateAsNew: true,
      canRestore: true,
    })

    expect(archive.requestRestore(8)).toBe("noop")
    expect(archive.requestRestore(7)).toBe("opened")
    expect(archive.getSnapshot().restoreConfirm.details).toMatchObject({
      title: "Restore digital guest link?",
      primaryLabel: "Restore link",
    })

    const restored = await archive.confirmRestore()
    expect(restored).toMatchObject({
      outcome: "restored",
      restoredFact: {
        qrCodeId: 7,
        status: "Paused",
        qrType: "DigitalGuestLink",
        linkName: "Summer promo",
      },
      locationId: 42,
    })
    expect(restoreCapturePlacement).toHaveBeenCalledWith(42, 7)
    expect(archive.getSnapshot().archive?.rows).toEqual([])
    expect(archive.getSnapshot().archive?.totalCount).toBe(0)
  })

  it("debounces search and refetches with q + page reset", async () => {
    const getArchivedCapturePlacements = vi.fn(
      async (params: CaptureArchiveListQueryParams) =>
        emptyPaged({
          placements: params.q
            ? [
                {
                  qrCodeId: 7,
                  locationId: 42,
                  locationName: "Camden",
                  qrType: "CounterCard",
                  status: "Archived",
                  linkName: null,
                  channel: null,
                  internalDescription: null,
                  qrLinkUrl: "https://example.test/scan/counter",
                  archivedAt: "2026-07-24T10:00:00.000Z",
                  archivedByDisplayName: "Mohamed",
                  qrScans: 1,
                  feedbackSubmitted: 0,
                  lastScanAt: null,
                  canRestore: true,
                },
              ]
            : [],
          totalCount: params.q ? 1 : 0,
          page: params.page,
        })
    )
    const { archive } = createArchiveModule({
      getArchivedCapturePlacements,
      debounceMs: 300,
    })

    await archive.enter({
      returnPath: "/single-dashboard/capture",
      showLocationFilter: false,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    getArchivedCapturePlacements.mockClear()

    archive.setSearchQuery("counter")
    expect(archive.getSnapshot().archive?.searchQuery).toBe("counter")
    expect(getArchivedCapturePlacements).not.toHaveBeenCalled()

    await vi.advanceTimersByTimeAsync(299)
    expect(getArchivedCapturePlacements).not.toHaveBeenCalled()

    await vi.advanceTimersByTimeAsync(1)
    expect(getArchivedCapturePlacements).toHaveBeenCalledOnce()
    expect(getArchivedCapturePlacements).toHaveBeenCalledWith(
      expect.objectContaining({ q: "counter", page: 1 })
    )
    expect(archive.getSnapshot().archive?.rows).toHaveLength(1)
  })

  it("resets to page 1 and refetches when filters or sort change", async () => {
    const getArchivedCapturePlacements = vi.fn(
      async (params: CaptureArchiveListQueryParams) =>
        emptyPaged({
          totalCount: 40,
          page: params.page,
          placements: [
            {
              qrCodeId: params.page,
              locationId: 42,
              locationName: "Camden",
              qrType: "CounterCard",
              status: "Archived",
              linkName: null,
              channel: null,
              internalDescription: null,
              qrLinkUrl: `https://example.test/scan/${params.page}`,
              archivedAt: "2026-07-24T10:00:00.000Z",
              archivedByDisplayName: "Mohamed",
              qrScans: 1,
              feedbackSubmitted: 0,
              lastScanAt: null,
              canRestore: true,
            },
          ],
        })
    )
    const { archive } = createArchiveModule({ getArchivedCapturePlacements })

    await archive.enter({
      returnPath: "/single-dashboard/capture",
      showLocationFilter: false,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    archive.goToNextPage()
    await vi.advanceTimersByTimeAsync(0)
    expect(getArchivedCapturePlacements).toHaveBeenLastCalledWith(
      expect.objectContaining({ page: 2 })
    )
    expect(archive.getSnapshot().archive?.page).toBe(2)

    archive.setSort("highest-qr-scans")
    await vi.advanceTimersByTimeAsync(0)
    expect(getArchivedCapturePlacements).toHaveBeenLastCalledWith(
      expect.objectContaining({
        sort: "highest-qr-scans",
        page: 1,
      })
    )
    expect(archive.getSnapshot().archive?.page).toBe(1)

    archive.goToNextPage()
    await vi.advanceTimersByTimeAsync(0)

    archive.setFilters({
      locationIds: [],
      placementTypes: ["SmartGuest"],
      archivedDate: { preset: "any-time" },
      archivedByDisplayNames: [],
    })
    await vi.advanceTimersByTimeAsync(0)
    expect(getArchivedCapturePlacements).toHaveBeenLastCalledWith(
      expect.objectContaining({
        qrTypes: ["SmartGuest"],
        page: 1,
      })
    )
  })

  it("pages next/previous within bounds and ignores out-of-range next", async () => {
    const getArchivedCapturePlacements = vi.fn(
      async (params: CaptureArchiveListQueryParams) =>
        emptyPaged({
          totalCount: 26,
          page: params.page,
          placements: [
            {
              qrCodeId: params.page,
              locationId: 42,
              locationName: "Camden",
              qrType: "CounterCard",
              status: "Archived",
              linkName: null,
              channel: null,
              internalDescription: null,
              qrLinkUrl: `https://example.test/scan/${params.page}`,
              archivedAt: "2026-07-24T10:00:00.000Z",
              archivedByDisplayName: "Mohamed",
              qrScans: 1,
              feedbackSubmitted: 0,
              lastScanAt: null,
              canRestore: true,
            },
          ],
        })
    )
    const { archive } = createArchiveModule({ getArchivedCapturePlacements })

    await archive.enter({
      returnPath: "/single-dashboard/capture",
      showLocationFilter: false,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const callsAfterEnter = getArchivedCapturePlacements.mock.calls.length
    archive.goToPreviousPage()
    await vi.advanceTimersByTimeAsync(0)
    expect(getArchivedCapturePlacements.mock.calls.length).toBe(callsAfterEnter)

    archive.goToNextPage()
    await vi.advanceTimersByTimeAsync(0)
    expect(archive.getSnapshot().archive?.page).toBe(2)
    expect(archive.getSnapshot().archive?.canGoNext).toBe(false)

    const callsOnPage2 = getArchivedCapturePlacements.mock.calls.length
    archive.goToNextPage()
    await vi.advanceTimersByTimeAsync(0)
    expect(getArchivedCapturePlacements.mock.calls.length).toBe(callsOnPage2)

    archive.goToPreviousPage()
    await vi.advanceTimersByTimeAsync(0)
    expect(archive.getSnapshot().archive?.page).toBe(1)
  })

  it("ignores stale list responses after a newer generation starts", async () => {
    let resolveFirst!: (value: CaptureArchivedPlacementsResponse) => void
    let resolveSecond!: (value: CaptureArchivedPlacementsResponse) => void
    const getArchivedCapturePlacements = vi.fn(
      async (params: CaptureArchiveListQueryParams) => {
        if (params.sort === "recently-archived") {
          return new Promise<CaptureArchivedPlacementsResponse>((resolve) => {
            resolveFirst = resolve
          })
        }
        return new Promise<CaptureArchivedPlacementsResponse>((resolve) => {
          resolveSecond = resolve
        })
      }
    )
    const { archive } = createArchiveModule({ getArchivedCapturePlacements })

    const enterPromise = archive.enter({
      returnPath: "/single-dashboard/capture",
      showLocationFilter: false,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    archive.setSort("placement-name-az")
    await vi.advanceTimersByTimeAsync(0)

    resolveSecond(
      emptyPaged({
        placements: [
          {
            qrCodeId: 99,
            locationId: 42,
            locationName: "Camden",
            qrType: "SmartGuest",
            status: "Archived",
            linkName: null,
            channel: null,
            internalDescription: null,
            qrLinkUrl: "https://example.test/scan/smart",
            archivedAt: "2026-07-24T10:00:00.000Z",
            archivedByDisplayName: "Mohamed",
            qrScans: 1,
            feedbackSubmitted: 0,
            lastScanAt: null,
            canRestore: true,
          },
        ],
        totalCount: 1,
        page: 1,
      })
    )
    await vi.advanceTimersByTimeAsync(0)

    resolveFirst(
      emptyPaged({
        placements: [
          {
            qrCodeId: 1,
            locationId: 42,
            locationName: "Camden",
            qrType: "CounterCard",
            status: "Archived",
            linkName: null,
            channel: null,
            internalDescription: null,
            qrLinkUrl: "https://example.test/scan/counter",
            archivedAt: "2026-07-24T10:00:00.000Z",
            archivedByDisplayName: "Mohamed",
            qrScans: 1,
            feedbackSubmitted: 0,
            lastScanAt: null,
            canRestore: true,
          },
        ],
        totalCount: 1,
        page: 1,
      })
    )
    await enterPromise

    expect(archive.getSnapshot().archive?.sort).toBe("placement-name-az")
    expect(archive.getSnapshot().archive?.rows.map((r) => r.qrCodeId)).toEqual([
      99,
    ])
  })

  it("prefills Duplicate as new for digital archived links only", async () => {
    const { archive } = createArchiveModule({
      archived: emptyPaged({
        placements: [
          {
            qrCodeId: 7,
            locationId: 42,
            locationName: "Camden",
            qrType: "DigitalGuestLink",
            status: "Archived",
            linkName: "Summer promo",
            channel: "Email",
            internalDescription: null,
            qrLinkUrl: "https://example.test/scan/summer",
            archivedAt: "2026-07-24T10:00:00.000Z",
            archivedByDisplayName: "Mohamed",
            qrScans: 1,
            feedbackSubmitted: 0,
            lastScanAt: null,
            canRestore: true,
          },
          {
            qrCodeId: 8,
            locationId: 42,
            locationName: "Camden",
            qrType: "WindowSticker",
            status: "Archived",
            linkName: null,
            channel: null,
            internalDescription: null,
            qrLinkUrl: "https://example.test/scan/window",
            archivedAt: "2026-07-20T10:00:00.000Z",
            archivedByDisplayName: "Mohamed",
            qrScans: 1,
            feedbackSubmitted: 0,
            lastScanAt: null,
            canRestore: true,
          },
        ],
        totalCount: 2,
      }),
    })

    await archive.enter({
      returnPath: "/single-dashboard/capture",
      showLocationFilter: false,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(archive.requestDuplicateAsNew(8)).toBe("noop")
    expect(archive.requestDuplicateAsNew(7)).toBe("opened")
    expect(archive.getSnapshot().archive?.createPrefill).toEqual({
      linkName: "Summer promo (copy)",
      channel: "Email",
      status: "Active",
      locationId: 42,
    })
  })

  it("rejects restore conflicts from the adapter", async () => {
    const onPlacementActionError = vi.fn()
    const { archive } = createArchiveModule({
      onPlacementActionError,
      archived: emptyPaged({
        placements: [
          {
            qrCodeId: 7,
            locationId: 42,
            locationName: "Camden",
            qrType: "CounterCard",
            status: "Archived",
            linkName: null,
            channel: null,
            internalDescription: null,
            qrLinkUrl: "https://example.test/scan/counter",
            archivedAt: "2026-07-24T10:00:00.000Z",
            archivedByDisplayName: "Mohamed",
            qrScans: 1,
            feedbackSubmitted: 0,
            lastScanAt: null,
            canRestore: true,
          },
        ],
        totalCount: 1,
      }),
      restoreCapturePlacement: async () => ({
        ok: false,
        reason: "conflict",
        message: "A QR code of this type already exists at this location.",
      }),
    })

    await archive.enter({
      returnPath: "/single-dashboard/capture",
      showLocationFilter: false,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    archive.requestRestore(7)
    const result = await archive.confirmRestore()
    expect(result).toBe("conflict")
    expect(onPlacementActionError).toHaveBeenCalled()
    expect(archive.getSnapshot().archive?.rows).toHaveLength(1)
  })

  it("notifies archive subscribers on search without requiring a parent relay", async () => {
    const { archive } = createArchiveModule({
      archived: emptyPaged({
        placements: [
          {
            qrCodeId: 7,
            locationId: 42,
            locationName: "Camden",
            qrType: "CounterCard",
            status: "Archived",
            linkName: null,
            channel: null,
            internalDescription: null,
            qrLinkUrl: "https://example.test/scan/counter",
            archivedAt: "2026-07-24T10:00:00.000Z",
            archivedByDisplayName: "Mohamed",
            qrScans: 1,
            feedbackSubmitted: 0,
            lastScanAt: null,
            canRestore: true,
          },
        ],
        totalCount: 1,
      }),
    })

    await archive.enter({
      returnPath: "/single-dashboard/capture",
      showLocationFilter: false,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const listener = vi.fn()
    archive.subscribe(listener)
    archive.setSearchQuery("counter")
    expect(listener).toHaveBeenCalledOnce()
    expect(archive.getSnapshot().archive?.searchQuery).toBe("counter")
  })

  it("resolves getArchivedPlacement from the current page only", async () => {
    const { archive } = createArchiveModule({
      archived: emptyPaged({
        placements: [
          {
            qrCodeId: 7,
            locationId: 42,
            locationName: "Camden",
            qrType: "CounterCard",
            status: "Archived",
            linkName: null,
            channel: null,
            internalDescription: null,
            qrLinkUrl: "https://example.test/scan/counter",
            archivedAt: "2026-07-24T10:00:00.000Z",
            archivedByDisplayName: "Mohamed",
            qrScans: 1,
            feedbackSubmitted: 0,
            lastScanAt: null,
            canRestore: true,
          },
        ],
        totalCount: 1,
      }),
    })

    await archive.enter({
      returnPath: "/single-dashboard/capture",
      showLocationFilter: false,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(archive.getArchivedPlacement(7)?.qrCodeId).toBe(7)
    expect(archive.getArchivedPlacement(999)).toBeNull()
  })

  it("clamps page and refetches when Restore empties a non-first page", async () => {
    const getArchivedCapturePlacements = vi.fn(
      async (params: CaptureArchiveListQueryParams) => {
        if (params.page === 2) {
          return emptyPaged({
            totalCount: 26,
            page: 2,
            placements: [
              {
                qrCodeId: 26,
                locationId: 42,
                locationName: "Camden",
                qrType: "CounterCard",
                status: "Archived",
                linkName: null,
                channel: null,
                internalDescription: null,
                qrLinkUrl: "https://example.test/scan/26",
                archivedAt: "2026-07-24T10:00:00.000Z",
                archivedByDisplayName: "Mohamed",
                qrScans: 1,
                feedbackSubmitted: 0,
                lastScanAt: null,
                canRestore: true,
              },
            ],
          })
        }
        return emptyPaged({
          totalCount: 26,
          page: 1,
          placements: [
            {
              qrCodeId: 1,
              locationId: 42,
              locationName: "Camden",
              qrType: "SmartGuest",
              status: "Archived",
              linkName: null,
              channel: null,
              internalDescription: null,
              qrLinkUrl: "https://example.test/scan/1",
              archivedAt: "2026-07-24T10:00:00.000Z",
              archivedByDisplayName: "Mohamed",
              qrScans: 1,
              feedbackSubmitted: 0,
              lastScanAt: null,
              canRestore: true,
            },
          ],
        })
      }
    )
    const { archive } = createArchiveModule({ getArchivedCapturePlacements })

    await archive.enter({
      returnPath: "/single-dashboard/capture",
      showLocationFilter: false,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    archive.goToNextPage()
    await vi.advanceTimersByTimeAsync(0)
    expect(archive.getSnapshot().archive?.page).toBe(2)

    // After Restore, subsequent page-1 fetches reflect the decremented total.
    getArchivedCapturePlacements.mockImplementation(
      async (params: CaptureArchiveListQueryParams) =>
        emptyPaged({
          totalCount: 25,
          page: params.page,
          placements: [
            {
              qrCodeId: 1,
              locationId: 42,
              locationName: "Camden",
              qrType: "SmartGuest",
              status: "Archived",
              linkName: null,
              channel: null,
              internalDescription: null,
              qrLinkUrl: "https://example.test/scan/1",
              archivedAt: "2026-07-24T10:00:00.000Z",
              archivedByDisplayName: "Mohamed",
              qrScans: 1,
              feedbackSubmitted: 0,
              lastScanAt: null,
              canRestore: true,
            },
          ],
        })
    )

    archive.requestRestore(26)
    await archive.confirmRestore()
    await vi.advanceTimersByTimeAsync(0)

    expect(archive.getSnapshot().archive?.page).toBe(1)
    expect(archive.getSnapshot().archive?.totalCount).toBe(25)
    expect(archive.getSnapshot().archive?.rows.map((r) => r.qrCodeId)).toEqual([
      1,
    ])
  })
})

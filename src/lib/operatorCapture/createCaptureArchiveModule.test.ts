import { describe, expect, it, vi } from "vitest"

import { createCaptureArchiveModule } from "./createCaptureArchiveModule"
import type { CaptureArchivedPlacementsResponse } from "@/types/dashboard"

function createArchiveModule(options?: {
  archived?: CaptureArchivedPlacementsResponse
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
}) {
  const getArchivedCapturePlacements = vi.fn(async () => {
    return (
      options?.archived ?? {
        success: true as const,
        placements: [],
      }
    )
  })
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
  })

  return {
    archive,
    getArchivedCapturePlacements,
    restoreCapturePlacement,
  }
}

describe("createCaptureArchiveModule", () => {
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

  it("loads Archive with location preselect, filters, restore, and digital duplicate prefill", async () => {
    const { archive, restoreCapturePlacement, getArchivedCapturePlacements } =
      createArchiveModule({
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
            {
              qrCodeId: 8,
              locationId: 99,
              locationName: "Shoreditch",
              qrType: "CounterCard",
              status: "Archived",
              linkName: null,
              channel: null,
              internalDescription: null,
              qrLinkUrl: "https://example.test/scan/counter",
              archivedAt: "2026-07-10T10:00:00.000Z",
              archivedByDisplayName: "Ada",
              qrScans: 2,
              feedbackSubmitted: 1,
              lastScanAt: null,
              canRestore: false,
            },
          ],
        },
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
    const view = archive.getSnapshot().archive
    expect(view?.returnPath).toBe("/multi-dashboard/capture/locations/42")
    expect(view?.filters.locationIds).toEqual([42])
    expect(view?.activeFilterCount).toBe(1)
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
  })

  it("prefills Duplicate as new for digital archived links only", async () => {
    const { archive } = createArchiveModule({
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
      },
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
      archived: {
        success: true,
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
      },
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
      archived: {
        success: true,
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
      },
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
})

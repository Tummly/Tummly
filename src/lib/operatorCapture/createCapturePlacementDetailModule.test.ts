import { describe, expect, it, vi } from "vitest"

import { createCapturePlacementDetailModule } from "./createCapturePlacementDetailModule"
import type { PlacementDetailFact } from "./createCapturePlacementDetailModule"

const NOW = Date.parse("2026-07-01T15:00:00.000Z")

function catalogFact(
  overrides?: Partial<PlacementDetailFact>
): PlacementDetailFact {
  return {
    qrCodeId: 9,
    qrType: "DeliveryInsert",
    status: "Active",
    qrLinkUrl: "https://example.test/scan/delivery",
    qrScans: 10,
    feedbackSubmitted: 4,
    marketingOptIns: 2,
    offerClaims: 0,
    lastScanAt: "2026-07-01T12:00:00.000Z",
    internalDescription: "Front counter",
    ...overrides,
  }
}

describe("createCapturePlacementDetailModule", () => {
  it("starts closed with an empty drawer snapshot", () => {
    const detail = createCapturePlacementDetailModule({ nowMs: () => NOW })
    expect(detail.getSnapshot()).toEqual({
      isOpen: false,
      selectedQrCodeId: null,
      details: null,
    })
    expect(detail.getOpenContext()).toEqual({
      isOpen: false,
      qrCodeId: null,
      fact: null,
      locationId: null,
      descriptionDraft: "",
    })
  })

  it("opens from a live fact and closes without notifying unrelated listeners incorrectly", () => {
    const detail = createCapturePlacementDetailModule({ nowMs: () => NOW })
    const listener = vi.fn()
    detail.subscribe(listener)

    expect(
      detail.openFromLive({
        fact: catalogFact(),
        locationName: "Camden",
        locationCapturePaused: false,
      })
    ).toBe("opened")

    expect(listener).toHaveBeenCalledTimes(1)
    const open = detail.getSnapshot()
    expect(open.isOpen).toBe(true)
    expect(open.selectedQrCodeId).toBe(9)
    expect(open.details).toMatchObject({
      kind: "catalog",
      title: "Delivery insert",
      status: "Active",
      locationName: "Camden",
      descriptionDraft: "Front counter",
      canRotate: true,
    })
    expect(detail.getOpenContext()).toMatchObject({
      isOpen: true,
      qrCodeId: 9,
      locationId: null,
      descriptionDraft: "Front counter",
    })

    detail.close()
    expect(listener).toHaveBeenCalledTimes(2)
    expect(detail.getSnapshot()).toEqual({
      isOpen: false,
      selectedQrCodeId: null,
      details: null,
    })
  })

  it("opens from an Archive fact with location id for page-module actions", () => {
    const detail = createCapturePlacementDetailModule({ nowMs: () => NOW })
    const fact = catalogFact({
      status: "Archived",
      qrType: "DigitalGuestLink",
      linkName: "Summer promo",
      channel: "SocialMedia",
    })

    expect(
      detail.openFromArchive({
        fact,
        locationId: 42,
        locationName: "Camden",
      })
    ).toBe("opened")

    expect(detail.getSnapshot().details).toMatchObject({
      kind: "digital",
      title: "Summer promo",
      status: "Archived",
      locationName: "Camden",
      canArchive: false,
    })
    expect(detail.getOpenContext().locationId).toBe(42)
  })

  it("publishes description draft changes only to Detail subscribers", () => {
    const detail = createCapturePlacementDetailModule({ nowMs: () => NOW })
    detail.openFromLive({
      fact: catalogFact({ internalDescription: "" }),
      locationName: "Camden",
      locationCapturePaused: false,
    })

    const listener = vi.fn()
    detail.subscribe(listener)

    detail.setDescriptionDraft("Follow up note")
    expect(listener).toHaveBeenCalledTimes(1)
    expect(detail.getSnapshot().details?.descriptionDraft).toBe("Follow up note")
    expect(detail.getOpenContext().descriptionDraft).toBe("Follow up note")
  })

  it("ignores draft updates when the drawer is closed", () => {
    const detail = createCapturePlacementDetailModule({ nowMs: () => NOW })
    const listener = vi.fn()
    detail.subscribe(listener)

    detail.setDescriptionDraft("Ignored")
    expect(listener).not.toHaveBeenCalled()
    expect(detail.getOpenContext().descriptionDraft).toBe("")
  })

  it("patches the open fact after a page-module mutate without closing", () => {
    const detail = createCapturePlacementDetailModule({ nowMs: () => NOW })
    detail.openFromLive({
      fact: catalogFact(),
      locationName: "Camden",
      locationCapturePaused: false,
    })

    detail.patchFact({
      fact: catalogFact({
        status: "Paused",
        internalDescription: "Updated note",
        updatedAt: "2026-07-01T14:00:00.000Z",
        updatedByDisplayName: "Test Operator",
      }),
      locationName: "Camden",
      locationCapturePaused: false,
      descriptionDraft: "Updated note",
    })

    expect(detail.getSnapshot().details).toMatchObject({
      status: "Paused",
      descriptionDraft: "Updated note",
      pauseActivateLabel: "Activate placement",
    })
    expect(detail.getOpenContext().fact?.status).toBe("Paused")
  })

  it("reset closes drawer state for workspace sync", () => {
    const detail = createCapturePlacementDetailModule({ nowMs: () => NOW })
    detail.openFromLive({
      fact: catalogFact(),
      locationName: "Camden",
      locationCapturePaused: false,
    })
    detail.setDescriptionDraft("draft")

    detail.reset()
    expect(detail.getSnapshot()).toEqual({
      isOpen: false,
      selectedQrCodeId: null,
      details: null,
    })
    expect(detail.getOpenContext().descriptionDraft).toBe("")
  })
})

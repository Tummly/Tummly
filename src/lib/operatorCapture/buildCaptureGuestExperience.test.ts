import { describe, expect, it } from "vitest"

import {
  buildCaptureGuestExperience,
  CAPTURE_CONNECTED_OFFERS_STUB,
} from "./buildCaptureGuestExperience"

describe("buildCaptureGuestExperience", () => {
  it("derives Figma summary rows from Active/Paused placement counts", () => {
    const result = buildCaptureGuestExperience({
      locationName: "Camden",
      locationAddress: "12 High St",
      lastJourneyUpdate: {
        createdAt: "2026-07-14T13:00:00.000Z",
        guestName: "Jane Doe",
      },
      placements: [
        { qrCodeId: 1, qrType: "CounterCard", status: "Active" },
        { qrCodeId: 2, qrType: "PackagingSticker", status: "Paused" },
        { qrCodeId: 3, qrType: "SmartGuest", status: "Active" },
        { qrCodeId: 4, qrType: "WindowSticker", status: "Active" },
      ],
    })

    expect(result).toEqual({
      guestFormsText:
        "1 published form · Used by 3 of 3 active placements",
      qrPlacementsText: "3 of 4 placements active",
      connectedOffersText: CAPTURE_CONNECTED_OFFERS_STUB,
      needsAttentionText: "1 placements require action",
      lastJourneyUpdateText: "14 Jul 2026 by Jane Doe",
      previewEntry: { kind: "open-picker" },
      previewPlacementLabel: "Smart Guest",
      locationName: "Camden",
      locationAddress: "12 High St",
      thankYouOffer: { offerId: null, title: null, live: false },
    })
  })

  it("shows attached thank-you offer title in Connected offers", () => {
    const result = buildCaptureGuestExperience({
      locationName: "Camden",
      locationAddress: "12 High St",
      placements: [
        { qrCodeId: 1, qrType: "SmartGuest", status: "Active" },
      ],
      thankYouOffer: {
        offerId: 9,
        title: "Free dessert",
        live: true,
      },
    })

    expect(result.connectedOffersText).toBe("Free dessert")
    expect(result.thankYouOffer).toEqual({
      offerId: 9,
      title: "Free dessert",
      live: true,
    })
  })

  it("shows All active placements are ready when no codes are Paused", () => {
    const result = buildCaptureGuestExperience({
      locationName: "Soho",
      locationAddress: "",
      lastJourneyUpdate: null,
      placements: [
        { qrCodeId: 1, qrType: "SmartGuest", status: "Active" },
      ],
    })

    expect(result.needsAttentionText).toBe("All active placements are ready")
    expect(result.lastJourneyUpdateText).toBe("—")
    expect(result.previewEntry).toEqual({
      kind: "open-preview",
      qrCodeId: 1,
      placementLabel: "Smart Guest",
    })
    expect(result.previewPlacementLabel).toBe("Smart Guest")
  })

  it("disables Preview when there are zero Active/Paused codes", () => {
    const result = buildCaptureGuestExperience({
      locationName: "Soho",
      locationAddress: "",
      lastJourneyUpdate: null,
      placements: [],
    })

    expect(result.guestFormsText).toBe(
      "1 published form · Used by 0 of 0 active placements"
    )
    expect(result.qrPlacementsText).toBe("0 of 0 placements active")
    expect(result.previewEntry).toEqual({ kind: "disabled" })
  })

  it("returns em dashes for placement-derived rows when facts are unavailable", () => {
    const result = buildCaptureGuestExperience({
      locationName: "Soho",
      locationAddress: "",
      lastJourneyUpdate: undefined,
      placements: null,
    })

    expect(result.guestFormsText).toBe("—")
    expect(result.qrPlacementsText).toBe("—")
    expect(result.needsAttentionText).toBe("—")
    expect(result.lastJourneyUpdateText).toBe("—")
    expect(result.connectedOffersText).toBe("No active offers")
    expect(result.previewEntry).toEqual({ kind: "disabled" })
  })
})

import { describe, expect, it } from "vitest"

import {
  GUEST_PREVIEW_EMPTY_VALUE,
  GUEST_PREVIEW_OFFER_COPY_LABEL,
  GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
} from "@/lib/operatorFeedback/guestPreviewPresentation"

import {
  buildCaptureThankYouPreviewCoupon,
  CAPTURE_CONNECTED_OFFERS_NONE,
  formatCaptureConnectedOffersText,
} from "./captureThankYouOfferPresentation"

describe("formatCaptureConnectedOffersText", () => {
  it("returns the live attached title", () => {
    expect(
      formatCaptureConnectedOffersText({
        offerId: 9,
        title: "Free dessert",
        live: true,
      })
    ).toBe("Free dessert")
  })

  it("returns No active offers when attach is missing or not live", () => {
    expect(formatCaptureConnectedOffersText(null)).toBe(
      CAPTURE_CONNECTED_OFFERS_NONE
    )
    expect(
      formatCaptureConnectedOffersText({
        offerId: 9,
        title: "Paused dessert",
        live: false,
      })
    ).toBe(CAPTURE_CONNECTED_OFFERS_NONE)
  })
})

describe("buildCaptureThankYouPreviewCoupon", () => {
  it("builds a sample coupon when thank-you attach is live with a title", () => {
    expect(
      buildCaptureThankYouPreviewCoupon({
        offerId: 9,
        title: "Free dessert",
        live: true,
      })
    ).toEqual({
      title: "Free dessert",
      description: "",
      redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
      expiryLabel: `Expires: ${GUEST_PREVIEW_EMPTY_VALUE}`,
      copyLabel: GUEST_PREVIEW_OFFER_COPY_LABEL,
      copyEnabled: false,
    })
  })

  it("returns null when thank-you attach is missing, not live, or untitled", () => {
    expect(buildCaptureThankYouPreviewCoupon(null)).toBeNull()
    expect(
      buildCaptureThankYouPreviewCoupon({
        offerId: 9,
        title: "Paused dessert",
        live: false,
      })
    ).toBeNull()
    expect(
      buildCaptureThankYouPreviewCoupon({
        offerId: 9,
        title: "  ",
        live: true,
      })
    ).toBeNull()
  })
})

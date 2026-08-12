import { describe, expect, it } from "vitest"

import { recoveryOfferIssuedActivityLabel } from "./feedbackDetailsActivityPresentation"

describe("feedbackDetailsActivityPresentation", () => {
  it("labels Offer-issue activity with title, Claim code, and status", () => {
    expect(
      recoveryOfferIssuedActivityLabel({
        offerTitle: "10% off next visit",
        redemptionCode: "TUM-ABC123",
        redemptionStatus: "not_redeemed",
      })
    ).toBe(
      "Recovery offer issued · 10% off next visit · TUM-ABC123 · Not redeemed"
    )
  })

  it("shows Redeemed when the Offer issue is redeemed", () => {
    expect(
      recoveryOfferIssuedActivityLabel({
        offerTitle: "10% off next visit",
        redemptionCode: "TUM-ABC123",
        redemptionStatus: "redeemed",
      })
    ).toBe(
      "Recovery offer issued · 10% off next visit · TUM-ABC123 · Redeemed"
    )
  })

  it("omits status for historical one-offs without Issue redeem facts", () => {
    expect(
      recoveryOfferIssuedActivityLabel({
        offerTitle: "Legacy dessert",
        redemptionCode: "TUM-LEGACY",
        redemptionStatus: null,
      })
    ).toBe("Recovery offer issued · Legacy dessert · TUM-LEGACY")
  })

  it("falls back when title or Claim code is missing", () => {
    expect(
      recoveryOfferIssuedActivityLabel({
        offerTitle: null,
        redemptionCode: "TUM-ABC123",
        redemptionStatus: "not_redeemed",
      })
    ).toBe("Recovery offer issued · TUM-ABC123 · Not redeemed")

    expect(
      recoveryOfferIssuedActivityLabel({
        offerTitle: null,
        redemptionCode: null,
        redemptionStatus: null,
      })
    ).toBe("Recovery offer issued")
  })
})

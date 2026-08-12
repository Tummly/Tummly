import { describe, expect, it } from "vitest"

import { recoveryOfferIssuedActivityLabel } from "./feedbackDetailsActivityPresentation"

describe("feedbackDetailsActivityPresentation", () => {
  it("labels Offer-issue activity with title and Claim code", () => {
    expect(
      recoveryOfferIssuedActivityLabel({
        offerTitle: "10% off next visit",
        redemptionCode: "TUM-ABC123",
      })
    ).toBe("Recovery offer issued · 10% off next visit · TUM-ABC123")
  })

  it("falls back when title or Claim code is missing", () => {
    expect(
      recoveryOfferIssuedActivityLabel({
        offerTitle: null,
        redemptionCode: "TUM-ABC123",
      })
    ).toBe("Recovery offer issued · TUM-ABC123")

    expect(
      recoveryOfferIssuedActivityLabel({
        offerTitle: "Legacy dessert",
        redemptionCode: "  ",
      })
    ).toBe("Recovery offer issued · Legacy dessert")

    expect(
      recoveryOfferIssuedActivityLabel({
        offerTitle: null,
        redemptionCode: null,
      })
    ).toBe("Recovery offer issued")
  })
})

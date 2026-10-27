import { describe, expect, it } from "vitest"

import { SHOP_OFFER_CARD_SETUP_COPY } from "@/lib/operatorShop/shopOfferCardSetupPresentation"

describe("SHOP_OFFER_CARD_SETUP_COPY", () => {
  it("names Guest Cards in the offer-required guard toast", () => {
    expect(SHOP_OFFER_CARD_SETUP_COPY.guardToast).toBe(
      "Set up your card offer before ordering Guest Cards.",
    )
  })
})

import { describe, expect, it } from "vitest"

import {
  SHOP_OFFER_CARD_SETUP_COPY,
  shopOfferCardAutoTitle,
} from "@/lib/operatorShop/shopOfferCardSetupPresentation"

describe("SHOP_OFFER_CARD_SETUP_COPY", () => {
  it("names Guest Cards in the offer-required guard toast", () => {
    expect(SHOP_OFFER_CARD_SETUP_COPY.guardToast).toBe(
      "Set up your card offer before ordering Guest Cards.",
    )
  })

  it("uses the Shop default offer description", () => {
    expect(SHOP_OFFER_CARD_SETUP_COPY.defaultDescription).toBe(
      "Show this offer to a member of staff on your next visit to redeem it.",
    )
  })
})

describe("shopOfferCardAutoTitle", () => {
  const empty = {
    offerType: null,
    discountPercentage: "",
    discountAmount: "",
    freeItemText: "",
    replacementItemText: "",
  } as const

  it("builds the title for each Shop offer type", () => {
    expect(
      shopOfferCardAutoTitle({ ...empty, offerType: "percentage_discount", discountPercentage: "20" }),
    ).toBe("Get 20% off your next visit")
    expect(
      shopOfferCardAutoTitle({ ...empty, offerType: "fixed_discount", discountAmount: "5" }),
    ).toBe("Get £5 off your next order")
    expect(
      shopOfferCardAutoTitle({ ...empty, offerType: "free_item", freeItemText: "Dessert" }),
    ).toBe("Enjoy a free Dessert")
    expect(
      shopOfferCardAutoTitle({ ...empty, offerType: "replacement_item", replacementItemText: "Main course" }),
    ).toBe("Replacement Main course")
  })

  it("returns an empty title when the benefit value is missing or invalid", () => {
    expect(shopOfferCardAutoTitle({ ...empty, offerType: "percentage_discount", discountPercentage: "" })).toBe("")
    expect(shopOfferCardAutoTitle({ ...empty, offerType: "percentage_discount", discountPercentage: "0" })).toBe("")
    expect(shopOfferCardAutoTitle({ ...empty, offerType: "free_item", freeItemText: "  " })).toBe("")
    expect(shopOfferCardAutoTitle({ ...empty, offerType: null })).toBe("")
  })
})

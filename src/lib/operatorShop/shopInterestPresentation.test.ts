import { describe, expect, it } from "vitest"

import { SHOP_INTEREST_THANKS_MESSAGE } from "./shopInterestPresentation"

describe("shopInterestPresentation", () => {
  it("exposes thanks confirmation copy with no purchase language", () => {
    expect(SHOP_INTEREST_THANKS_MESSAGE).toBe(
      "Thanks — we have noted your interest."
    )
    expect(SHOP_INTEREST_THANKS_MESSAGE.toLowerCase()).not.toMatch(
      /cart|checkout|order|pay/
    )
  })
})

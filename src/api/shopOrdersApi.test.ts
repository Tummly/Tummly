import { describe, expect, it } from "vitest"

import { computeShopCheckoutTotalsPence } from "@/api/shopOrdersApi"

describe("computeShopCheckoutTotalsPence", () => {
  it("OFF (vatRateBps 0): expectedGross is net only", () => {
    const totals = computeShopCheckoutTotalsPence({
      materialsNetPence: 2400,
      deliveryMethod: "standard",
      vatRateBps: 0,
    })

    expect(totals.vatPence).toBe(0)
    expect(totals.grossPence).toBe(2400)
  })

  it("ACTIVE (vatRateBps 2000): gross matches server half-up math", () => {
    const materialsNetPence = 2400
    const totals = computeShopCheckoutTotalsPence({
      materialsNetPence,
      deliveryMethod: "standard",
      vatRateBps: 2000,
    })

    // Matches TummlyVatMath.VatPenceFromNetPence / GrossMinorFromNetPence.
    expect(totals.vatPence).toBe(480)
    expect(totals.grossPence).toBe(2880)
  })

  it("ACTIVE with express: VAT on materials only; delivery stays net", () => {
    const totals = computeShopCheckoutTotalsPence({
      materialsNetPence: 2400,
      deliveryMethod: "express",
      vatRateBps: 2000,
    })

    expect(totals.deliveryNetPence).toBe(2000)
    expect(totals.vatPence).toBe(480)
    expect(totals.grossPence).toBe(4880)
  })
})

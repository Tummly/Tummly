import { describe, expect, it } from "vitest"

import type { AdminCommercialCustomUnitRate } from "@/api/adminCommercialCatalogApi"
import {
  creditsFromPackCounts,
  estimateCustomPlanTotalPence,
  lineNetPence,
} from "@/lib/marketingCustomPlan/customPlanEstimate"

function rate(
  partial: Partial<AdminCommercialCustomUnitRate> & {
    rateKey: string
    monthlyNetPence: number
  }
): AdminCommercialCustomUnitRate {
  return {
    id: partial.id ?? "r1",
    rateKey: partial.rateKey,
    unitQuantity: partial.unitQuantity ?? 1,
    monthlyNetPence: partial.monthlyNetPence,
    annualNetPence: partial.annualNetPence ?? null,
  }
}

describe("lineNetPence", () => {
  it("uses monthly for monthly cadence", () => {
    expect(
      lineNetPence(rate({ rateKey: "location", monthlyNetPence: 3900 }), 2, "monthly")
    ).toBe(7800)
  })

  it("uses annual when set for annual cadence", () => {
    expect(
      lineNetPence(
        rate({
          rateKey: "location",
          monthlyNetPence: 3900,
          annualNetPence: 39800,
        }),
        2,
        "annual"
      )
    ).toBe(79600)
  })

  it("falls back to monthly × 12 × 0.85 when annual unset", () => {
    // monthly line for qty 1 = 300; annual fallback = round(300 * 12 * 0.85) = 3060
    expect(
      lineNetPence(
        rate({ rateKey: "team_user", monthlyNetPence: 300, annualNetPence: null }),
        1,
        "annual"
      )
    ).toBe(3060)
  })

  it("treats annual 0 as unset for fallback", () => {
    expect(
      lineNetPence(
        rate({ rateKey: "team_user", monthlyNetPence: 300, annualNetPence: 0 }),
        1,
        "annual"
      )
    ).toBe(3060)
  })
})

describe("creditsFromPackCounts", () => {
  it("sums pack steppers into channel credit totals", () => {
    expect(
      creditsFromPackCounts({
        email_20k: 1,
        email_50k: 2,
        sms_500: 1,
        sms_1000: 0,
        sms_5000: 1,
        ai_500: 2,
        ai_2000: 1,
      })
    ).toEqual({
      emailCredits: 120_000,
      smsCredits: 5_500,
      aiCredits: 3_000,
    })
  })
})

describe("estimateCustomPlanTotalPence", () => {
  const rates = [
    rate({ rateKey: "location", monthlyNetPence: 3900, annualNetPence: 39800 }),
    rate({ rateKey: "team_user", monthlyNetPence: 300, annualNetPence: null }),
  ]

  it("sums mixed annual and fallback rows for annual cadence", () => {
    const result = estimateCustomPlanTotalPence(
      rates,
      {
        locations: 1,
        teamUsers: 1,
        qrPlacements: 0,
        offers: 0,
        emailCredits: 0,
        smsCredits: 0,
        aiCredits: 0,
      },
      "annual"
    )
    // location annual 39800 + team fallback 3060
    expect(result.displayNetPence).toBe(42860)
    expect(result.monthlyNetPence).toBe(4200)
  })

  it("keeps monthly as sum of monthly lines", () => {
    const result = estimateCustomPlanTotalPence(
      rates,
      {
        locations: 1,
        teamUsers: 1,
        qrPlacements: 0,
        offers: 0,
        emailCredits: 0,
        smsCredits: 0,
        aiCredits: 0,
      },
      "monthly"
    )
    expect(result.displayNetPence).toBe(4200)
    expect(result.monthlyNetPence).toBe(4200)
  })
})

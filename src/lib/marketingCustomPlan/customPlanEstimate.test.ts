import { describe, expect, it } from "vitest"

import type { AdminCommercialCustomUnitRate } from "@/api/adminCommercialCatalogApi"
import {
  decodeCustomPlanIntentV2,
  encodeCustomPlanIntentV2,
  estimateCustomPlanV2,
  lineNetPence,
  minBasePlanForLocations,
  normalizePackCounts,
  type PublicCatalogPlanSummary,
} from "@/lib/marketingCustomPlan/customPlanEstimate"

function rate(
  partial: Partial<AdminCommercialCustomUnitRate> & {
    rateKey: string
    monthlyNetPence: number
  },
): AdminCommercialCustomUnitRate {
  return {
    id: partial.id ?? "r1",
    rateKey: partial.rateKey,
    unitQuantity: partial.unitQuantity ?? 1,
    monthlyNetPence: partial.monthlyNetPence,
    annualNetPence: partial.annualNetPence ?? null,
  }
}

const PLANS: PublicCatalogPlanSummary[] = [
  {
    planKey: "starter",
    displayName: "Starter",
    monthlyNetPence: 3900,
    annualNetPence: 39800,
    includedLocations: 1,
    includedTeamMembers: 3,
    activeOffersAccount: 3,
    activeQrPlacementsPerLocation: 10,
    creditsMonthlyAi: 100,
    creditsMonthlyEmail: 2500,
    creditsMonthlySms: 100,
  },
  {
    planKey: "growth",
    displayName: "Growth",
    monthlyNetPence: 9900,
    annualNetPence: 101000,
    includedLocations: 3,
    includedTeamMembers: 10,
    activeOffersAccount: 10,
    activeQrPlacementsPerLocation: 25,
    creditsMonthlyAi: 500,
    creditsMonthlyEmail: 10000,
    creditsMonthlySms: 350,
  },
  {
    planKey: "group",
    displayName: "Group",
    monthlyNetPence: 19900,
    annualNetPence: 203000,
    includedLocations: 5,
    includedTeamMembers: 25,
    activeOffersAccount: 25,
    activeQrPlacementsPerLocation: 50,
    creditsMonthlyAi: 1500,
    creditsMonthlyEmail: 25000,
    creditsMonthlySms: 700,
  },
]

const RATES = [
  rate({ rateKey: "location", monthlyNetPence: 3900, annualNetPence: 39800 }),
  rate({
    rateKey: "team_user",
    unitQuantity: 5,
    monthlyNetPence: 1500,
    annualNetPence: null,
  }),
  rate({
    rateKey: "offer",
    unitQuantity: 5,
    monthlyNetPence: 1200,
    annualNetPence: null,
  }),
  rate({
    rateKey: "qr_placement",
    unitQuantity: 10,
    monthlyNetPence: 600,
    annualNetPence: null,
  }),
  rate({
    rateKey: "email_credit",
    unitQuantity: 1000,
    monthlyNetPence: 120,
    annualNetPence: null,
  }),
]

describe("lineNetPence", () => {
  it("uses monthly for monthly cadence", () => {
    expect(
      lineNetPence(rate({ rateKey: "location", monthlyNetPence: 3900 }), 2, "monthly"),
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
        "annual",
      ),
    ).toBe(79600)
  })

  it("falls back to monthly × 12 × 0.85 when annual unset", () => {
    expect(
      lineNetPence(
        rate({ rateKey: "team_user", monthlyNetPence: 300, annualNetPence: null }),
        1,
        "annual",
      ),
    ).toBe(3060)
  })
})

describe("minBasePlanForLocations", () => {
  it("maps 1 → starter, 2–3 → growth, 4+ → group", () => {
    expect(minBasePlanForLocations(1, PLANS)).toBe("starter")
    expect(minBasePlanForLocations(2, PLANS)).toBe("growth")
    expect(minBasePlanForLocations(3, PLANS)).toBe("growth")
    expect(minBasePlanForLocations(4, PLANS)).toBe("group")
    expect(minBasePlanForLocations(6, PLANS)).toBe("group")
  })
})

describe("estimateCustomPlanV2", () => {
  it("uses Starter for 1 Location even when Group was requested", () => {
    const result = estimateCustomPlanV2({
      cadence: "monthly",
      requestedBasePlan: "group",
      locations: 1,
      packs: {},
      plans: PLANS,
      rates: RATES,
    })
    expect(result.basePlan).toBe("starter")
    expect(result.basePlanChanged).toBe(true)
    expect(result.storedAsPlan).toBe("Starter")
    expect(result.periodNetPence).toBe(3900)
  })

  it("keeps Starter with packs instead of moving to Growth", () => {
    const result = estimateCustomPlanV2({
      cadence: "monthly",
      requestedBasePlan: null,
      locations: 1,
      packs: { offers_5: 1, team_users_5: 1, sms_1000: 1 },
      plans: PLANS,
      rates: RATES,
    })
    expect(result.basePlan).toBe("starter")
    expect(result.basePlanChanged).toBe(false)
    expect(result.storedAsPlan).toBe("Custom")
  })

  it("auto-raises base plan when locations exceed the requested package", () => {
    const result = estimateCustomPlanV2({
      cadence: "monthly",
      requestedBasePlan: "growth",
      locations: 5,
      packs: {},
      plans: PLANS,
      rates: RATES,
    })
    expect(result.basePlan).toBe("group")
    expect(result.basePlanChanged).toBe(true)
    expect(result.requestedBasePlan).toBe("growth")
  })

  it("prices packs and marks storedAsPlan Custom", () => {
    const result = estimateCustomPlanV2({
      cadence: "monthly",
      requestedBasePlan: "starter",
      locations: 1,
      packs: { offers_5: 1, email_20k: 1 },
      plans: PLANS,
      rates: RATES,
    })
    // starter 3900 + offer pack 1200 + email 20k = 20 * 120 = 2400 → 7500
    expect(result.periodNetPence).toBe(7500)
    expect(result.storedAsPlan).toBe("Custom")
    expect(result.quantities.offers).toBe(8)
    expect(result.quantities.emailCredits).toBe(22_500)
  })

  it("prices additional Locations on Group with unit rate", () => {
    const result = estimateCustomPlanV2({
      cadence: "monthly",
      requestedBasePlan: "group",
      locations: 7,
      packs: {},
      plans: PLANS,
      rates: RATES,
      extraLocation: {
        usersAdded: 2,
        creditsMonthlyAi: 50,
        creditsMonthlyEmail: 1000,
        creditsMonthlySms: 20,
      },
    })
    expect(result.additionalLocations).toBe(2)
    // group 19900 + 2 × 3900 = 27700
    expect(result.periodNetPence).toBe(27700)
    expect(result.quantities.teamUsers).toBe(25 + 4)
    expect(result.quantities.emailCredits).toBe(25000 + 2000)
    expect(result.storedAsPlan).toBe("Custom")
  })

  it("flags contact sales above 500 Locations", () => {
    const result = estimateCustomPlanV2({
      cadence: "monthly",
      requestedBasePlan: "group",
      locations: 501,
      packs: {},
      plans: PLANS,
      rates: RATES,
    })
    expect(result.contactSales).toBe(true)
  })

  it("treats QR pack as account-wide pool, not per Location", () => {
    const result = estimateCustomPlanV2({
      cadence: "monthly",
      requestedBasePlan: "growth",
      locations: 3,
      packs: { qr_10: 2 },
      plans: PLANS,
      rates: RATES,
    })
    expect(result.quantities.qrPlacements).toBe(25)
    expect(result.quantities.qrPlacementsAccountExtra).toBe(20)
    // growth 9900 + 2 × 600 = 11100
    expect(result.periodNetPence).toBe(11100)
  })
})

describe("encode/decode CustomPlanIntentV2", () => {
  it("round-trips compact v2 config", () => {
    const encoded = encodeCustomPlanIntentV2({
      version: 2,
      cadence: "annual",
      basePlan: "growth",
      locations: 3,
      packs: { offers_5: 2, email_20k: 0 },
    })
    expect(decodeCustomPlanIntentV2(encoded)).toEqual({
      version: 2,
      cadence: "annual",
      basePlan: "growth",
      locations: 3,
      packs: normalizePackCounts({ offers_5: 2 }),
    })
  })

  it("parses raw JSON without base64", () => {
    const json = JSON.stringify({
      version: 2,
      cadence: "monthly",
      basePlan: "starter",
      locations: 1,
      packs: { team_users_5: 1 },
    })
    expect(decodeCustomPlanIntentV2(json)?.packs.team_users_5).toBe(1)
  })
})

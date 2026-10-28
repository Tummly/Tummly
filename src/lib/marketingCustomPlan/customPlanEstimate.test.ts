import { describe, expect, it } from "vitest"

import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import {
  decodeCustomPlanIntentV2,
  encodeCustomPlanIntentV2,
  minBasePlanForLocations,
  CUSTOM_PLAN_MAX_CAPACITY_UNITS,
  CUSTOM_PLAN_PACKS,
  hasLegacyCapacityPack,
  normalizePackCounts,
  packMaxCount,
  quotePackLineAmount,
  type PublicCatalogPlanSummary,
} from "@/lib/marketingCustomPlan/customPlanEstimate"

describe("single-unit capacity packs", () => {
  it("sells team users, Offers and QR placements one unit at a time", () => {
    for (const key of ["team_user", "offer", "qr_placement"] as const) {
      const def = CUSTOM_PLAN_PACKS.find((p) => p.packKey === key)
      expect(def?.packSize).toBe(1)
      expect(def?.maxCount).toBe(CUSTOM_PLAN_MAX_CAPACITY_UNITS)
    }
    expect(CUSTOM_PLAN_PACKS.map((p) => p.packKey)).not.toContain("team_users_5")
  })

  it("keeps the credit pack bound", () => {
    expect(packMaxCount("email_20k")).toBe(99)
    expect(packMaxCount("team_user")).toBe(500)
  })

  it("flags a replaced capacity pack key", () => {
    expect(hasLegacyCapacityPack({ qr_10: 0 })).toBe(true)
    expect(hasLegacyCapacityPack({ qr_placement: 3 })).toBe(false)
    expect(hasLegacyCapacityPack(null)).toBe(false)
  })
})

describe("normalizePackCounts", () => {
  it("keeps a typed count as typed, so an invalid count stays visible", () => {
    const packs = normalizePackCounts({ offer: 150, qr_placement: 2.5 })
    expect(packs.offer).toBe(150)
    expect(packs.qr_placement).toBe(2.5)
  })

  it("drops non-numeric values and fills missing packs with zero", () => {
    const packs = normalizePackCounts({
      offer: Number.NaN,
      team_user: 3,
    } as Record<string, number>)
    expect(packs.offer).toBe(0)
    expect(packs.team_user).toBe(3)
    expect(packs.sms_500).toBe(0)
  })
})

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

describe("minBasePlanForLocations", () => {
  it("maps 1 → starter, 2–3 → growth, 4+ → group", () => {
    expect(minBasePlanForLocations(1, PLANS)).toBe("starter")
    expect(minBasePlanForLocations(2, PLANS)).toBe("growth")
    expect(minBasePlanForLocations(3, PLANS)).toBe("growth")
    expect(minBasePlanForLocations(4, PLANS)).toBe("group")
    expect(minBasePlanForLocations(6, PLANS)).toBe("group")
  })
})

describe("encode/decode CustomPlanIntentV2", () => {
  it("round-trips compact v2 config", () => {
    const encoded = encodeCustomPlanIntentV2({
      version: 2,
      cadence: "annual",
      basePlan: "growth",
      locations: 3,
      packs: { offer: 2, email_20k: 0 },
    })
    expect(decodeCustomPlanIntentV2(encoded)).toEqual({
      version: 2,
      cadence: "annual",
      basePlan: "growth",
      locations: 3,
      packs: normalizePackCounts({ offer: 2 }),
    })
  })

  it("drops a saved selection that names a replaced capacity pack", () => {
    for (const legacyKey of ["team_users_5", "offers_5", "qr_10"]) {
      const json = JSON.stringify({
        version: 2,
        cadence: "monthly",
        basePlan: "starter",
        locations: 1,
        packs: { [legacyKey]: 1 },
      })
      expect(decodeCustomPlanIntentV2(json)).toBeNull()
      expect(decodeCustomPlanIntentV2(btoa(json))).toBeNull()
    }
  })

  it("parses raw JSON without base64", () => {
    const json = JSON.stringify({
      version: 2,
      cadence: "monthly",
      basePlan: "starter",
      locations: 1,
      packs: { team_user: 1 },
    })
    expect(decodeCustomPlanIntentV2(json)?.packs.team_user).toBe(1)
  })
})

describe("capacity copy", () => {
  it("does not name a period for the QR cost row", async () => {
    const { CUSTOM_PLAN_STEPS } = await import("@/content/marketing/customPlanPage")
    expect(CUSTOM_PLAN_STEPS.capacity.qrExtraCostLabel).toBe("Additional cost")
  })
})

describe("minBasePlanForLocations without catalogue plans", () => {
  it("returns null instead of a browser-side band guess", () => {
    expect(minBasePlanForLocations(2, [])).toBeNull()
  })
})

function quoteFixture(
  overrides: Partial<CustomPlanQuoteResult>,
): CustomPlanQuoteResult {
  return {
    version: 2,
    catalogPublicId: "cat-1",
    cadence: "monthly",
    basePlan: "starter",
    locations: 1,
    packs: {},
    quantities: {
      locations: 1,
      teamUsers: 3,
      qrPlacements: 10,
      qrPlacementsAccountExtra: 0,
      offers: 3,
      emailCredits: 2500,
      smsCredits: 100,
      aiCredits: 100,
    },
    monthlyNetPence: 3900,
    periodNetPence: 3900,
    lines: [],
    basePlanChanged: false,
    requestedBasePlan: null,
    storedAsPlan: "Starter",
    quoteHash: "0123456789abcdef0123456789abcdef",
    vatRateBps: 0,
    vatAmountPence: 0,
    grossTotalPence: 3900,
    recurringGrossPence: 3900,
    dueNowGrossPence: 3900,
    effectiveTiming: "on_payment_confirmed",
    ...overrides,
  }
}

describe("quotePackLineAmount", () => {
  const qrLine = { key: "pack:qr_placement", label: "Extra QR placements × 1", quantity: 1 }

  it("takes the QR amount and monthly suffix from the server line", () => {
    const quote = quoteFixture({
      cadence: "monthly",
      lines: [{ ...qrLine, netPence: 600 }],
    })
    expect(quotePackLineAmount(quote, "qr_placement")).toEqual({
      netPence: 600,
      suffix: "/mo",
    })
  })

  it("takes the QR amount and yearly suffix from the server line for annual", () => {
    const quote = quoteFixture({
      cadence: "annual",
      lines: [{ ...qrLine, netPence: 6000 }],
    })
    expect(quotePackLineAmount(quote, "qr_placement")).toEqual({
      netPence: 6000,
      suffix: "/ per year",
    })
  })

  it("is null without a QR line", () => {
    expect(quotePackLineAmount(quoteFixture({ lines: [] }), "qr_placement")).toBeNull()
  })

  it("is null without a quote", () => {
    expect(quotePackLineAmount(null, "qr_placement")).toBeNull()
  })
})

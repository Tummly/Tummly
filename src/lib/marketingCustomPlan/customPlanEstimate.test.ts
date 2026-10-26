import { describe, expect, it } from "vitest"

import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import {
  decodeCustomPlanIntentV2,
  encodeCustomPlanIntentV2,
  minBasePlanForLocations,
  normalizePackCounts,
  quotePackLineAmount,
  type PublicCatalogPlanSummary,
} from "@/lib/marketingCustomPlan/customPlanEstimate"

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
    ...overrides,
  }
}

describe("quotePackLineAmount", () => {
  const qrLine = { key: "pack:qr_10", label: "Extra QR placements × 1", quantity: 1 }

  it("takes the QR amount and monthly suffix from the server line", () => {
    const quote = quoteFixture({
      cadence: "monthly",
      lines: [{ ...qrLine, netPence: 600 }],
    })
    expect(quotePackLineAmount(quote, "qr_10")).toEqual({
      netPence: 600,
      suffix: "/mo",
    })
  })

  it("takes the QR amount and yearly suffix from the server line for annual", () => {
    const quote = quoteFixture({
      cadence: "annual",
      lines: [{ ...qrLine, netPence: 6000 }],
    })
    expect(quotePackLineAmount(quote, "qr_10")).toEqual({
      netPence: 6000,
      suffix: "/ per year",
    })
  })

  it("is null without a QR line", () => {
    expect(quotePackLineAmount(quoteFixture({ lines: [] }), "qr_10")).toBeNull()
  })

  it("is null without a quote", () => {
    expect(quotePackLineAmount(null, "qr_10")).toBeNull()
  })
})

import { describe, expect, it } from "vitest"

import {
  decodeCustomPlanIntentV2,
  encodeCustomPlanIntentV2,
  minBasePlanForLocations,
  normalizePackCounts,
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

describe("minBasePlanForLocations without catalogue plans", () => {
  it("returns null instead of a browser-side band guess", () => {
    expect(minBasePlanForLocations(2, [])).toBeNull()
  })
})

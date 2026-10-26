import { describe, expect, it } from "vitest"

import type { PublicCatalogPlanSummaryDto } from "@/api/adminCommercialCatalogApi"
import {
  buildPlanCostAnswer,
  toPublicPlanPrices,
} from "@/lib/marketingCustomPlan/publicPlanPrices"

function plan(
  planKey: string,
  displayName: string,
  monthlyNetPence: number,
  annualNetPence: number,
): PublicCatalogPlanSummaryDto {
  return {
    planKey,
    displayName,
    monthlyNetPence,
    annualNetPence,
    includedLocations: 1,
    includedTeamMembers: 1,
    activeOffersAccount: 1,
    activeQrPlacementsPerLocation: 1,
    creditsMonthlyAi: 0,
    creditsMonthlyEmail: 0,
    creditsMonthlySms: 0,
  }
}

const SEEDED_PLANS = [
  plan("starter", "Starter", 3900, 39800),
  plan("growth", "Growth", 9900, 101000),
  plan("group", "Group", 19900, 203000),
]

describe("toPublicPlanPrices", () => {
  it("formats the seeded paid plans in whole pounds", () => {
    expect(toPublicPlanPrices(SEEDED_PLANS)).toEqual({
      Starter: { monthly: "£39", annual: "£398" },
      Growth: { monthly: "£99", annual: "£1,010" },
      Group: { monthly: "£199", annual: "£2,030" },
    })
  })

  it("ignores catalogue rows that are not a paid public plan", () => {
    expect(
      toPublicPlanPrices([plan("pilot", "Pilot", 0, 0), ...SEEDED_PLANS]),
    ).not.toHaveProperty("Pilot")
  })

  it("keeps pence when a price is not whole pounds", () => {
    expect(
      toPublicPlanPrices([plan("starter", "Starter", 3950, 39800)]).Starter,
    ).toEqual({ monthly: "£39.50", annual: "£398" })
  })
})

describe("buildPlanCostAnswer", () => {
  it("builds the doc 10 cost sentence from the server prices", () => {
    expect(buildPlanCostAnswer(toPublicPlanPrices(SEEDED_PLANS))).toBe(
      "Starter is £39/month or £398/year; Growth is £99/month or £1,010/year; Group is £199/month or £2,030/year before selected extras. Compare the current quote before confirming.",
    )
  })

  it("returns null when the Group price is missing", () => {
    const prices = toPublicPlanPrices(SEEDED_PLANS.slice(0, 2))
    expect(buildPlanCostAnswer(prices)).toBeNull()
  })

  it("returns null when no prices have loaded", () => {
    expect(buildPlanCostAnswer({})).toBeNull()
  })
})

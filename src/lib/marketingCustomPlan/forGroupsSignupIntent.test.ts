import { describe, expect, it } from "vitest"

import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import { normalizePackCounts } from "@/lib/marketingCustomPlan/customPlanEstimate"
import { buildForGroupsSignupIntent } from "@/lib/marketingCustomPlan/forGroupsSignupIntent"

function quote(
  partial: Partial<CustomPlanQuoteResult> = {},
): CustomPlanQuoteResult {
  return {
    version: 2,
    catalogPublicId: "catalog-1",
    cadence: "monthly",
    basePlan: "group",
    locations: 31,
    packs: {},
    quantities: {
      locations: 31,
      teamUsers: 25,
      qrPlacements: 1,
      qrPlacementsAccountExtra: 0,
      offers: 1,
      emailCredits: 0,
      smsCredits: 0,
      aiCredits: 0,
    },
    monthlyNetPence: 100000,
    periodNetPence: 100000,
    lines: [],
    basePlanChanged: false,
    requestedBasePlan: null,
    storedAsPlan: "Custom",
    quoteHash: "b".repeat(32),
    vatRateBps: 0,
    ...partial,
  }
}

describe("buildForGroupsSignupIntent", () => {
  it("sends plain Group at 5 Locations", () => {
    expect(
      buildForGroupsSignupIntent({
        locations: 5,
        includedLocations: 5,
        cadence: "annual",
        quote: null,
      }),
    ).toEqual({ plan: "Group", cadence: "annual" })
  })

  it("sends the v2 config with the quote hash at 31 Locations", () => {
    const intent = buildForGroupsSignupIntent({
      locations: 31,
      includedLocations: 5,
      cadence: "monthly",
      quote: quote(),
    })
    expect(intent).toEqual({
      plan: "Custom",
      cadence: "monthly",
      config: {
        version: 2,
        cadence: "monthly",
        basePlan: "group",
        locations: 31,
        packs: normalizePackCounts({}),
        quoteHash: "b".repeat(32),
      },
    })
  })

  it("returns null when the quote is missing or does not match", () => {
    const base = { locations: 31, includedLocations: 5, cadence: "monthly" as const }
    expect(buildForGroupsSignupIntent({ ...base, quote: null })).toBeNull()
    expect(
      buildForGroupsSignupIntent({ ...base, quote: quote({ cadence: "annual" }) }),
    ).toBeNull()
    expect(
      buildForGroupsSignupIntent({ ...base, quote: quote({ locations: 30 }) }),
    ).toBeNull()
  })
})

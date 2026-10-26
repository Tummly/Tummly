import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import {
  normalizePackCounts,
  storedPlanFromQuote,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import type { SignupPlanIntent } from "@/lib/signupPlanIntent"

/**
 * Signup intent for "Continue with Group".
 * Up to the included Locations: plain Group. Above: the same v2 config that the
 * Custom Plan page sends, with the hash of the ready quote.
 * Returns null when a configured plan has no matching ready quote.
 */
export function buildForGroupsSignupIntent(args: {
  locations: number
  includedLocations: number
  cadence: BillingCadence
  quote: CustomPlanQuoteResult | null
}): SignupPlanIntent | null {
  const { locations, includedLocations, cadence, quote } = args
  if (locations <= includedLocations) {
    return { plan: "Group", cadence }
  }
  if (
    quote == null ||
    quote.cadence !== cadence ||
    quote.locations !== locations ||
    quote.basePlan !== "group"
  ) {
    return null
  }
  return {
    plan: storedPlanFromQuote(quote.storedAsPlan),
    cadence,
    config: {
      version: 2,
      cadence,
      basePlan: "group",
      locations: quote.locations,
      packs: normalizePackCounts(quote.packs),
      quoteHash: quote.quoteHash,
    },
  }
}

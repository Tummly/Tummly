import type { PublicCatalogPlanSummaryDto } from "@/api/adminCommercialCatalogApi"
import { formatPenceGbp } from "@/lib/marketingCustomPlan/customPlanQuoteState"

export type PublicPlanPrice = { monthly: string; annual: string }

export type PublicPlanName = "Starter" | "Growth" | "Group"

export type PublicPlanPrices = Partial<Record<PublicPlanName, PublicPlanPrice>>

const PAID_PLAN_ORDER: readonly PublicPlanName[] = ["Starter", "Growth", "Group"]

function paidPlanName(planKey: string): PublicPlanName | null {
  switch (planKey.trim().toLowerCase()) {
    case "starter":
      return "Starter"
    case "growth":
      return "Growth"
    case "group":
      return "Group"
    default:
      return null
  }
}

/** Marketing prices from the published catalogue. Pilot is not a paid plan here. */
export function toPublicPlanPrices(
  plans: PublicCatalogPlanSummaryDto[],
): PublicPlanPrices {
  const prices: PublicPlanPrices = {}
  for (const plan of plans) {
    const name = paidPlanName(plan.planKey)
    if (name == null) {
      continue
    }
    prices[name] = {
      monthly: formatPenceGbp(plan.monthlyNetPence),
      annual: formatPenceGbp(plan.annualNetPence),
    }
  }
  return prices
}

/**
 * Doc 10 FAQ.N03.13 cost sentence. Null when any paid plan price is missing;
 * the caller then shows only "Compare the current quote before confirming."
 */
export function buildPlanCostAnswer(prices: PublicPlanPrices): string | null {
  const parts: string[] = []
  for (const name of PAID_PLAN_ORDER) {
    const price = prices[name]
    if (price == null) {
      return null
    }
    parts.push(`${name} is ${price.monthly}/month or ${price.annual}/year`)
  }
  const [starter, growth, group] = parts
  return `${starter}; ${growth}; ${group} before selected extras. Compare the current quote before confirming.`
}

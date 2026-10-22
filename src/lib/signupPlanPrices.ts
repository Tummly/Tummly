import type { PublicCatalogPlanSummaryDto } from "@/api/adminCommercialCatalogApi"
import {
  SIGNUP_PILOT_PRICE,
  type SignupPlanCardId,
} from "@/content/marketing/signupComparePlans"
import { formatPenceGbp } from "@/lib/marketingCustomPlan/customPlanEstimate"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"

export type SignupPlanPriceView = {
  amount: string
  suffix: string
  subline: string
}

/** Display price for one plan card from the published catalog (Pilot is free). */
export function signupPlanPrice(
  plan: SignupPlanCardId,
  cadence: BillingCadence,
  plans: PublicCatalogPlanSummaryDto[],
): SignupPlanPriceView | null {
  if (plan === "Pilot") {
    return SIGNUP_PILOT_PRICE
  }
  const row = plans.find((p) => p.planKey.toLowerCase() === plan.toLowerCase())
  if (row == null) {
    return null
  }
  if (cadence === "annual") {
    return {
      amount: formatPenceGbp(row.annualNetPence),
      suffix: "/ year",
      subline: `${formatPenceGbp(row.monthlyNetPence)}/month billed monthly`,
    }
  }
  return {
    amount: formatPenceGbp(row.monthlyNetPence),
    suffix: "/ month",
    subline: `${formatPenceGbp(row.annualNetPence)}/year`,
  }
}

export function signupPlanPrices(
  cadence: BillingCadence,
  plans: PublicCatalogPlanSummaryDto[],
): Record<SignupPlanCardId, SignupPlanPriceView | null> {
  return {
    Pilot: signupPlanPrice("Pilot", cadence, plans),
    Starter: signupPlanPrice("Starter", cadence, plans),
    Growth: signupPlanPrice("Growth", cadence, plans),
    Group: signupPlanPrice("Group", cadence, plans),
  }
}

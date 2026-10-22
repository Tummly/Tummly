import type { PublicCatalogPlanSummaryDto } from "@/api/adminCommercialCatalogApi"
import {
  parseBasePlanKey,
  type PublicCatalogPlanSummary,
} from "@/lib/marketingCustomPlan/customPlanEstimate"

export function toPublicCatalogPlanSummaries(
  plans: PublicCatalogPlanSummaryDto[],
): PublicCatalogPlanSummary[] {
  const mapped: PublicCatalogPlanSummary[] = []
  for (const plan of plans) {
    const planKey = parseBasePlanKey(plan.planKey)
    if (planKey == null) continue
    mapped.push({
      planKey,
      displayName: plan.displayName || planKey,
      monthlyNetPence: plan.monthlyNetPence,
      annualNetPence: plan.annualNetPence,
      includedLocations: plan.includedLocations,
      includedTeamMembers: plan.includedTeamMembers,
      activeOffersAccount: plan.activeOffersAccount,
      activeQrPlacementsPerLocation: plan.activeQrPlacementsPerLocation,
      creditsMonthlyAi: plan.creditsMonthlyAi,
      creditsMonthlyEmail: plan.creditsMonthlyEmail,
      creditsMonthlySms: plan.creditsMonthlySms,
    })
  }
  return mapped
}

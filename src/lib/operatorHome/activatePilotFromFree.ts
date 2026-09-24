import { submitBillingPlanChange } from "@/api/billingCreditsApi"
import { MANAGE_PLAN_COPY } from "@/lib/operatorBillingCredits/managePlanPresentation"

export const ACTIVATE_PILOT_FROM_FREE_CONFIRM = {
  title: MANAGE_PLAN_COPY.startPilot,
  body: MANAGE_PLAN_COPY.confirmActivatePilotBody,
  primaryLabel: MANAGE_PLAN_COPY.confirmPrimaryActivatePilot,
} as const

/** Free → Pilot via Manage Plan plan-change (no Revolut). */
export async function submitActivatePilotFromFree(): Promise<void> {
  const result = await submitBillingPlanChange({
    targetPlan: "Pilot",
    targetCadence: "monthly",
  })
  if (result.outcome !== "applied") {
    throw new Error("activate_pilot_failed")
  }
}

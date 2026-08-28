import axiosInstance from "@/api/axiosInstance"
import type {
  BillingCreditsPageData,
  PlanChangeRequest,
  PlanChangeResult,
} from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import type { CreditsUsageSnapshot } from "@/lib/operatorBillingCredits/creditsUsagePresentation"

type BillingCreditsPageResponse = {
  actorPermissionRole: string
  actorCanManage: boolean
  planSubscription: BillingCreditsPageData["planSubscription"]
}

type PlanChangeResponse = {
  outcome: "pay" | "scheduled"
  redirectUrl?: string | null
  scheduledChangeLine?: string | null
}

export async function getBillingCreditsPage(): Promise<BillingCreditsPageData> {
  const { data } = await axiosInstance.get<BillingCreditsPageResponse>(
    "/billing-credits"
  )
  return {
    actorPermissionRole: data.actorPermissionRole,
    actorCanManage: data.actorCanManage,
    planSubscription: data.planSubscription,
  }
}

export async function getBillingCreditsUsage(): Promise<CreditsUsageSnapshot> {
  const { data } = await axiosInstance.get<CreditsUsageSnapshot>(
    "/billing-credits/usage"
  )
  return data
}

export async function submitBillingPlanChange(
  request: PlanChangeRequest
): Promise<PlanChangeResult> {
  const { data } = await axiosInstance.post<PlanChangeResponse>(
    "/billing-credits/plan-change",
    {
      targetPlan: request.targetPlan,
      targetCadence: request.targetCadence,
    }
  )
  return {
    outcome: data.outcome,
    redirectUrl: data.redirectUrl,
    scheduledChangeLine: data.scheduledChangeLine,
  }
}

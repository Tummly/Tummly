import axiosInstance from "@/api/axiosInstance"
import type { BillingCreditsPageData } from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import type { CreditsUsageSnapshot } from "@/lib/operatorBillingCredits/creditsUsagePresentation"

type BillingCreditsPageResponse = {
  actorPermissionRole: string
  actorCanManage: boolean
  planSubscription: BillingCreditsPageData["planSubscription"]
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

import axiosInstance from "@/api/axiosInstance"
import type { BillingCreditsPageData } from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"

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

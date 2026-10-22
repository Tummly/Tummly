import axiosInstance from "@/api/axiosInstance"
import {
  normalizePlanEntitlementsAccount,
  type PlanEntitlementLimit,
} from "@/lib/planEntitlements/planEntitlementsPresentation"

/** GET /api/plan-entitlements — account location cap/current for signup setup. */
export async function fetchSignupLocationCapacity(): Promise<PlanEntitlementLimit> {
  const { data } = await axiosInstance.get<{
    success?: boolean
    entitlements?: {
      account?: Record<string, unknown>
    }
  }>("/plan-entitlements")

  const account = normalizePlanEntitlementsAccount(
    data.entitlements?.account ?? null
  )
  return account.locations
}

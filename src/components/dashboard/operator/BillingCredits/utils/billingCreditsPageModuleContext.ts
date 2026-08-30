import { createContext, useContext } from "react"

import type { OperatorBillingCreditsPageModule } from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"

export const billingCreditsPageModuleContext =
  createContext<OperatorBillingCreditsPageModule | null>(null)

export function useBillingCreditsPageModuleApi(): OperatorBillingCreditsPageModule {
  const value = useContext(billingCreditsPageModuleContext)
  if (value == null) {
    throw new Error(
      "useBillingCreditsPageModuleApi must be used within BillingCreditsPageModuleProvider"
    )
  }
  return value
}

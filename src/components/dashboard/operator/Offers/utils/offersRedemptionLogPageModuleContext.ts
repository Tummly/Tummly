import { createContext, useContext } from "react"

import type { OperatorOffersRedemptionLogModule } from "@/lib/operatorOffers/createOperatorOffersRedemptionLogModule"

export const offersRedemptionLogPageModuleContext =
  createContext<OperatorOffersRedemptionLogModule | null>(null)

export function useOffersRedemptionLogPageModuleApi(): OperatorOffersRedemptionLogModule {
  const value = useContext(offersRedemptionLogPageModuleContext)
  if (value == null) {
    throw new Error(
      "useOffersRedemptionLogPageModuleApi requires OffersRedemptionLogPageModuleProvider"
    )
  }
  return value
}

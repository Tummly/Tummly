import { createContext, useContext } from "react"

import type { OperatorOffersPageModule } from "@/lib/operatorOffers/createOperatorOffersPageModule"

export const offersPageModuleContext =
  createContext<OperatorOffersPageModule | null>(null)

export function useOffersPageModuleApi(): OperatorOffersPageModule {
  const pageModule = useContext(offersPageModuleContext)
  if (pageModule == null) {
    throw new Error(
      "useOffersPageModuleApi must be used within OffersPageModuleProvider"
    )
  }
  return pageModule
}

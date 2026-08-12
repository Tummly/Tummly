import { createContext, useContext } from "react"

import type { VoidRequestModule } from "@/lib/operatorOffers/createVoidRequestModule"

export const voidRequestModuleContext =
  createContext<VoidRequestModule | null>(null)

export function useVoidRequestModuleApi(): VoidRequestModule {
  const voidRequest = useContext(voidRequestModuleContext)
  if (voidRequest == null) {
    throw new Error(
      "useVoidRequestModuleApi must be used within OffersPageModuleProvider"
    )
  }
  return voidRequest
}

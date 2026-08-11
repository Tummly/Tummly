import { createContext, useContext } from "react"

import type { OfferDetailsPageModule } from "@/lib/operatorOffers/createOfferDetailsPageModule"

export const offerDetailsPageModuleContext =
  createContext<OfferDetailsPageModule | null>(null)

export function useOfferDetailsPageModuleApi(): OfferDetailsPageModule {
  const value = useContext(offerDetailsPageModuleContext)
  if (value == null) {
    throw new Error(
      "useOfferDetailsPageModuleApi requires OfferDetailsPageModuleProvider"
    )
  }
  return value
}

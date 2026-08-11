import { createElement, useState, type ReactNode } from "react"

import { getCatalogOfferById } from "@/api/dashboardApi"
import { offerDetailsPageModuleContext } from "@/components/dashboard/operator/Offers/utils/offerDetailsPageModuleContext"
import { createOfferDetailsPageModule } from "@/lib/operatorOffers/createOfferDetailsPageModule"

export function OfferDetailsPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() =>
    createOfferDetailsPageModule({
      getOffer: async (offerId) => {
        const response = await getCatalogOfferById(offerId)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog get failed.")
        }
        return response.offer
      },
    })
  )

  return createElement(
    offerDetailsPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

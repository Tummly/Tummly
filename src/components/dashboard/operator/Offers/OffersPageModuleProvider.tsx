import { createElement, useState, type ReactNode } from "react"

import { createCatalogOffer, listCatalogOffers } from "@/api/dashboardApi"
import { offersPageModuleContext } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import { staffRedeemModuleContext } from "@/components/dashboard/operator/Offers/utils/staffRedeemModuleContext"
import { createOperatorOffersPageModule } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import { createStaffRedeemModule } from "@/lib/operatorOffers/createStaffRedeemModule"
import { createStubStaffRedeemAdapters } from "@/lib/operatorOffers/staffRedeemAdapters"

export function OffersPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() =>
    createOperatorOffersPageModule({
      listCatalogOffers,
      createOffer: async (body) => {
        const response = await createCatalogOffer(body)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog create failed.")
        }
        return response.offer
      },
    })
  )
  // Swap stub adapters for real check/redeem APIs when those writes go live.
  const [staffRedeemModule] = useState(() =>
    createStaffRedeemModule(createStubStaffRedeemAdapters())
  )

  return createElement(
    offersPageModuleContext.Provider,
    { value: pageModule },
    createElement(
      staffRedeemModuleContext.Provider,
      { value: staffRedeemModule },
      children
    )
  )
}

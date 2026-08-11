import { createElement, useState, type ReactNode } from "react"

import { listCatalogOffers } from "@/api/dashboardApi"
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
    createOperatorOffersPageModule({ listCatalogOffers })
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

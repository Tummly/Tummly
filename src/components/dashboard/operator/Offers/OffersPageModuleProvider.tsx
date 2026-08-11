import { createElement, useState, type ReactNode } from "react"

import { listCatalogOffers } from "@/api/dashboardApi"
import { offersPageModuleContext } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import { createOperatorOffersPageModule } from "@/lib/operatorOffers/createOperatorOffersPageModule"

export function OffersPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() =>
    createOperatorOffersPageModule({ listCatalogOffers })
  )

  return createElement(
    offersPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

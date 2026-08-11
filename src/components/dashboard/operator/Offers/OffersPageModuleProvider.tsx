import { createElement, useState, type ReactNode } from "react"

import { offersPageModuleContext } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import { createOperatorOffersPageModule } from "@/lib/operatorOffers/createOperatorOffersPageModule"

export function OffersPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() => createOperatorOffersPageModule())

  return createElement(
    offersPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

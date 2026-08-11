import { createElement, useState, type ReactNode } from "react"

import { offersRedemptionLogPageModuleContext } from "@/components/dashboard/operator/Offers/utils/offersRedemptionLogPageModuleContext"
import { createOperatorOffersRedemptionLogModule } from "@/lib/operatorOffers/createOperatorOffersRedemptionLogModule"

export function OffersRedemptionLogPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() => createOperatorOffersRedemptionLogModule())

  return createElement(
    offersRedemptionLogPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

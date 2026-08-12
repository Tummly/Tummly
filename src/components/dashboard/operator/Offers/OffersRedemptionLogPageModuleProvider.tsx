import { createElement, useState, type ReactNode } from "react"

import { getLocationRedemptions } from "@/api/dashboardApi"
import { offersRedemptionLogPageModuleContext } from "@/components/dashboard/operator/Offers/utils/offersRedemptionLogPageModuleContext"
import { createOperatorOffersRedemptionLogModule } from "@/lib/operatorOffers/createOperatorOffersRedemptionLogModule"
import { loadLocationRedemptionLogRows } from "@/lib/operatorOffers/offersRedemptionLogQuery"

export function OffersRedemptionLogPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() =>
    createOperatorOffersRedemptionLogModule({
      listRedemptions: (locationId) =>
        loadLocationRedemptionLogRows(locationId, {
          fetchRedemptions: getLocationRedemptions,
        }),
    })
  )

  return createElement(
    offersRedemptionLogPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

import { createElement, useState, type ReactNode } from "react"

import { campaignsPageModuleContext } from "@/components/dashboard/operator/Campaigns/utils/campaignsPageModuleContext"
import { createOperatorCampaignsPageModule } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"

/**
 * Empty-first overview adapter until the thin Campaign Draft list API ships.
 * Later tickets replace this with a live list call.
 */
async function loadEmptyCampaignsOverview(): Promise<{ totalCount: number }> {
  return { totalCount: 0 }
}

export function CampaignsPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() =>
    createOperatorCampaignsPageModule({
      loadOverview: loadEmptyCampaignsOverview,
    })
  )

  return createElement(
    campaignsPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

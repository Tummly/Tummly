import { createElement, useState, type ReactNode } from "react"

import { getGuests } from "@/api/dashboardApi"
import { campaignsPageModuleContext } from "@/components/dashboard/operator/Campaigns/utils/campaignsPageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { createOperatorCampaignsPageModule } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import type { CampaignsOverviewDateRange } from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import { emptySelection } from "@/lib/operatorFilterSheet"
import { guestsFilterSheetSchema } from "@/lib/operatorGuests/guestsFilterSheetSchema"
import { buildGuestsListQueryParams } from "@/lib/operatorGuests/guestsListQueryParams"
import { OPERATOR_GUEST_DEFAULT_SORT_ID } from "@/lib/operatorGuests/guestsPresentation"

/**
 * Empty-first overview adapter until the thin Campaign Draft list API ships.
 * Later tickets replace this with a live list call.
 */
async function loadEmptyCampaignsOverview(): Promise<{ totalCount: number }> {
  return { totalCount: 0 }
}

const GUESTS_SCHEMA = guestsFilterSheetSchema()

async function loadMarketingEligibleFromGuests(input: {
  locationId: number
  overviewDateRange: CampaignsOverviewDateRange
}): Promise<number> {
  const params = buildGuestsListQueryParams({
    locationId: input.locationId,
    smartGroup: "all-guests",
    q: "",
    sort: OPERATOR_GUEST_DEFAULT_SORT_ID,
    page: 1,
    pageSize: 1,
    filters: emptySelection(GUESTS_SCHEMA),
    overviewDateRange: input.overviewDateRange,
  })
  const response = await getGuests(params)
  return response.overview?.marketingEligible ?? 0
}

export function CampaignsPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const [pageModule] = useState(() =>
    createOperatorCampaignsPageModule({
      loadOverview: loadEmptyCampaignsOverview,
      loadMarketingEligible: loadMarketingEligibleFromGuests,
      getCampaignsOverviewDateRange: () =>
        dashboardUiStore.getState().campaignsOverviewDateRange,
    })
  )

  return createElement(
    campaignsPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

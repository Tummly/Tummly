import { createElement, useState, type ReactNode } from "react"

import {
  getCampaignsList,
  getCampaignsSummary,
  getCampaignRecommendation,
  getGuests,
} from "@/api/dashboardApi"
import { campaignsPageModuleContext } from "@/components/dashboard/operator/Campaigns/utils/campaignsPageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import {
  createOperatorCampaignsPageModule,
  type CampaignsSummarySiblingFacts,
} from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import {
  resolveCampaignsOverviewWindow,
  type CampaignsOverviewDateRange,
} from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import { emptySelection } from "@/lib/operatorFilterSheet"
import { guestsFilterSheetSchema } from "@/lib/operatorGuests/guestsFilterSheetSchema"
import { buildGuestsListQueryParams } from "@/lib/operatorGuests/guestsListQueryParams"
import {
  OPERATOR_GUEST_DEFAULT_SORT_ID,
  OPERATOR_GUEST_PAGE_SIZE,
} from "@/lib/operatorGuests/guestsPresentation"

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
    pageSize: OPERATOR_GUEST_PAGE_SIZE,
    filters: emptySelection(GUESTS_SCHEMA),
    overviewDateRange: input.overviewDateRange,
  })
  const response = await getGuests(params)
  return response.overview?.marketingEligible ?? 0
}

async function loadCampaignsSummaryFacts(input: {
  locationId: number
  overviewDateRange: CampaignsOverviewDateRange
}): Promise<CampaignsSummarySiblingFacts> {
  const window = resolveCampaignsOverviewWindow(input.overviewDateRange)
  const response = await getCampaignsSummary({
    locationId: input.locationId,
    ...(window == null
      ? {}
      : {
          overviewDateFrom: window.from.toISOString(),
          overviewDateTo: window.to.toISOString(),
        }),
  })
  const summary = response.summary
  return {
    scheduledCount: summary.campaignsInFlightScheduled,
    sendingCount: summary.campaignsInFlightSending,
    messagesSentAccepted: summary.messagesSentAccepted,
  }
}

export function CampaignsPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const [pageModule] = useState(() =>
    createOperatorCampaignsPageModule({
      loadCampaignsList: getCampaignsList,
      loadMarketingEligible: loadMarketingEligibleFromGuests,
      loadCampaignsSummary: loadCampaignsSummaryFacts,
      loadCampaignRecommendation: async ({ request }) =>
        getCampaignRecommendation(request),
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

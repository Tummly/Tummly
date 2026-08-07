import { useSyncExternalStore } from "react"

import { useCampaignsPageModuleApi } from "@/components/dashboard/operator/Campaigns/utils/campaignsPageModuleContext"
import type {
  OperatorCampaignsPageModule,
  OperatorCampaignsPageSnapshot,
} from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"

export type OperatorCampaignsPageModuleApi = {
  snapshot: OperatorCampaignsPageSnapshot
  retryLoad: OperatorCampaignsPageModule["retryLoad"]
  reloadForOverviewDateRange: OperatorCampaignsPageModule["reloadForOverviewDateRange"]
  retryRecommendation: OperatorCampaignsPageModule["retryRecommendation"]
  dismissRecommendation: OperatorCampaignsPageModule["dismissRecommendation"]
  openRecommendationAudience: OperatorCampaignsPageModule["openRecommendationAudience"]
  closeRecommendationAudience: OperatorCampaignsPageModule["closeRecommendationAudience"]
  setListView: OperatorCampaignsPageModule["setListView"]
  setSearchQuery: OperatorCampaignsPageModule["setSearchQuery"]
  clearSearchAndFilters: OperatorCampaignsPageModule["clearSearchAndFilters"]
  viewAllCampaigns: OperatorCampaignsPageModule["viewAllCampaigns"]
}

export function useCampaignsPageModule(): OperatorCampaignsPageModuleApi {
  const pageModule = useCampaignsPageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    retryLoad: pageModule.retryLoad,
    reloadForOverviewDateRange: pageModule.reloadForOverviewDateRange,
    retryRecommendation: pageModule.retryRecommendation,
    dismissRecommendation: pageModule.dismissRecommendation,
    openRecommendationAudience: pageModule.openRecommendationAudience,
    closeRecommendationAudience: pageModule.closeRecommendationAudience,
    setListView: pageModule.setListView,
    setSearchQuery: pageModule.setSearchQuery,
    clearSearchAndFilters: pageModule.clearSearchAndFilters,
    viewAllCampaigns: pageModule.viewAllCampaigns,
  }
}

export type { OperatorCampaignsPageSnapshot }

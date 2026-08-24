import { useSyncExternalStore } from "react"

import { useOfferDetailsPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offerDetailsPageModuleContext"
import type {
  OfferDetailsPageModule,
  OfferDetailsSnapshot,
} from "@/lib/operatorOffers/createOfferDetailsPageModule"

export type OfferDetailsPageModuleHookApi = {
  snapshot: OfferDetailsSnapshot
  pageModule: OfferDetailsPageModule
  retryLoad: OfferDetailsPageModule["retryLoad"]
  setActiveTab: OfferDetailsPageModule["setActiveTab"]
  setCampaignsSubTab: OfferDetailsPageModule["setCampaignsSubTab"]
  setOverviewDateRange: OfferDetailsPageModule["setOverviewDateRange"]
  retryRecommendation: OfferDetailsPageModule["retryRecommendation"]
  requestHeaderAction: OfferDetailsPageModule["requestHeaderAction"]
  confirmPendingHeaderAction: OfferDetailsPageModule["confirmPendingHeaderAction"]
  cancelPendingHeaderAction: OfferDetailsPageModule["cancelPendingHeaderAction"]
  requestClaimsRowAction: OfferDetailsPageModule["requestClaimsRowAction"]
  confirmPendingRowAction: OfferDetailsPageModule["confirmPendingRowAction"]
  cancelPendingRowAction: OfferDetailsPageModule["cancelPendingRowAction"]
}

export function useOfferDetailsPageModule(): OfferDetailsPageModuleHookApi {
  const pageModule = useOfferDetailsPageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    pageModule,
    retryLoad: pageModule.retryLoad,
    setActiveTab: pageModule.setActiveTab,
    setCampaignsSubTab: pageModule.setCampaignsSubTab,
    setOverviewDateRange: pageModule.setOverviewDateRange,
    retryRecommendation: pageModule.retryRecommendation,
    requestHeaderAction: pageModule.requestHeaderAction,
    confirmPendingHeaderAction: pageModule.confirmPendingHeaderAction,
    cancelPendingHeaderAction: pageModule.cancelPendingHeaderAction,
    requestClaimsRowAction: pageModule.requestClaimsRowAction,
    confirmPendingRowAction: pageModule.confirmPendingRowAction,
    cancelPendingRowAction: pageModule.cancelPendingRowAction,
  }
}

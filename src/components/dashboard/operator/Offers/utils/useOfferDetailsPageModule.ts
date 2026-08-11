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
  setOverviewDateRange: OfferDetailsPageModule["setOverviewDateRange"]
  requestHeaderAction: OfferDetailsPageModule["requestHeaderAction"]
  confirmPendingHeaderAction: OfferDetailsPageModule["confirmPendingHeaderAction"]
  cancelPendingHeaderAction: OfferDetailsPageModule["cancelPendingHeaderAction"]
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
    setOverviewDateRange: pageModule.setOverviewDateRange,
    requestHeaderAction: pageModule.requestHeaderAction,
    confirmPendingHeaderAction: pageModule.confirmPendingHeaderAction,
    cancelPendingHeaderAction: pageModule.cancelPendingHeaderAction,
  }
}

import { useSyncExternalStore } from "react"

import { useOffersPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import type {
  OperatorOffersPageModule,
  OperatorOffersPageSnapshot,
} from "@/lib/operatorOffers/createOperatorOffersPageModule"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import type { CampaignCatalogOfferDetailsDraft } from "@/lib/operatorOffers/offerCatalogPresentation"

export type OperatorOffersPageModuleApi = {
  snapshot: OperatorOffersPageSnapshot
  pageModule: OperatorOffersPageModule
  setPerformanceDateRange: (range: HomePerformanceDateRange) => void | Promise<void>
  openCreateOffer: () => Promise<void>
  openCreateOfferDrawer: () => void
  closeCreateOfferDrawer: () => void
  patchCreateOfferDraft: (
    patch: Partial<CampaignCatalogOfferDetailsDraft>
  ) => void
  confirmCreateOffer: () => Promise<void>
}

export function useOffersPageModule(): OperatorOffersPageModuleApi {
  const pageModule = useOffersPageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    pageModule,
    setPerformanceDateRange: pageModule.setPerformanceDateRange,
    openCreateOffer: pageModule.openCreateOffer,
    openCreateOfferDrawer: pageModule.openCreateOfferDrawer,
    closeCreateOfferDrawer: pageModule.closeCreateOfferDrawer,
    patchCreateOfferDraft: pageModule.patchCreateOfferDraft,
    confirmCreateOffer: pageModule.confirmCreateOffer,
  }
}

export type { OperatorOffersPageModule, OperatorOffersPageSnapshot }

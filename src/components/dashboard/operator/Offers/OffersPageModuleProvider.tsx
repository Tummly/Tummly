import { createElement, useState, type ReactNode } from "react"

import {
  archiveCatalogOffer,
  approveVoidRequest,
  checkStaffRedeemCode,
  createCatalogOffer,
  createVoidRequest,
  duplicateCatalogOffer,
  getCatalogOfferById,
  getOffersPerformance,
  getVoidRequest,
  listCatalogOffers,
  listOpenVoidAttention,
  markStaffRedeemed,
  notifyVoidApprovers,
  notifyVoidSubmitter,
  pauseCatalogOffer,
  rejectVoidRequest,
  resumeCatalogOffer,
  updateCatalogOffer,
} from "@/api/dashboardApi"
import { offersPageModuleContext } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import { staffRedeemModuleContext } from "@/components/dashboard/operator/Offers/utils/staffRedeemModuleContext"
import { voidRequestModuleContext } from "@/components/dashboard/operator/Offers/utils/voidRequestModuleContext"
import { createOperatorOffersPageModule } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import { createStaffRedeemModule } from "@/lib/operatorOffers/createStaffRedeemModule"
import { createVoidRequestModule } from "@/lib/operatorOffers/createVoidRequestModule"
import { createLiveStaffRedeemAdapters } from "@/lib/operatorOffers/staffRedeemAdapters"
import { createLiveVoidRequestAdapters } from "@/lib/operatorOffers/voidRequestAdapters"

export function OffersPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [voidAdapters] = useState(() =>
    createLiveVoidRequestAdapters({
      createVoidRequest,
      getVoidRequest,
      approveVoidRequest,
      rejectVoidRequest,
      notifyVoidApprovers,
      notifyVoidSubmitter,
      listOpenVoidAttention,
    })
  )
  const [pageModule] = useState(() =>
    createOperatorOffersPageModule({
      listCatalogOffers,
      listOpenVoidAttention: (locationId) =>
        voidAdapters.listOpenVoidAttention(locationId),
      getOffersPerformance: async (locationId, from, to) => {
        const response = await getOffersPerformance({ locationId, from, to })
        return {
          activeOffers: response.activeOffers,
          offersIssued: response.offersIssued,
          claims: response.claims,
          redemptions: response.redemptions,
        }
      },
      createOffer: async (body) => {
        const response = await createCatalogOffer(body)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog create failed.")
        }
        return response.offer
      },
      updateOffer: async (offerId, body) => {
        const response = await updateCatalogOffer(offerId, body)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog update failed.")
        }
        return response.offer
      },
      getOffer: async (offerId) => {
        const response = await getCatalogOfferById(offerId)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog get failed.")
        }
        return response.offer
      },
      pauseOffer: async (offerId) => {
        const response = await pauseCatalogOffer(offerId)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog pause failed.")
        }
        return response.offer
      },
      resumeOffer: async (offerId) => {
        const response = await resumeCatalogOffer(offerId)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog resume failed.")
        }
        return response.offer
      },
      archiveOffer: async (offerId) => {
        const response = await archiveCatalogOffer(offerId)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog archive failed.")
        }
        return response.offer
      },
      duplicateOffer: async (offerId) => {
        const response = await duplicateCatalogOffer(offerId)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog duplicate failed.")
        }
        return response.offer
      },
    })
  )
  const [staffRedeemModule] = useState(() =>
    createStaffRedeemModule(
      createLiveStaffRedeemAdapters({
        checkStaffRedeemCode,
        markStaffRedeemed,
      })
    )
  )
  // Live void adapters — shared instance for Needs attention void rows + dialogues.
  const [voidRequestModule] = useState(() =>
    createVoidRequestModule(voidAdapters)
  )

  return createElement(
    offersPageModuleContext.Provider,
    { value: pageModule },
    createElement(
      staffRedeemModuleContext.Provider,
      { value: staffRedeemModule },
      createElement(
        voidRequestModuleContext.Provider,
        { value: voidRequestModule },
        children
      )
    )
  )
}

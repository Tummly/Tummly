import { createElement, useState, type ReactNode } from "react"

import {
  archiveCatalogOffer,
  createCatalogOffer,
  duplicateCatalogOffer,
  getCatalogOfferById,
  getOffersPerformance,
  listCatalogOffers,
  pauseCatalogOffer,
  resumeCatalogOffer,
  updateCatalogOffer,
} from "@/api/dashboardApi"
import { offersPageModuleContext } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import { staffRedeemModuleContext } from "@/components/dashboard/operator/Offers/utils/staffRedeemModuleContext"
import { voidRequestModuleContext } from "@/components/dashboard/operator/Offers/utils/voidRequestModuleContext"
import { createOperatorOffersPageModule } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import { createStaffRedeemModule } from "@/lib/operatorOffers/createStaffRedeemModule"
import { createVoidRequestModule } from "@/lib/operatorOffers/createVoidRequestModule"
import { createStubStaffRedeemAdapters } from "@/lib/operatorOffers/staffRedeemAdapters"
import { createStubVoidRequestAdapters } from "@/lib/operatorOffers/voidRequestAdapters"

export function OffersPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [voidAdapters] = useState(() => createStubVoidRequestAdapters())
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
  // Swap stub adapters for real check/redeem APIs when those writes go live.
  const [staffRedeemModule] = useState(() =>
    createStaffRedeemModule(createStubStaffRedeemAdapters())
  )
  // Swap stub adapters for real void request / notify APIs when those writes go live.
  // Share the same stub instance so Needs attention void rows see pending creates.
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

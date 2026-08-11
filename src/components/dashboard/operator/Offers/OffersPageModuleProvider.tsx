import { createElement, useState, type ReactNode } from "react"

import {
  archiveCatalogOffer,
  createCatalogOffer,
  duplicateCatalogOffer,
  getCatalogOfferById,
  listCatalogOffers,
  pauseCatalogOffer,
  resumeCatalogOffer,
  updateCatalogOffer,
} from "@/api/dashboardApi"
import { offersPageModuleContext } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import { staffRedeemModuleContext } from "@/components/dashboard/operator/Offers/utils/staffRedeemModuleContext"
import { createOperatorOffersPageModule } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import { createStaffRedeemModule } from "@/lib/operatorOffers/createStaffRedeemModule"
import { createStubStaffRedeemAdapters } from "@/lib/operatorOffers/staffRedeemAdapters"

export function OffersPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [pageModule] = useState(() =>
    createOperatorOffersPageModule({
      listCatalogOffers,
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

  return createElement(
    offersPageModuleContext.Provider,
    { value: pageModule },
    createElement(
      staffRedeemModuleContext.Provider,
      { value: staffRedeemModule },
      children
    )
  )
}

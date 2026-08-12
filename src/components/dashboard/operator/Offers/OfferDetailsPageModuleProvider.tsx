import { createElement, useRef, useState, type ReactNode } from "react"
import { useNavigate, useOutletContext } from "react-router-dom"

import {
  archiveCatalogOffer,
  duplicateCatalogOffer,
  getCatalogOfferById,
  getOfferClaims,
  getOfferIssuanceSources,
  getOfferLinkedCampaigns,
  getOfferMetrics,
  getOfferRedemptions,
  getOfferVoidRequests,
  pauseCatalogOffer,
  resumeCatalogOffer,
} from "@/api/dashboardApi"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { offerDetailsPageModuleContext } from "@/components/dashboard/operator/Offers/utils/offerDetailsPageModuleContext"
import { operatorDashboardOfferDetailsPath } from "@/lib/operatorHome/operatorDashboardPaths"
import { createOfferDetailsPageModule } from "@/lib/operatorOffers/createOfferDetailsPageModule"
import {
  loadOfferDetailsClaims,
  loadOfferDetailsIssuanceSources,
  loadOfferDetailsLinkedCampaigns,
  loadOfferDetailsRedemptions,
  loadOfferDetailsVoidRequests,
} from "@/lib/operatorOffers/offerDetailsLifecycleQuery"
import { loadOfferDetailsOverviewMetrics } from "@/lib/operatorOffers/offerDetailsMetricsQuery"

export function OfferDetailsPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const navigate = useNavigate()
  const { mode, selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const navigateRef = useRef(navigate)
  const modeRef = useRef(mode)
  const locationIdRef = useRef(selectedLocationId ?? locations[0]?.id ?? null)
  navigateRef.current = navigate
  modeRef.current = mode
  locationIdRef.current = selectedLocationId ?? locations[0]?.id ?? null

  const [pageModule] = useState(() =>
    createOfferDetailsPageModule({
      getOffer: async (offerId) => {
        const response = await getCatalogOfferById(offerId)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog get failed.")
        }
        return response.offer
      },
      getOfferMetrics: (offerId, range) =>
        loadOfferDetailsOverviewMetrics(offerId, range, {
          fetchMetrics: getOfferMetrics,
        }),
      getClaims: (offerId) =>
        loadOfferDetailsClaims(offerId, { fetchClaims: getOfferClaims }),
      getRedemptions: (offerId) =>
        loadOfferDetailsRedemptions(offerId, {
          fetchRedemptions: getOfferRedemptions,
        }),
      getLinkedCampaigns: (offerId) =>
        loadOfferDetailsLinkedCampaigns(offerId, {
          fetchLinkedCampaigns: getOfferLinkedCampaigns,
        }),
      getIssuanceSources: (offerId) =>
        loadOfferDetailsIssuanceSources(offerId, {
          fetchIssuanceSources: getOfferIssuanceSources,
        }),
      getVoidRequests: (offerId) =>
        loadOfferDetailsVoidRequests(offerId, {
          fetchVoidRequests: getOfferVoidRequests,
        }),
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
      onDuplicated: (newOfferId) => {
        const locationId = locationIdRef.current
        if (locationId == null) {
          return
        }
        void navigateRef.current(
          operatorDashboardOfferDetailsPath(
            modeRef.current,
            newOfferId,
            locationId
          )
        )
      },
    })
  )

  return createElement(
    offerDetailsPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

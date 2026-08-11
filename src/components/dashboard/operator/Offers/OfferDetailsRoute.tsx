import { useEffect, useMemo, useRef } from "react"
import { useOutletContext, useParams } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { OfferDetailsPage } from "@/components/dashboard/operator/Offers/OfferDetailsPage"
import { useOfferDetailsPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offerDetailsPageModuleContext"
import { useOffersPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import {
  operatorDashboardCampaignsPathWithOffer,
  operatorDashboardNavPath,
} from "@/lib/operatorHome/operatorDashboardPaths"

function parseOfferRouteId(raw: string | undefined): number | null {
  if (raw == null || raw.trim() === "") {
    return null
  }
  const parsed = Number(raw)
  if (!Number.isInteger(parsed) || parsed <= 0) {
    return null
  }
  return parsed
}

export function OfferDetailsRoute() {
  const { offerId: offerIdParam } = useParams<{ offerId: string }>()
  const { selectedLocationId, locations, mode } =
    useOutletContext<DashboardOutletContext>()
  const pageModule = useOfferDetailsPageModuleApi()
  const offersPageModule = useOffersPageModuleApi()
  const syncRef = useRef(pageModule.syncWorkspace)
  const syncOffersRef = useRef(offersPageModule.syncWorkspace)
  syncRef.current = pageModule.syncWorkspace
  syncOffersRef.current = offersPageModule.syncWorkspace

  const offerId = parseOfferRouteId(offerIdParam)

  useEffect(() => {
    void syncRef.current({
      selectedLocationId,
      locations: locations.map((location) => ({
        id: location.id,
        locationName: location.locationName,
      })),
      offerId,
    })
  }, [offerId, selectedLocationId, locations])

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }
    // Keep list Create/Edit drawer module hydrated so Details Edit works.
    void syncOffersRef.current({
      selectedLocationId,
      locations: locations.map((location) => ({
        id: location.id,
        locationName: location.locationName,
      })),
    })
  }, [selectedLocationId, locations])

  const offersHref = useMemo(() => {
    const locationId = selectedLocationId ?? locations[0]?.id ?? 0
    return operatorDashboardNavPath(mode, "offers", locationId)
  }, [locations, mode, selectedLocationId])

  const shareOfferInCampaignHref = useMemo(() => {
    const locationId = selectedLocationId ?? locations[0]?.id ?? 0
    const catalogOfferId = offerId ?? 0
    return operatorDashboardCampaignsPathWithOffer(
      mode,
      locationId,
      catalogOfferId
    )
  }, [locations, mode, offerId, selectedLocationId])

  return (
    <OfferDetailsPage
      offersHref={offersHref}
      shareOfferInCampaignHref={shareOfferInCampaignHref}
      mode={mode}
    />
  )
}

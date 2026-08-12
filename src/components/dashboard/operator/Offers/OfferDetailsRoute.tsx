import { useEffect, useMemo, useRef } from "react"
import { useOutletContext, useParams, useSearchParams } from "react-router-dom"

import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { OfferDetailsPage } from "@/components/dashboard/operator/Offers/OfferDetailsPage"
import { useOfferDetailsPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offerDetailsPageModuleContext"
import { useOffersPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import {
  operatorDashboardCampaignsPathWithOffer,
  operatorDashboardNavPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import {
  OFFER_DETAILS_TAB_IDS,
  type OfferDetailsTabId,
} from "@/lib/operatorOffers/offerDetailsPresentation"

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

function parseOfferDetailsTab(
  raw: string | null
): OfferDetailsTabId | undefined {
  if (raw == null) {
    return undefined
  }
  return (OFFER_DETAILS_TAB_IDS as readonly string[]).includes(raw)
    ? (raw as OfferDetailsTabId)
    : undefined
}

export function OfferDetailsRoute() {
  const { offerId: offerIdParam } = useParams<{ offerId: string }>()
  const [searchParams] = useSearchParams()
  const { selectedLocationId, locations, mode } =
    useOutletContext<DashboardOutletContext>()
  const pageModule = useOfferDetailsPageModuleApi()
  const offersPageModule = useOffersPageModuleApi()
  const syncRef = useRef(pageModule.syncWorkspace)
  const syncOffersRef = useRef(offersPageModule.syncWorkspace)
  syncRef.current = pageModule.syncWorkspace
  syncOffersRef.current = offersPageModule.syncWorkspace

  const offerId = parseOfferRouteId(offerIdParam)
  const initialTabId = parseOfferDetailsTab(searchParams.get("tab"))

  useEffect(() => {
    void syncRef.current({
      selectedLocationId,
      locations: locations.map((location) => ({
        id: location.id,
        locationName: location.locationName,
      })),
      offerId,
      initialTabId,
    })
  }, [offerId, selectedLocationId, locations, initialTabId])

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

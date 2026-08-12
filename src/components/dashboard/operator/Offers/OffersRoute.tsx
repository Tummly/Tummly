import { useEffect, useRef } from "react"
import { useOutletContext } from "react-router-dom"

import { OffersPage } from "@/components/dashboard/operator/Offers/OffersPage"
import { useOffersPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function OffersRoute() {
  const { selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const offersPageModule = useOffersPageModuleApi()
  const syncOffersRef = useRef(offersPageModule.syncWorkspace)

  syncOffersRef.current = offersPageModule.syncWorkspace

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncOffersRef.current({
      selectedLocationId,
      locations: locations.map((location) => ({
        id: location.id,
        locationName: location.locationName,
      })),
    })
  }, [selectedLocationId, locations])

  return <OffersPage />
}

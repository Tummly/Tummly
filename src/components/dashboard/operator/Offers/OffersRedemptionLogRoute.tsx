import { useEffect, useMemo, useRef } from "react"
import { useOutletContext } from "react-router-dom"

import { OffersRedemptionLogPage } from "@/components/dashboard/operator/Offers/OffersRedemptionLogPage"
import { useOffersRedemptionLogPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offersRedemptionLogPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { operatorDashboardNavPath } from "@/lib/operatorHome/operatorDashboardPaths"

export function OffersRedemptionLogRoute() {
  const { selectedLocationId, locations, mode } =
    useOutletContext<DashboardOutletContext>()
  const pageModule = useOffersRedemptionLogPageModuleApi()
  const syncRef = useRef(pageModule.syncWorkspace)

  syncRef.current = pageModule.syncWorkspace

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncRef.current({
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

  return <OffersRedemptionLogPage offersHref={offersHref} />
}

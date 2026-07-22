import { useEffect, useRef } from "react"
import { useOutletContext } from "react-router-dom"

import { GuestsPage } from "@/components/dashboard/operator/Guests/GuestsPage"
import { useGuestsPageModuleApi } from "@/components/dashboard/operator/Guests/utils/guestsPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function GuestsRoute() {
  const { selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const guestsPageModule = useGuestsPageModuleApi()
  const syncGuestsRef = useRef(guestsPageModule.syncWorkspace)

  syncGuestsRef.current = guestsPageModule.syncWorkspace

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncGuestsRef.current({
      selectedLocationId,
      locations: locations.map((location) => ({
        id: location.id,
        locationName: location.locationName,
      })),
    })
  }, [selectedLocationId, locations])

  return <GuestsPage />
}

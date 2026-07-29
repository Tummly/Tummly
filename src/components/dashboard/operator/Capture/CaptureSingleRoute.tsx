import { useEffect, useRef } from "react"
import { useOutletContext } from "react-router-dom"

import { CapturePage } from "@/components/dashboard/operator/Capture/CapturePage"
import { useCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function CaptureSingleRoute() {
  const { selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const capturePageModule = useCapturePageModuleApi()
  const syncRef = useRef(capturePageModule.syncWorkspace)
  syncRef.current = capturePageModule.syncWorkspace

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncRef.current({
      selectedLocationId,
      locations: locations.map((location) => ({
        id: location.id,
        locationName: location.locationName,
        address: location.address,
      })),
    })
  }, [selectedLocationId, locations])

  return <CapturePage />
}

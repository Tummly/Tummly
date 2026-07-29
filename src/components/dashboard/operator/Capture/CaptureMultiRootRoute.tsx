import { useEffect, useRef } from "react"
import { useOutletContext } from "react-router-dom"

import { CaptureMultiRootPage } from "@/components/dashboard/operator/Capture/CaptureMultiRootPage"
import { MultiCapturePageModuleProvider } from "@/components/dashboard/operator/Capture/MultiCapturePageModuleProvider"
import { useMultiCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/multiCapturePageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

function CaptureMultiRootRouteContent() {
  const { locations } = useOutletContext<DashboardOutletContext>()
  const multiCapturePageModule = useMultiCapturePageModuleApi()
  const syncRef = useRef(multiCapturePageModule.syncWorkspace)
  syncRef.current = multiCapturePageModule.syncWorkspace

  useEffect(() => {
    void syncRef.current({
      locations: locations.map((location) => ({
        id: location.id,
        locationName: location.locationName,
        address: location.address,
      })),
    })
  }, [locations])

  return <CaptureMultiRootPage />
}

/** Multi Capture root route — own page module provider + workspace sync. */
export function CaptureMultiRootRoute() {
  return (
    <MultiCapturePageModuleProvider>
      <CaptureMultiRootRouteContent />
    </MultiCapturePageModuleProvider>
  )
}

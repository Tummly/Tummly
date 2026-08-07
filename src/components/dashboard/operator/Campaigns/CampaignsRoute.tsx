import { useEffect, useRef } from "react"
import { useOutletContext } from "react-router-dom"

import { CampaignsPage } from "@/components/dashboard/operator/Campaigns/CampaignsPage"
import { useCampaignsPageModuleApi } from "@/components/dashboard/operator/Campaigns/utils/campaignsPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function CampaignsRoute() {
  const { selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const campaignsPageModule = useCampaignsPageModuleApi()
  const syncCampaignsRef = useRef(campaignsPageModule.syncWorkspace)

  syncCampaignsRef.current = campaignsPageModule.syncWorkspace

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncCampaignsRef.current({
      selectedLocationId,
      locations: locations.map((location) => ({
        id: location.id,
        locationName: location.locationName,
      })),
    })
  }, [selectedLocationId, locations])

  return <CampaignsPage />
}

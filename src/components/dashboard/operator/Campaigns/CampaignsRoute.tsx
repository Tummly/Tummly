import { useEffect, useRef } from "react"
import {
  useLocation,
  useNavigate,
  useOutletContext,
  useSearchParams,
} from "react-router-dom"

import { CampaignsPage } from "@/components/dashboard/operator/Campaigns/CampaignsPage"
import { useCampaignsPageModuleApi } from "@/components/dashboard/operator/Campaigns/utils/campaignsPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import {
  readGlobalSearchQueryParam,
  readGlobalSearchScopeParam,
  stripGlobalSearchListParams,
  withAllLocationsFilter,
} from "@/lib/operatorGlobalSearch/applySearchQueryFromParam"

export function CampaignsRoute() {
  const { selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const campaignsPageModule = useCampaignsPageModuleApi()
  const syncCampaignsRef = useRef(campaignsPageModule.syncWorkspace)
  const location = useLocation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const consumedQueryRef = useRef<string | null>(null)

  syncCampaignsRef.current = campaignsPageModule.syncWorkspace

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncCampaignsRef.current({
      selectedLocationId,
      locations: locations.map((locationRow) => ({
        id: locationRow.id,
        locationName: locationRow.locationName,
      })),
    })
  }, [selectedLocationId, locations])

  useEffect(() => {
    const q = readGlobalSearchQueryParam(searchParams)
    if (q == null) {
      return
    }
    const key = `${q}:${location.key}`
    if (consumedQueryRef.current === key) {
      return
    }
    consumedQueryRef.current = key
    campaignsPageModule.setSearchQuery(q)
    if (readGlobalSearchScopeParam(searchParams) === "all") {
      campaignsPageModule.applyFilters(
        withAllLocationsFilter(
          campaignsPageModule.getSnapshot().appliedFilters
        )
      )
    }
    const nextParams = stripGlobalSearchListParams(searchParams)
    const nextSearch = nextParams.toString()
    navigate(
      nextSearch === "" ? location.pathname : `${location.pathname}?${nextSearch}`,
      { replace: true }
    )
  }, [
    campaignsPageModule,
    location.key,
    location.pathname,
    navigate,
    searchParams,
  ])

  return <CampaignsPage />
}

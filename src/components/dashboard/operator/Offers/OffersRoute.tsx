import { useEffect, useRef } from "react"
import {
  useLocation,
  useNavigate,
  useOutletContext,
  useSearchParams,
} from "react-router-dom"

import { OffersPage } from "@/components/dashboard/operator/Offers/OffersPage"
import { useOffersPageModuleApi } from "@/components/dashboard/operator/Offers/utils/offersPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import {
  readGlobalSearchQueryParam,
  stripGlobalSearchListParams,
} from "@/lib/operatorGlobalSearch/applySearchQueryFromParam"

export function OffersRoute() {
  const { selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const offersPageModule = useOffersPageModuleApi()
  const syncOffersRef = useRef(offersPageModule.syncWorkspace)
  const location = useLocation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const consumedQueryRef = useRef<string | null>(null)

  syncOffersRef.current = offersPageModule.syncWorkspace

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncOffersRef.current({
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
    offersPageModule.setSearchQuery(q)
    const nextParams = stripGlobalSearchListParams(searchParams)
    const nextSearch = nextParams.toString()
    navigate(
      nextSearch === "" ? location.pathname : `${location.pathname}?${nextSearch}`,
      { replace: true }
    )
  }, [offersPageModule, location.key, location.pathname, navigate, searchParams])

  return <OffersPage />
}

import { useEffect, useRef } from "react"
import {
  useLocation,
  useNavigate,
  useOutletContext,
  useSearchParams,
} from "react-router-dom"

import { GuestsPage } from "@/components/dashboard/operator/Guests/GuestsPage"
import { useGuestsPageModuleApi } from "@/components/dashboard/operator/Guests/utils/guestsPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import {
  readGlobalSearchQueryParam,
  stripGlobalSearchListParams,
} from "@/lib/operatorGlobalSearch/applySearchQueryFromParam"

export function GuestsRoute() {
  const { selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const guestsPageModule = useGuestsPageModuleApi()
  const syncGuestsRef = useRef(guestsPageModule.syncWorkspace)
  const location = useLocation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const consumedQueryRef = useRef<string | null>(null)

  syncGuestsRef.current = guestsPageModule.syncWorkspace

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncGuestsRef.current({
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
    guestsPageModule.setSearchQuery(q)
    const nextParams = stripGlobalSearchListParams(searchParams)
    const nextSearch = nextParams.toString()
    navigate(
      nextSearch === "" ? location.pathname : `${location.pathname}?${nextSearch}`,
      { replace: true }
    )
  }, [guestsPageModule, location.key, location.pathname, navigate, searchParams])

  return <GuestsPage />
}

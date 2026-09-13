import { useEffect, useRef } from "react"
import {
  useLocation,
  useNavigate,
  useOutletContext,
  useSearchParams,
} from "react-router-dom"

import { CaptureMultiRootPage } from "@/components/dashboard/operator/Capture/CaptureMultiRootPage"
import { MultiCapturePageModuleProvider } from "@/components/dashboard/operator/Capture/MultiCapturePageModuleProvider"
import { useMultiCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/multiCapturePageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import {
  readGlobalSearchQueryParam,
  readGlobalSearchScopeParam,
  stripGlobalSearchListParams,
  withAllLocationsFilter,
} from "@/lib/operatorGlobalSearch/applySearchQueryFromParam"

function CaptureMultiRootRouteContent() {
  const { locations } = useOutletContext<DashboardOutletContext>()
  const multiCapturePageModule = useMultiCapturePageModuleApi()
  const syncRef = useRef(multiCapturePageModule.syncWorkspace)
  syncRef.current = multiCapturePageModule.syncWorkspace
  const location = useLocation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const consumedQueryRef = useRef<string | null>(null)

  useEffect(() => {
    void syncRef.current({
      locations: locations.map((locationRow) => ({
        id: locationRow.id,
        locationName: locationRow.locationName,
        address: locationRow.address,
      })),
    })
  }, [locations])

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
    multiCapturePageModule.setSearchQuery(q)
    if (readGlobalSearchScopeParam(searchParams) === "all") {
      multiCapturePageModule.applyFilters(
        withAllLocationsFilter(
          multiCapturePageModule.getSnapshot().appliedFilters
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
    location.key,
    location.pathname,
    multiCapturePageModule,
    navigate,
    searchParams,
  ])

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


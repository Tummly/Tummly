import {
  createElement,
  useEffect,
  useMemo,
  type ReactNode,
} from "react"
import { useOutletContext, useParams, useSearchParams } from "react-router-dom"

import { getLocationDetail, mutateLocationLifecycle } from "@/api/dashboardApi"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { locationDetailPageModuleContext } from "@/components/dashboard/operator/Locations/utils/locationDetailPageModuleContext"
import { createOperatorLocationDetailPageModule } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"

export function LocationDetailPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const { locationId: rawLocationId } = useParams()
  const [searchParams] = useSearchParams()
  const { locations, mode } = useOutletContext<DashboardOutletContext>()
  const locationId = Number.parseInt(rawLocationId ?? "", 10)
  const safeLocationId = Number.isFinite(locationId) ? locationId : -1
  const fallback = locations.find((location) => location.id === safeLocationId)

  const pageModule = useMemo(
    () =>
      createOperatorLocationDetailPageModule(
        safeLocationId,
        {
          getDetail: (id) => getLocationDetail(id),
          mutateLifecycle: async (id, action) => {
            await mutateLocationLifecycle(id, action)
          },
        },
        {
          initialTabId: searchParams.get("tab"),
          fallbackName: fallback?.locationName,
          dashboardMode: mode,
        }
      ),
    // Recreate only when the path location changes; tab sync is separate.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fallback name is best-effort on create
    [safeLocationId, mode]
  )

  useEffect(() => {
    void pageModule.load()
  }, [pageModule])

  useEffect(() => {
    pageModule.setActiveTabFromUrl(searchParams.get("tab"))
  }, [pageModule, searchParams])

  return createElement(
    locationDetailPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

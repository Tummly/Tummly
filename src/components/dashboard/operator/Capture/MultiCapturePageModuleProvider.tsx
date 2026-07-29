import { createElement, useRef, useState, type ReactNode } from "react"
import { useNavigate, useOutletContext } from "react-router-dom"
import { toast } from "sonner"

import { getCaptureLocations, getCaptureOverview } from "@/api/dashboardApi"
import { multiCapturePageModuleContext } from "@/components/dashboard/operator/Capture/utils/multiCapturePageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { createOperatorMultiCapturePageModule } from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"
import { operatorDashboardCaptureLocationPath } from "@/lib/operatorHome/operatorDashboardPaths"

export function MultiCapturePageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const navigate = useNavigate()
  const { selectLocation } = useOutletContext<DashboardOutletContext>()
  const selectLocationRef = useRef(selectLocation)
  selectLocationRef.current = selectLocation
  const navigateRef = useRef(navigate)
  navigateRef.current = navigate

  const [pageModule] = useState(() =>
    createOperatorMultiCapturePageModule({
      getCaptureOverview,
      getCaptureLocations,
      getMultiCaptureOverviewDateRange: () =>
        dashboardUiStore.getState().multiCaptureOverviewDateRange,
      syncSelectedLocation: (locationId) => {
        selectLocationRef.current(locationId)
      },
      navigateToCaptureLocation: (locationId) => {
        navigateRef.current(operatorDashboardCaptureLocationPath(locationId))
      },
      onOverviewLoadError: (message) => {
        toast.error(message)
      },
      onLocationsLoadError: (message) => {
        toast.error(message)
      },
    })
  )

  return createElement(
    multiCapturePageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

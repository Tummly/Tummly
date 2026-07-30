import { createElement, useRef, useState, type ReactNode } from "react"
import { useNavigate, useOutletContext } from "react-router-dom"
import { toast } from "sonner"

import {
  getCaptureLocations,
  getCaptureOverview,
  getCapturePreviewOptions,
  pauseCaptureLocation,
  activateCaptureLocation,
} from "@/api/dashboardApi"
import { multiCapturePageModuleContext } from "@/components/dashboard/operator/Capture/utils/multiCapturePageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { buildCaptureLocationHandoffState } from "@/lib/operatorCapture/captureLocationHandoff"
import { createDigitalGuestLinkAdapters } from "@/lib/operatorCapture/createDigitalGuestLinkAdapters"
import {
  createOperatorMultiCapturePageModule,
} from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"
import { operatorDashboardCaptureLocationPath } from "@/lib/operatorHome/operatorDashboardPaths"
import { useAuthStore } from "@/stores/authStore"

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
      getCapturePreviewOptions,
      ...createDigitalGuestLinkAdapters,
      pauseLocationCapture: async (locationId) => {
        try {
          const response = await pauseCaptureLocation(locationId)
          return {
            ok: true as const,
            status: response.status,
            pauseRestoreQrCodeCount: response.pauseRestoreQrCodeCount,
          }
        } catch {
          return {
            ok: false as const,
            message: "Could not pause location capture. Please try again.",
          }
        }
      },
      activateLocationCapture: async (locationId) => {
        try {
          const response = await activateCaptureLocation(locationId)
          return {
            ok: true as const,
            status: response.status,
            pauseRestoreQrCodeCount: response.pauseRestoreQrCodeCount,
          }
        } catch {
          return {
            ok: false as const,
            message: "Could not activate location capture. Please try again.",
          }
        }
      },
      getMultiCaptureOverviewDateRange: () =>
        dashboardUiStore.getState().multiCaptureOverviewDateRange,
      syncSelectedLocation: (locationId) => {
        selectLocationRef.current(locationId)
      },
      navigateToCaptureLocation: (locationId, options) => {
        const path = operatorDashboardCaptureLocationPath(locationId)
        if (options?.openPlacementDetailQrCodeId != null) {
          navigateRef.current(path, {
            state: buildCaptureLocationHandoffState(
              options.openPlacementDetailQrCodeId
            ),
          })
          return
        }
        navigateRef.current(path)
      },
      canManageLocationCapture: () => useAuthStore.getState().role === "USER",
      onOverviewLoadError: (message) => {
        toast.error(message)
      },
      onLocationsLoadError: (message) => {
        toast.error(message)
      },
      onLocationCaptureError: (message) => {
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

import { createElement, useRef, useState, type ReactNode } from "react"
import { isAxiosError } from "axios"
import { useNavigate, useOutletContext } from "react-router-dom"
import { toast } from "sonner"

import {
  createDigitalGuestLink as createDigitalGuestLinkApi,
  getCaptureLocations,
  getCaptureOverview,
  getCaptureLocationSnapshot,
  pauseCaptureLocation,
  activateCaptureLocation,
} from "@/api/dashboardApi"
import { multiCapturePageModuleContext } from "@/components/dashboard/operator/Capture/utils/multiCapturePageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { buildCaptureLocationHandoffState } from "@/lib/operatorCapture/captureLocationHandoff"
import {
  OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import {
  createOperatorMultiCapturePageModule,
} from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"
import { operatorDashboardCaptureLocationPath } from "@/lib/operatorHome/operatorDashboardPaths"
import { useAuthStore } from "@/stores/authStore"
import type {
  CreateDigitalGuestLinkAdapterResult,
  CreateDigitalGuestLinkModuleInput,
} from "@/lib/operatorCapture/createOperatorCapturePageModule"
import type {
  CreateDigitalGuestLinkErrorBody,
} from "@/types/dashboard"

async function createDigitalGuestLink(
  locationId: number,
  input: CreateDigitalGuestLinkModuleInput
): Promise<CreateDigitalGuestLinkAdapterResult> {
  try {
    const response = await createDigitalGuestLinkApi(locationId, input)
    return { ok: true, qrCodeId: response.qrCodeId }
  } catch (error) {
    if (isAxiosError(error) && error.response?.status === 409) {
      const body = error.response.data as CreateDigitalGuestLinkErrorBody | undefined
      return {
        ok: false,
        reason: "duplicate_link_name",
        message:
          body?.message
          ?? OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY.linkNameDuplicate,
      }
    }
    return {
      ok: false,
      reason: "failed",
      message: OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY.failureToast,
    }
  }
}

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
      getCaptureLocationSnapshot,
      createDigitalGuestLink,
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
      onCreateDigitalGuestLinkError: (message) => {
        toast.error(message)
      },
      onDigitalGuestLinkCreated: (message) => {
        toast.success(message)
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

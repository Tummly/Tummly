import { createElement, useState, type ReactNode } from "react"
import { isAxiosError } from "axios"
import { toast } from "sonner"

import {
  createDigitalGuestLink as createDigitalGuestLinkApi,
  getCapturePerformance,
  getCapturePlacements,
  pauseCapturePlacement,
  resumeCapturePlacement,
  rotateCapturePlacement,
} from "@/api/dashboardApi"
import { capturePageModuleContext } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import {
  createOperatorCapturePageModule,
  type CreateDigitalGuestLinkAdapterResult,
  type CreateDigitalGuestLinkModuleInput,
} from "@/lib/operatorCapture/createOperatorCapturePageModule"
import {
  OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { CreateDigitalGuestLinkErrorBody } from "@/types/dashboard"

async function copyText(
  text: string
): Promise<{ ok: true } | { ok: false; error: string }> {
  try {
    await navigator.clipboard.writeText(text)
    return { ok: true }
  } catch {
    return {
      ok: false,
      error: "Could not copy link. Please try again.",
    }
  }
}

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

export function CapturePageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const [pageModule] = useState(() =>
    createOperatorCapturePageModule({
      getCapturePerformance,
      getCapturePlacements,
      pauseCapturePlacement,
      resumeCapturePlacement,
      rotateCapturePlacement,
      createDigitalGuestLink,
      copyText,
      getCapturePerformanceDateRange: () =>
        dashboardUiStore.getState().capturePerformanceDateRange,
      onPerformanceLoadError: (message) => {
        toast.error(message)
      },
      onPlacementsLoadError: (message) => {
        toast.error(message)
      },
      onPlacementActionError: (message) => {
        toast.error(message)
      },
      onCopyPlacementLinkError: (message) => {
        toast.error(message)
      },
      onCreateDigitalGuestLinkError: (message) => {
        toast.error(message)
      },
    })
  )

  return createElement(
    capturePageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

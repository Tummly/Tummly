import { createElement, useState, type ReactNode } from "react"
import { isAxiosError } from "axios"
import { toast } from "sonner"

import {
  archiveCapturePlacement as archiveCapturePlacementApi,
  createDigitalGuestLink as createDigitalGuestLinkApi,
  getArchivedCapturePlacements,
  getCapturePerformance,
  getCapturePlacements,
  pauseCapturePlacement,
  restoreCapturePlacement as restoreCapturePlacementApi,
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
import type {
  CapturePlacementRestoreErrorBody,
  CreateDigitalGuestLinkErrorBody,
} from "@/types/dashboard"

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

async function restoreCapturePlacement(
  locationId: number,
  qrCodeId: number
): Promise<
  | { ok: true; qrCodeId: number; status: "Paused"; qrLinkUrl: string }
  | { ok: false; reason: "conflict" | "failed"; message: string }
> {
  try {
    const response = await restoreCapturePlacementApi(locationId, qrCodeId)
    return {
      ok: true,
      qrCodeId: response.qrCodeId,
      status: response.status,
      qrLinkUrl: response.qrLinkUrl,
    }
  } catch (error) {
    if (isAxiosError(error) && error.response?.status === 409) {
      const body = error.response.data as
        | CapturePlacementRestoreErrorBody
        | undefined
      return {
        ok: false,
        reason: "conflict",
        message:
          body?.message
          ?? "This QR code cannot be restored because its type or link name is already in use.",
      }
    }
    return {
      ok: false,
      reason: "failed",
      message: "Could not restore QR code. Please try again.",
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
      getArchivedCapturePlacements,
      pauseCapturePlacement,
      resumeCapturePlacement,
      rotateCapturePlacement,
      archiveCapturePlacement: async (locationId, qrCodeId) => {
        const response = await archiveCapturePlacementApi(locationId, qrCodeId)
        return {
          qrCodeId: response.qrCodeId,
          status: response.status,
          archivedAt: response.archivedAt,
          archivedByDisplayName: response.archivedByDisplayName,
        }
      },
      restoreCapturePlacement,
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
      onArchiveLoadError: (message) => {
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

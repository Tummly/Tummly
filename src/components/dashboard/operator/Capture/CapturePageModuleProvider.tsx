import { createElement, useState, type ReactNode } from "react"
import { isAxiosError } from "axios"
import { toast } from "sonner"

import {
  archiveCapturePlacement as archiveCapturePlacementApi,
  createCatalogOffer,
  getArchivedCapturePlacements,
  getCaptureLocationSnapshot,
  listCatalogOffers,
  pauseCapturePlacement,
  putCaptureThankYouOffer,
  restoreCapturePlacement as restoreCapturePlacementApi,
  resumeCapturePlacement,
  rotateCapturePlacement,
  updateCapturePlacementInternalDescription,
} from "@/api/dashboardApi"
import { capturePageModuleContext } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { createDigitalGuestLinkAdapters } from "@/lib/operatorCapture/createDigitalGuestLinkAdapters"
import { createOperatorCapturePageModule } from "@/lib/operatorCapture/createOperatorCapturePageModule"
import type { CapturePlacementRestoreErrorBody } from "@/types/dashboard"

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
      getCaptureLocationSnapshot,
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
      createDigitalGuestLink:
        createDigitalGuestLinkAdapters.createDigitalGuestLink,
      updatePlacementInternalDescription: async (
        locationId,
        qrCodeId,
        internalDescription
      ) => {
        const response = await updateCapturePlacementInternalDescription(
          locationId,
          qrCodeId,
          internalDescription
        )
        return {
          qrCodeId: response.qrCodeId,
          internalDescription: response.internalDescription,
          updatedAt: response.updatedAt,
          updatedByDisplayName: response.updatedByDisplayName,
        }
      },
      createCatalogOffer: async (body) => {
        const response = await createCatalogOffer(body)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog create failed.")
        }
        return { id: response.offer.id, title: response.offer.title }
      },
      listCatalogOffers: async (params) => {
        const response = await listCatalogOffers(params)
        return { items: response.items }
      },
      putCaptureThankYouOffer: async (locationId, offerId) => {
        const response = await putCaptureThankYouOffer(locationId, offerId)
        return {
          thankYouOfferId: response.thankYouOfferId,
          thankYouOfferTitle: response.thankYouOfferTitle,
          thankYouOfferLive: response.thankYouOfferLive,
        }
      },
      copyText,
      getCapturePerformanceDateRange: () =>
        dashboardUiStore.getState().capturePerformanceDateRange,
      onCaptureLoadError: (message) => {
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
      onCreateDigitalGuestLinkError:
        createDigitalGuestLinkAdapters.onCreateDigitalGuestLinkError,
      onThankYouOfferError: (message) => {
        toast.error(message)
      },
      onThankYouOfferSuccess: (message) => {
        toast.success(message)
      },
    })
  )

  return createElement(
    capturePageModuleContext.Provider,
    { value: pageModule },
    children
  )
}

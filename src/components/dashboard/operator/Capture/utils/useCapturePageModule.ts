import { useSyncExternalStore } from "react"
import { toast } from "sonner"

import { useCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import type {
  OperatorCapturePageModule,
  OperatorCapturePageSnapshot,
} from "@/lib/operatorCapture/createOperatorCapturePageModule"

export type OperatorCapturePageModuleApi = {
  snapshot: OperatorCapturePageSnapshot
  syncWorkspace: OperatorCapturePageModule["syncWorkspace"]
  retryLoad: OperatorCapturePageModule["retryLoad"]
  reloadForCapturePerformanceDateRange: OperatorCapturePageModule["reloadForCapturePerformanceDateRange"]
  pausePlacement: (qrCodeId: number) => void
  resumePlacement: (qrCodeId: number) => void
  copyPlacementLink: (qrCodeId: number) => void
  openGuestExperiencePreview: () => void
  closeGuestExperiencePreview: () => void
}

export function useCapturePageModule(): OperatorCapturePageModuleApi {
  const pageModule = useCapturePageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    syncWorkspace: pageModule.syncWorkspace,
    retryLoad: pageModule.retryLoad,
    reloadForCapturePerformanceDateRange:
      pageModule.reloadForCapturePerformanceDateRange,
    pausePlacement: (qrCodeId) => {
      void pageModule.pausePlacement(qrCodeId).then((result) => {
        if (result === "paused") {
          toast.success("Paused")
        }
      })
    },
    resumePlacement: (qrCodeId) => {
      void pageModule.resumePlacement(qrCodeId).then((result) => {
        if (result === "resumed") {
          toast.success("Resumed")
        }
      })
    },
    copyPlacementLink: (qrCodeId) => {
      void pageModule.copyPlacementLink(qrCodeId).then((result) => {
        if (result === "copied") {
          toast.success("Link copied")
        }
      })
    },
    openGuestExperiencePreview: pageModule.openGuestExperiencePreview,
    closeGuestExperiencePreview: pageModule.closeGuestExperiencePreview,
  }
}

import { useSyncExternalStore } from "react"
import { toast } from "sonner"

import { useCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import { CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS } from "@/lib/operatorCapture/capturePresentation"
import type {
  CapturePlacementDetailModule,
  PlacementDetailDrawerSnapshot,
} from "@/lib/operatorCapture/createCapturePlacementDetailModule"
import type { OperatorCapturePageModule } from "@/lib/operatorCapture/createOperatorCapturePageModule"

export type CapturePlacementDetailModuleApi = {
  snapshot: PlacementDetailDrawerSnapshot
  closePlacementDetail: CapturePlacementDetailModule["close"]
  setPlacementDetailDescriptionDraft: CapturePlacementDetailModule["setDescriptionDraft"]
  savePlacementDetailDescription: () => Promise<"saved" | "failed" | "noop">
  requestPlacementDetailPause: OperatorCapturePageModule["requestPlacementDetailPause"]
  requestPlacementDetailActivate: OperatorCapturePageModule["requestPlacementDetailActivate"]
  requestPlacementDetailRotate: OperatorCapturePageModule["requestPlacementDetailRotate"]
  requestPlacementDetailArchive: () => void
  copyPlacementDetailLink: () => void
  openPlacementDetailPreview: OperatorCapturePageModule["openPlacementDetailPreview"]
}

/**
 * Subscribe to the Capture Placement Detail module without notifying live
 * Capture subscribers (description draft keystrokes stay on this seam).
 */
export function useCapturePlacementDetailModule(): CapturePlacementDetailModuleApi {
  const pageModule = useCapturePageModuleApi()
  const detailModule = pageModule.getPlacementDetailModule()
  const snapshot = useSyncExternalStore(
    detailModule.subscribe,
    detailModule.getSnapshot,
    detailModule.getSnapshot
  )

  return {
    snapshot,
    closePlacementDetail: detailModule.close,
    setPlacementDetailDescriptionDraft: detailModule.setDescriptionDraft,
    savePlacementDetailDescription: async () => {
      const result = await pageModule.savePlacementDetailDescription()
      if (result === "saved") {
        toast.success("Description saved")
      }
      return result
    },
    requestPlacementDetailPause: () => {
      pageModule.requestPlacementDetailPause()
    },
    requestPlacementDetailActivate: () => {
      pageModule.requestPlacementDetailActivate()
    },
    requestPlacementDetailRotate: () => {
      pageModule.requestPlacementDetailRotate()
    },
    requestPlacementDetailArchive: () => {
      void pageModule.requestPlacementDetailArchive().then((result) => {
        if (result !== "failed" && result !== "noop") {
          toast.success(result.toastMessage, {
            duration: CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS,
          })
        }
      })
    },
    copyPlacementDetailLink: () => {
      void pageModule.copyPlacementDetailLink().then((result) => {
        if (result === "copied") {
          toast.success("Link copied")
        }
      })
    },
    openPlacementDetailPreview: () => {
      pageModule.openPlacementDetailPreview()
    },
  }
}

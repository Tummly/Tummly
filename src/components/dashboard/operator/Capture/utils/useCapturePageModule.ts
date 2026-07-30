import { useSyncExternalStore } from "react"
import { toast } from "sonner"

import { useCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import {
  CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS,
  OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY,
  OPERATOR_CAPTURE_ROTATE_CONFIRM_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type {
  ConfirmPauseActivateResult,
  CreateDigitalGuestLinkModuleInput,
  CreateDigitalGuestLinkModuleResult,
  OperatorCapturePageModule,
  OperatorCapturePageSnapshot,
} from "@/lib/operatorCapture/createOperatorCapturePageModule"

export type OperatorCapturePageModuleApi = {
  snapshot: OperatorCapturePageSnapshot
  syncWorkspace: OperatorCapturePageModule["syncWorkspace"]
  retryLoad: OperatorCapturePageModule["retryLoad"]
  reloadForCapturePerformanceDateRange: OperatorCapturePageModule["reloadForCapturePerformanceDateRange"]
  requestPauseConfirm: (qrCodeId: number) => void
  requestActivateConfirm: (qrCodeId: number) => void
  cancelPauseActivateConfirm: () => void
  confirmPauseActivate: () => Promise<ConfirmPauseActivateResult>
  copyPlacementLink: (qrCodeId: number) => void
  createDigitalGuestLink: (
    input: CreateDigitalGuestLinkModuleInput
  ) => Promise<CreateDigitalGuestLinkModuleResult>
  openPlacementPreview: (qrCodeId: number) => void
  archivePlacement: (qrCodeId: number) => void
  requestDigitalGuestLinkArchive: (qrCodeId: number) => void
  openGuestExperiencePreview: () => void
  closeGuestExperiencePreview: () => void
  closeGuestExperiencePreviewPicker: () => void
  selectGuestExperiencePreviewPickerOption: (qrCodeId: number | null) => void
  confirmGuestExperiencePreviewPicker: () => void
  openPlacementDetail: (qrCodeId: number) => void
  closePlacementDetail: () => void
  setPlacementDetailDescriptionDraft: (value: string) => void
  savePlacementDetailDescription: () => void
  requestPlacementDetailPause: () => void
  requestPlacementDetailActivate: () => void
  requestRotate: (qrCodeId: number) => void
  requestPlacementDetailRotate: () => void
  setRotatePrintMaterialsAcknowledged: (acknowledged: boolean) => void
  cancelRotateConfirm: () => void
  confirmRotate: () => Promise<"rotated" | "failed" | "noop">
  requestPlacementDetailArchive: () => void
  copyPlacementDetailLink: () => void
  openPlacementDetailPreview: () => void
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
    requestPauseConfirm: (qrCodeId) => {
      pageModule.requestPauseConfirm(qrCodeId)
    },
    requestActivateConfirm: (qrCodeId) => {
      pageModule.requestActivateConfirm(qrCodeId)
    },
    cancelPauseActivateConfirm: pageModule.cancelPauseActivateConfirm,
    confirmPauseActivate: async () => {
      const result = await pageModule.confirmPauseActivate()
      if (result !== "failed" && result !== "noop") {
        toast.success(result.toastMessage, {
          duration: CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS,
        })
      }
      return result
    },
    copyPlacementLink: (qrCodeId) => {
      void pageModule.copyPlacementLink(qrCodeId).then((result) => {
        if (result === "copied") {
          toast.success("Link copied")
        }
      })
    },
    createDigitalGuestLink: async (input) => {
      const result = await pageModule.createDigitalGuestLink(input)
      if (result === "created") {
        toast.success(
          OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY.successToast
        )
      }
      return result
    },
    openPlacementPreview: (qrCodeId) => {
      pageModule.openPlacementPreview(qrCodeId)
    },
    archivePlacement: (qrCodeId) => {
      void pageModule.archivePlacement(qrCodeId).then((result) => {
        if (result !== "failed" && result !== "noop") {
          toast.success(result.toastMessage, {
            duration: CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS,
          })
        }
      })
    },
    requestDigitalGuestLinkArchive: (qrCodeId) => {
      void pageModule.requestDigitalGuestLinkArchive(qrCodeId).then((result) => {
        if (result !== "failed" && result !== "noop") {
          toast.success(result.toastMessage, {
            duration: CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS,
          })
        }
      })
    },
    openGuestExperiencePreview: pageModule.openGuestExperiencePreview,
    closeGuestExperiencePreview: pageModule.closeGuestExperiencePreview,
    closeGuestExperiencePreviewPicker:
      pageModule.closeGuestExperiencePreviewPicker,
    selectGuestExperiencePreviewPickerOption: (qrCodeId) => {
      pageModule.selectGuestExperiencePreviewPickerOption(qrCodeId)
    },
    confirmGuestExperiencePreviewPicker: () => {
      pageModule.confirmGuestExperiencePreviewPicker()
    },
    openPlacementDetail: (qrCodeId) => {
      pageModule.openPlacementDetail(qrCodeId)
    },
    closePlacementDetail: pageModule.closePlacementDetail,
    setPlacementDetailDescriptionDraft:
      pageModule.setPlacementDetailDescriptionDraft,
    savePlacementDetailDescription: () => {
      pageModule.savePlacementDetailDescription()
    },
    requestPlacementDetailPause: () => {
      pageModule.requestPlacementDetailPause()
    },
    requestPlacementDetailActivate: () => {
      pageModule.requestPlacementDetailActivate()
    },
    requestRotate: (qrCodeId) => {
      pageModule.requestRotate(qrCodeId)
    },
    requestPlacementDetailRotate: () => {
      pageModule.requestPlacementDetailRotate()
    },
    setRotatePrintMaterialsAcknowledged:
      pageModule.setRotatePrintMaterialsAcknowledged,
    cancelRotateConfirm: pageModule.cancelRotateConfirm,
    confirmRotate: async () => {
      const result = await pageModule.confirmRotate()
      if (result === "rotated") {
        toast.success(OPERATOR_CAPTURE_ROTATE_CONFIRM_COPY.successToast)
      }
      return result
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

import { isAxiosError } from "axios"
import { toast } from "sonner"

import { createDigitalGuestLink as createDigitalGuestLinkApi } from "@/api/dashboardApi"
import { OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY } from "@/lib/operatorCapture/capturePresentation"
import type {
  CreateDigitalGuestLinkAdapterResult,
  CreateDigitalGuestLinkModuleInput,
} from "@/lib/operatorCapture/createOperatorCapturePageModule"
import type { CreateDigitalGuestLinkErrorBody } from "@/types/dashboard"

async function createDigitalGuestLink(
  locationId: number,
  input: CreateDigitalGuestLinkModuleInput
): Promise<CreateDigitalGuestLinkAdapterResult> {
  try {
    const response = await createDigitalGuestLinkApi(locationId, input)
    return { ok: true, qrCodeId: response.qrCodeId }
  } catch (error) {
    if (isAxiosError(error) && error.response?.status === 409) {
      const body = error.response.data as
        | CreateDigitalGuestLinkErrorBody
        | undefined
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

function onCreateDigitalGuestLinkError(message: string): void {
  toast.error(message)
}

function onDigitalGuestLinkCreated(message: string): void {
  toast.success(message)
}

/** Shared Create digital guest link HTTP + toast wiring for Capture providers. */
export const createDigitalGuestLinkAdapters = {
  createDigitalGuestLink,
  onCreateDigitalGuestLinkError,
  onDigitalGuestLinkCreated,
} as const

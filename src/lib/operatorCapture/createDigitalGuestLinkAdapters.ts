import { isAxiosError } from "axios"
import { toast } from "sonner"

import {
  createDigitalGuestLink as createDigitalGuestLinkApi,
  listCatalogOffers as listCatalogOffersApi,
} from "@/api/dashboardApi"
import { OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY } from "@/lib/operatorCapture/capturePresentation"
import type {
  CreateDigitalGuestLinkAdapterResult,
  CreateDigitalGuestLinkModuleInput,
} from "@/lib/operatorCapture/createOperatorCapturePageModule"
import {
  ATTACHABLE_OFFER_STATUS_IDS,
  isAttachableCatalogOfferStatus,
} from "@/lib/operatorOffers/offersFilterSheetSchema"
import type {
  CreateDigitalGuestLinkErrorBody,
  CreateDigitalGuestLinkRequest,
} from "@/types/dashboard"

export type ConnectedOfferOption = {
  id: number
  title: string
}

function toCreateDigitalGuestLinkRequest(
  input: CreateDigitalGuestLinkModuleInput
): CreateDigitalGuestLinkRequest {
  return {
    linkName: input.linkName,
    internalDescription: input.internalDescription,
    channel: input.channel,
    status: input.status,
  }
}

async function createDigitalGuestLink(
  locationId: number,
  input: CreateDigitalGuestLinkModuleInput
): Promise<CreateDigitalGuestLinkAdapterResult> {
  try {
    const response = await createDigitalGuestLinkApi(
      locationId,
      toCreateDigitalGuestLinkRequest(input)
    )
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

async function listConnectedOfferOptions(
  locationId: number
): Promise<ConnectedOfferOption[]> {
  const response = await listCatalogOffersApi({
    locationId,
    view: "all",
    page: 1,
    pageSize: 25,
    status: [...ATTACHABLE_OFFER_STATUS_IDS],
  })
  return (response.items ?? [])
    .filter((item) => isAttachableCatalogOfferStatus(item.status))
    .map((item) => ({ id: item.id, title: item.title }))
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
  listConnectedOfferOptions,
  onCreateDigitalGuestLinkError,
  onDigitalGuestLinkCreated,
} as const

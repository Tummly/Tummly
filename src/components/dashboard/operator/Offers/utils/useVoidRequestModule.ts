import { useSyncExternalStore } from "react"

import { useVoidRequestModuleApi } from "@/components/dashboard/operator/Offers/utils/voidRequestModuleContext"
import type {
  VoidRequestModule,
  VoidRequestSnapshot,
} from "@/lib/operatorOffers/createVoidRequestModule"

export type VoidRequestModuleApi = VoidRequestModule & {
  snapshot: VoidRequestSnapshot
}

/** Details (and Main if needed) share this hook under OffersPageModuleProvider. */
export function useVoidRequestModule(): VoidRequestModuleApi {
  const voidRequest = useVoidRequestModuleApi()
  const snapshot = useSyncExternalStore(
    voidRequest.subscribe,
    voidRequest.getSnapshot,
    voidRequest.getSnapshot
  )

  return {
    snapshot,
    getSnapshot: voidRequest.getSnapshot,
    subscribe: voidRequest.subscribe,
    openCreate: voidRequest.openCreate,
    openReview: voidRequest.openReview,
    openApproveConfirm: voidRequest.openApproveConfirm,
    openRejectConfirm: voidRequest.openRejectConfirm,
    setReason: voidRequest.setReason,
    setExplanation: voidRequest.setExplanation,
    setCorrection: voidRequest.setCorrection,
    sendRequest: voidRequest.sendRequest,
    requestApprove: voidRequest.requestApprove,
    requestReject: voidRequest.requestReject,
    confirmApprove: voidRequest.confirmApprove,
    confirmReject: voidRequest.confirmReject,
    goBack: voidRequest.goBack,
    close: voidRequest.close,
  }
}

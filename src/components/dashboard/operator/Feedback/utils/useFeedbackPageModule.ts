import { useSyncExternalStore } from "react"

import { useFeedbackPageModuleApi } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import type {
  OperatorFeedbackPageModule,
  OperatorFeedbackPageSnapshot,
} from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"

export type OperatorFeedbackPageModuleApi = {
  snapshot: OperatorFeedbackPageSnapshot
  retryLoad: OperatorFeedbackPageModule["retryLoad"]
  reloadForFeedbackPageDateRange: OperatorFeedbackPageModule["reloadForFeedbackPageDateRange"]
  reviewNeedsAttention: OperatorFeedbackPageModule["reviewNeedsAttention"]
  requestOpenDateRange: OperatorFeedbackPageModule["requestOpenDateRange"]
  setActiveInboxTabId: OperatorFeedbackPageModule["setActiveInboxTabId"]
}

export function useFeedbackPageModule(): OperatorFeedbackPageModuleApi {
  const pageModule = useFeedbackPageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )

  return {
    snapshot,
    retryLoad: pageModule.retryLoad,
    reloadForFeedbackPageDateRange: pageModule.reloadForFeedbackPageDateRange,
    reviewNeedsAttention: pageModule.reviewNeedsAttention,
    requestOpenDateRange: pageModule.requestOpenDateRange,
    setActiveInboxTabId: pageModule.setActiveInboxTabId,
  }
}

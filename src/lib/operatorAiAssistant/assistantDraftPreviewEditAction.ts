import type {
  OperatorAiAssistantAction,
  OperatorAiAssistantDraftPreview,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { RecoveryDraftActionPayload } from "@/lib/operatorFeedback/recoveryDraftAction"

export function assistantDraftPreviewEditAction(
  draft: OperatorAiAssistantDraftPreview,
  pendingRecoveryDraft?: RecoveryDraftActionPayload | null
): OperatorAiAssistantAction | null {
  if (draft.kind === "campaign" && draft.campaignId != null) {
    return {
      type: "review-campaign",
      label: "Edit Draft",
      campaignId: draft.campaignId,
    }
  }
  if (draft.kind === "offer" && draft.offerId != null) {
    return {
      type: "review-offer",
      label: "Edit Draft",
      offerId: draft.offerId,
    }
  }
  if (draft.kind === "recovery" && draft.feedbackId != null) {
    // After send, pending draft is cleared — do not show a dead Edit Draft control.
    if (pendingRecoveryDraft == null) {
      return null
    }
    if (
      pendingRecoveryDraft.bulkShared === true
      && pendingRecoveryDraft.batchId != null
      && pendingRecoveryDraft.batchId > 0
    ) {
      return {
        type: "open-bulk-recovery",
        label: "Edit Draft",
        feedbackId: draft.feedbackId,
        batchId: pendingRecoveryDraft.batchId,
        intent: draft.intent ?? undefined,
        count: pendingRecoveryDraft.feedbackIds?.length,
      }
    }
    return {
      type: "open-recovery",
      label: "Edit Draft",
      feedbackId: draft.feedbackId,
      intent: draft.intent ?? undefined,
    }
  }
  return null
}

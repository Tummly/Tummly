import type {
  OperatorAiAssistantAction,
  OperatorAiAssistantDraftPreview,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"

export function assistantDraftPreviewEditAction(
  draft: OperatorAiAssistantDraftPreview
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
    return {
      type: "open-recovery",
      label: "Edit Draft",
      feedbackId: draft.feedbackId,
      intent: draft.intent ?? undefined,
    }
  }
  return null
}

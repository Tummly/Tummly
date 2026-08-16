import type {
  RespondToGuestChannel,
  RespondToGuestPurposeId,
  RespondToGuestToneId,
} from "@/lib/operatorFeedback/respondToGuestPresentation"
import type { InternalActionCategoryId } from "@/lib/operatorFeedback/internalActionPresentation"
import type { StartRecoveryIntentId } from "@/lib/operatorFeedback/startRecoveryPresentation"
import {
  buildStartRecoveryIntents,
  deriveStartRecoveryContactCapability,
  type StartRecoveryContactCapability,
} from "@/lib/operatorFeedback/startRecoveryPresentation"
import type {
  ContactType,
  FeedbackWorkflowStatus,
} from "@/types/dashboard"

/** Server pendingRecoveryDraft / one-shot router payload for Review hydrate. */
export type RecoveryDraftActionPayload = {
  feedbackId: number
  intent: StartRecoveryIntentId
  channel?: RespondToGuestChannel | null
  purpose?: RespondToGuestPurposeId | "include_a_recovery_offer" | null
  tone?: RespondToGuestToneId | null
  includeNotes?: string | null
  subject?: string | null
  message?: string | null
  category?: InternalActionCategoryId | null
  note?: string | null
  offerId?: number | null
  useConfirmedActionForGuestResponse?: boolean
}

export const RECOVERY_DRAFT_ACTION_TOASTS = {
  resolved: "This feedback is resolved. Reopen it before starting recovery.",
  noContact: "No contact method available",
  offersOptOut: "Guest has opted out of offers",
  statusAdvance: "Could not update follow-up status. Please try again.",
  openFailed: "Could not open recovery. Please try again.",
} as const

export function isRecoveryDraftIntent(
  value: string | null | undefined
): value is StartRecoveryIntentId {
  return (
    value === "respond-to-guest"
    || value === "respond-and-record-internal-action"
    || value === "record-internal-action-only"
    || value === "respond-with-recovery-offer"
  )
}

export function recoveryDraftActionGateToast(input: {
  intent: StartRecoveryIntentId
  workflowStatus: FeedbackWorkflowStatus
  contactType: ContactType
  guestContact: string
  guestOffersOptOut: boolean
}): string | null {
  if (input.workflowStatus === "resolved") {
    return RECOVERY_DRAFT_ACTION_TOASTS.resolved
  }

  const contactCapability: StartRecoveryContactCapability =
    deriveStartRecoveryContactCapability(input.contactType, input.guestContact)
  const card = buildStartRecoveryIntents({
    contactCapability,
    guestOffersOptOut: input.guestOffersOptOut,
    workflowStatus: input.workflowStatus,
  }).find((item) => item.id === input.intent)

  if (card == null || card.enabled) {
    return null
  }

  if (card.disableReason === "No contact method available") {
    return RECOVERY_DRAFT_ACTION_TOASTS.noContact
  }
  if (card.disableReason === "Guest has opted out of offers") {
    return RECOVERY_DRAFT_ACTION_TOASTS.offersOptOut
  }
  return RECOVERY_DRAFT_ACTION_TOASTS.openFailed
}

export function parseRecoveryDraftActionRouterState(
  state: unknown
): RecoveryDraftActionPayload | null {
  if (state == null || typeof state !== "object") {
    return null
  }
  const recoveryDraft = (state as { recoveryDraft?: unknown }).recoveryDraft
  if (recoveryDraft == null || typeof recoveryDraft !== "object") {
    return null
  }
  const row = recoveryDraft as Partial<RecoveryDraftActionPayload>
  if (
    typeof row.feedbackId !== "number"
    || row.feedbackId <= 0
    || !isRecoveryDraftIntent(row.intent)
  ) {
    return null
  }
  return {
    feedbackId: row.feedbackId,
    intent: row.intent,
    channel: row.channel ?? null,
    purpose: row.purpose ?? null,
    tone: row.tone ?? null,
    includeNotes: row.includeNotes ?? "",
    subject: row.subject ?? "",
    message: row.message ?? "",
    category: row.category ?? null,
    note: row.note ?? "",
    offerId: row.offerId ?? null,
    useConfirmedActionForGuestResponse:
      row.useConfirmedActionForGuestResponse === true,
  }
}

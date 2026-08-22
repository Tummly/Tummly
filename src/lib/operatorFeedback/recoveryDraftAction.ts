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
  LocationGuestMarketingPreference,
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
  locationId?: number | null
  useConfirmedActionForGuestResponse?: boolean
}

export const RECOVERY_DRAFT_ACTION_TOASTS = {
  resolved: "This feedback is resolved. Reopen it before starting recovery.",
  noContact: "No contact method available",
  offersOptOut: "Guest has opted out of offers",
  statusAdvance: "Could not update follow-up status. Please try again.",
  openFailed: "Could not open recovery. Please try again.",
} as const

const RESPOND_CHANNELS = new Set<RespondToGuestChannel>(["email", "sms"])

const RESPOND_PURPOSES = new Set<RespondToGuestPurposeId>([
  "acknowledge_feedback",
  "apologise_and_confirm_follow_up",
  "ask_for_more_information",
  "confirm_operational_action",
  "create_custom_response",
])

const RESPOND_TONES = new Set<RespondToGuestToneId>([
  "warm_and_apologetic",
  "direct_and_practical",
  "appreciative",
  "use_restaurant_tone",
])

const INTERNAL_CATEGORIES = new Set<InternalActionCategoryId>([
  "team_briefed",
  "order_or_service_process_reviewed",
  "delivery_issue_investigated",
  "product_quality_checked",
  "cleaning_issue_addressed",
  "staff_follow_up_completed",
  "other_action",
])

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

function asChannel(value: unknown): RespondToGuestChannel | null {
  return typeof value === "string" && RESPOND_CHANNELS.has(value as RespondToGuestChannel)
    ? (value as RespondToGuestChannel)
    : null
}

function asPurpose(
  value: unknown
): RespondToGuestPurposeId | "include_a_recovery_offer" | null {
  if (value === "include_a_recovery_offer") {
    return value
  }
  return typeof value === "string"
    && RESPOND_PURPOSES.has(value as RespondToGuestPurposeId)
    ? (value as RespondToGuestPurposeId)
    : null
}

function asTone(value: unknown): RespondToGuestToneId | null {
  return typeof value === "string" && RESPOND_TONES.has(value as RespondToGuestToneId)
    ? (value as RespondToGuestToneId)
    : null
}

function asCategory(value: unknown): InternalActionCategoryId | null {
  return typeof value === "string"
    && INTERNAL_CATEGORIES.has(value as InternalActionCategoryId)
    ? (value as InternalActionCategoryId)
    : null
}

function asOfferId(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) && value > 0
    ? value
    : null
}

function asTrimmedString(value: unknown): string {
  return typeof value === "string" ? value : ""
}

/**
 * Normalize a recovery draft object from router state or the Assistant API.
 * Returns null when required fields are missing or enum values are invalid.
 */
export function parseRecoveryDraftActionPayload(
  draft: unknown
): RecoveryDraftActionPayload | null {
  if (draft == null || typeof draft !== "object") {
    return null
  }
  const row = draft as Record<string, unknown>
  const feedbackId = row.feedbackId
  if (
    typeof feedbackId !== "number"
    || !Number.isFinite(feedbackId)
    || feedbackId <= 0
    || !isRecoveryDraftIntent(
      typeof row.intent === "string" ? row.intent : null
    )
  ) {
    return null
  }
  const intent = row.intent as StartRecoveryIntentId
  const channel = asChannel(row.channel)
  const purpose = asPurpose(row.purpose)
  const tone = asTone(row.tone)
  const category = asCategory(row.category)
  const offerId = asOfferId(row.offerId)

  if (
    intent === "respond-to-guest"
    || intent === "respond-and-record-internal-action"
  ) {
    if (channel == null || purpose == null || tone == null) {
      return null
    }
  }
  if (intent === "respond-and-record-internal-action" && category == null) {
    return null
  }
  if (intent === "record-internal-action-only" && category == null) {
    return null
  }
  if (intent === "respond-with-recovery-offer") {
    if (channel == null || tone == null || offerId == null) {
      return null
    }
  }

  return {
    feedbackId,
    intent,
    channel,
    purpose,
    tone,
    includeNotes: asTrimmedString(row.includeNotes),
    subject: asTrimmedString(row.subject),
    message: asTrimmedString(row.message),
    category,
    note: asTrimmedString(row.note),
    offerId,
    locationId: asOfferId(row.locationId),
    useConfirmedActionForGuestResponse:
      row.useConfirmedActionForGuestResponse === true,
  }
}

export function recoveryDraftActionGateToast(input: {
  intent: StartRecoveryIntentId
  workflowStatus: FeedbackWorkflowStatus
  contactType: ContactType
  guestContact: string
  marketingPreference: LocationGuestMarketingPreference | undefined
}): string | null {
  if (input.workflowStatus === "resolved") {
    return RECOVERY_DRAFT_ACTION_TOASTS.resolved
  }

  const contactCapability: StartRecoveryContactCapability =
    deriveStartRecoveryContactCapability(input.contactType, input.guestContact)
  const card = buildStartRecoveryIntents({
    contactCapability,
    marketingPreference: input.marketingPreference,
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
  return parseRecoveryDraftActionPayload(recoveryDraft)
}

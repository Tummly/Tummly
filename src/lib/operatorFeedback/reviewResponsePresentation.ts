import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"

/** Em dash for empty Review / Usage values (Figma Review response). */
export const REVIEW_RESPONSE_EMPTY_VALUE = "—"

/** Figma Review response step heading (under stepper; stepper may stay Review and send). */
export const REVIEW_RESPONSE_STEP_HEADING = "Review response"

export const REVIEW_RESPONSE_STEP_DESCRIPTION =
  "Confirm the guest, contact method and final response before sending."

/**
 * Session AI usage for Review — zero is an em dash, not `"0"`.
 */
export function formatReviewAiUsage(aiActionCount: number): string {
  const count = Math.max(0, Math.trunc(aiActionCount))
  if (count === 0) {
    return REVIEW_RESPONSE_EMPTY_VALUE
  }
  if (count === 1) {
    return "1 AI action used"
  }
  return `${count} AI actions used`
}

/**
 * Planned Delivery usage from the selected channel before send.
 */
export function formatReviewDeliveryUsage(
  channel: RespondToGuestChannel | null,
  recipientCount = 1
): string {
  const count = Math.max(1, Math.trunc(recipientCount))
  if (channel === "email") {
    return count === 1 ? "1 email" : `${count} emails`
  }
  if (channel === "sms") {
    return count === 1 ? "1 SMS" : `${count} SMS`
  }
  return REVIEW_RESPONSE_EMPTY_VALUE
}

/** Bulk Review guest row — e.g. "12 guests". */
export function formatBulkReviewGuestLabel(memberCount: number): string {
  const count = Math.max(0, Math.trunc(memberCount))
  if (count === 1) {
    return "1 guest"
  }
  return `${count} guests`
}

/** Bulk Review destination row. */
export function formatBulkReviewDestinationLabel(
  channel: RespondToGuestChannel | null,
  memberCount: number
): string {
  const count = Math.max(0, Math.trunc(memberCount))
  if (channel === "email") {
    return count === 1
      ? "1 email destination"
      : `${count} email destinations`
  }
  if (channel === "sms") {
    return count === 1
      ? "1 SMS destination"
      : `${count} SMS destinations`
  }
  return REVIEW_RESPONSE_EMPTY_VALUE
}

/** Bulk Review feedback reference row. */
export function formatBulkReviewFeedbackReference(memberCount: number): string {
  const count = Math.max(0, Math.trunc(memberCount))
  if (count === 1) {
    return "1 Feedback"
  }
  return `${count} Feedback`
}

export const BULK_REVIEW_ORIGINAL_FEEDBACK_COPY =
  "Shared message for the needs-attention Feedback set."

/** Channel row value on Review — separate from Destination. */
export function labelForReviewChannel(
  channel: RespondToGuestChannel | null
): string {
  if (channel === "email") {
    return "Email"
  }
  if (channel === "sms") {
    return "SMS"
  }
  return REVIEW_RESPONSE_EMPTY_VALUE
}

/** Feedback reference row — `FDB-{padded id}`. */
export function formatReviewFeedbackReference(feedbackId: number): string {
  const padded = String(Math.trunc(feedbackId)).padStart(6, "0")
  return `FDB-${padded}`
}

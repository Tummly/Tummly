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
  channel: RespondToGuestChannel | null
): string {
  if (channel === "email") {
    return "1 email"
  }
  if (channel === "sms") {
    return "1 SMS"
  }
  return REVIEW_RESPONSE_EMPTY_VALUE
}

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

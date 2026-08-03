import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"

/** Review right-rail Guest preview chrome (Figma U-01). */

export const GUEST_PREVIEW_HEADING = "Guest preview"

export const GUEST_PREVIEW_CONTROL_LABEL = "Preview"

export const GUEST_PREVIEW_EDIT_TEXT_LABEL = "Edit text"

export function guestPreviewMockTitle(
  channel: RespondToGuestChannel | null
): string {
  return channel === "sms" ? "SMS preview" : "Email preview"
}

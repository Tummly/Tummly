/**
 * Capture Guest experience — Guest form thank-you catalog attach (ticket 07).
 */

export const CAPTURE_THANK_YOU_OFFER_COPY = {
  dialogTitle: "Connected offers",
  dialogDescription:
    "Attach one catalog offer to the Guest form thank-you for this location.",
  createStanceTitle: "Create a new offer",
  createStanceDescription: "Build an Active catalog offer and attach it here.",
  existingStanceTitle: "Use an existing offer",
  existingStanceDescription: "Pick an Active offer from this location’s catalog.",
  clearStanceTitle: "Clear thank-you offer",
  clearStanceDescription: "Guests will not receive an offer on thank-you.",
  attachedLabel: "Attached offer",
  notLiveHelper: "This offer is not Active. Replace it or clear the attach.",
  closeLabel: "Close",
  attachSuccessToast: "Thank-you offer updated",
  clearSuccessToast: "Thank-you offer cleared",
  attachError: "Could not update the thank-you offer. Try again.",
  createThenAttachError: "Offer created, but attach failed. Try again.",
} as const

export const CAPTURE_CONNECTED_OFFERS_NONE = "No active offers" as const

export type CaptureThankYouOfferFact = {
  offerId: number | null
  title: string | null
  live: boolean
}

export function formatCaptureConnectedOffersText(
  thankYou: CaptureThankYouOfferFact | null | undefined
): string {
  if (thankYou == null || thankYou.offerId == null) {
    return CAPTURE_CONNECTED_OFFERS_NONE
  }

  const title = thankYou.title?.trim() ?? ""
  if (title.length > 0) {
    return title
  }

  return CAPTURE_CONNECTED_OFFERS_NONE
}

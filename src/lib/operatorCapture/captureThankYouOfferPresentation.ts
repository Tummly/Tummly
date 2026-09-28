import {
  GUEST_PREVIEW_EMPTY_VALUE,
  GUEST_PREVIEW_OFFER_COPY_LABEL,
  GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
  type GuestPreviewOfferCouponView,
} from "@/lib/operatorFeedback/guestPreviewPresentation"

/**
 * Capture Guest experience — Guest form thank-you catalog attach (ticket 07).
 */

export const CAPTURE_THANK_YOU_OFFER_COPY = {
  dialogTitle: "Guest form settings",
  dialogDescription:
    "Review the Guest form for this location and choose which catalog offer appears on thank-you.",
  createStanceTitle: "Create a new offer",
  createStanceDescription: "Build an Active catalog offer and attach it here.",
  existingStanceTitle: "Use an existing offer",
  existingStanceDescription: "Pick an Active offer from this location’s catalog.",
  clearStanceTitle: "Clear thank-you offer",
  clearStanceDescription: "Guests will not receive an offer on thank-you.",
  attachedLabel: "Attached offer",
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

/** Capture / DGL display: only live Active attaches count as attached. */
export function toDisplayedCaptureThankYouOffer(
  thankYou: CaptureThankYouOfferFact | null | undefined
): CaptureThankYouOfferFact {
  if (thankYou == null || thankYou.offerId == null || !thankYou.live) {
    return { offerId: null, title: null, live: false }
  }
  return thankYou
}

export function formatCaptureConnectedOffersText(
  thankYou: CaptureThankYouOfferFact | null | undefined
): string {
  const displayed = toDisplayedCaptureThankYouOffer(thankYou)
  if (displayed.offerId == null) {
    return CAPTURE_CONNECTED_OFFERS_NONE
  }

  const title = displayed.title?.trim() ?? ""
  if (title.length > 0) {
    return title
  }

  return CAPTURE_CONNECTED_OFFERS_NONE
}

/**
 * Capture Thank you tab sample coupon — placeholder Claim code, Copy disabled.
 * No Issue is created from preview.
 */
export function buildCaptureThankYouPreviewCoupon(
  thankYou: CaptureThankYouOfferFact | null | undefined
): GuestPreviewOfferCouponView | null {
  const displayed = toDisplayedCaptureThankYouOffer(thankYou)
  if (displayed.offerId == null) {
    return null
  }

  const title = displayed.title?.trim() ?? ""
  if (title === "") {
    return null
  }

  return {
    title,
    description: "",
    redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
    expiryLabel: `Expires: ${GUEST_PREVIEW_EMPTY_VALUE}`,
    copyLabel: GUEST_PREVIEW_OFFER_COPY_LABEL,
    copyEnabled: false,
  }
}

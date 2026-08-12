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
  if (thankYou == null || thankYou.offerId == null || !thankYou.live) {
    return CAPTURE_CONNECTED_OFFERS_NONE
  }

  const title = thankYou.title?.trim() ?? ""
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
  if (thankYou == null || thankYou.offerId == null || !thankYou.live) {
    return null
  }

  const title = thankYou.title?.trim() ?? ""
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

import { formatRecoverySuccessDate } from "@/lib/operatorFeedback/recoverySuccessPresentation"
import {
  labelForRecoveryOfferValidity,
  type ConfirmedRecoveryOfferPayload,
  type RecoveryOfferValidityId,
} from "@/lib/operatorFeedback/recoveryOfferPresentation"

/** Review right-rail + overlay Guest preview chrome (Figma recovery Review). */

/**
 * Full-viewport Guest preview overlay — portaled to `document.body`.
 * `z-[135]` sits above Operator wizard shell (`z-[130]`) and below Send test
 * / confirm dialogs (`z-[140]`).
 * `pointer-events-auto` is required: Radix Dialog sets `pointer-events: none` on
 * `body` while the wizard is open, so a body-portal without this is visible but
 * inert (clicks fall through to the wizard close control).
 * Header stays fixed; body scrolls (`min-h-0` + `overflow-y-auto`).
 * The overlay mounts under a nested `RemoveScroll` so wheel/touch scroll is not
 * blocked by the parent wizard Dialog scroll lock.
 */
export const GUEST_PREVIEW_OVERLAY_CLASS =
  "fixed inset-0 z-[135] flex flex-col overflow-hidden bg-op-surface-primary pointer-events-auto text-op-text-primary"

/**
 * Rounded body under the overlay header — same border token as Operator wizard
 * body (`border-op-card-border`), not Capture light gray-200.
 * Scroll lives here so tall email chrome is reachable under the sticky header.
 */
export const GUEST_PREVIEW_OVERLAY_BODY_CLASS =
  "flex min-h-0 flex-1 flex-col overflow-y-auto rounded-t-[20px] border-t border-op-card-border bg-op-background-primary"

export const GUEST_PREVIEW_HEADING = "Guest preview"

export const GUEST_PREVIEW_CONTROL_LABEL = "Preview"

export const GUEST_PREVIEW_EDIT_TEXT_LABEL = "Edit text"

export const GUEST_PREVIEW_SEND_TEST_LABEL = "Send test"

export const GUEST_PREVIEW_SEND_TEST_SUCCESS =
  "Test email sent to your account."

export const GUEST_PREVIEW_SEND_TEST_ERROR =
  "We could not send the test email. Try again."

export const GUEST_PREVIEW_DESKTOP_LABEL = "Desktop"

export const GUEST_PREVIEW_MOBILE_LABEL = "Mobile"

export const GUEST_PREVIEW_CLOSE_LABEL = "Close"

export const GUEST_PREVIEW_FOOTER_UNSUBSCRIBE = "Unsubscribe"

export const GUEST_PREVIEW_FOOTER_TERMS = "Terms"

export const GUEST_PREVIEW_FOOTER_PRIVACY = "Privacy"

export const GUEST_PREVIEW_FOOTER_COOKIE = "Cookie settings"

export const GUEST_PREVIEW_POWERED_BY_LABEL = "Powered by"

/** Display-only email CTA chrome (Figma guest response email). */
export const GUEST_PREVIEW_GIVE_FEEDBACK_LABEL = "Give feedback"

/**
 * Placeholder redemption code in Guest preview — not an issued code.
 * Real codes are created only on Send and issue offer.
 */
export const GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER = "PREVIEW-CODE"

export const GUEST_PREVIEW_OFFER_COPY_LABEL = "Copy"

/** Em dash for missing brand/address chrome. */
export const GUEST_PREVIEW_EMPTY_VALUE = "—"

export const GUEST_PREVIEW_DEVICE = {
  desktop: "desktop",
  mobile: "mobile",
} as const

export type GuestPreviewDevice =
  (typeof GUEST_PREVIEW_DEVICE)[keyof typeof GUEST_PREVIEW_DEVICE]

/**
 * Email header brand title — restaurant brand when present, else location name,
 * else em dash.
 */
export function guestPreviewBrandTitle(
  brandName: string | null | undefined,
  locationName: string | null | undefined
): string {
  const brand = brandName?.trim()
  if (brand) {
    return brand
  }
  const location = locationName?.trim()
  if (location) {
    return location
  }
  return GUEST_PREVIEW_EMPTY_VALUE
}

/**
 * Second line under brand — location name when a distinct brand title is shown;
 * otherwise null.
 */
export function guestPreviewBrandSubtitle(
  brandName: string | null | undefined,
  locationName: string | null | undefined
): string | null {
  const brand = brandName?.trim()
  const location = locationName?.trim()
  if (!brand || !location) {
    return null
  }
  return location
}

/** Address line in email footer — em dash when address is missing. */
export function guestPreviewFooterAddress(
  displayName: string,
  address: string | null | undefined
): string {
  const trimmedAddress = address?.trim()
  return `${displayName}, ${trimmedAddress || GUEST_PREVIEW_EMPTY_VALUE}`
}

export function guestPreviewFooterDisclaimer(displayName: string): string {
  return `You're receiving this because you joined ${displayName} customer club after visiting or giving feedback.`
}

export type GuestPreviewOfferCouponView = {
  title: string
  description: string
  /**
   * Always the preview placeholder — never an issued redemption code.
   * Offer claim QR encodes this same sample string.
   */
  redemptionCode: string
  expiryLabel: string
  copyLabel: string
}

type GuestPreviewOfferCouponInput = Pick<
  ConfirmedRecoveryOfferPayload,
  "title" | "description" | "validity" | "expiryDate"
>

/** Parse `YYYY-MM-DD` as a local calendar date for expiry chrome. */
function parseOfferExpiryDate(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value.trim())
  if (match == null) {
    return null
  }
  const year = Number(match[1])
  const month = Number(match[2])
  const day = Number(match[3])
  const date = new Date(year, month - 1, day)
  if (
    date.getFullYear() !== year
    || date.getMonth() !== month - 1
    || date.getDate() !== day
  ) {
    return null
  }
  return date
}

export function formatGuestPreviewOfferExpiryLabel(
  validity: RecoveryOfferValidityId,
  expiryDate: string | null | undefined
): string {
  if (validity === "choose_expiry_date") {
    const parsed =
      expiryDate != null && expiryDate.trim() !== ""
        ? parseOfferExpiryDate(expiryDate)
        : null
    if (parsed != null) {
      return `Expires: ${formatRecoverySuccessDate(parsed)}`
    }
    return `Expires: ${GUEST_PREVIEW_EMPTY_VALUE}`
  }
  return `Expires: ${labelForRecoveryOfferValidity(validity)}`
}

/**
 * Email Guest preview offer coupon from the confirmed offer draft.
 * Redemption code is always a placeholder until issue; Offer claim QR encodes
 * that same sample code (no Issue created).
 */
export function buildGuestPreviewOfferCoupon(
  offer: GuestPreviewOfferCouponInput | null | undefined
): GuestPreviewOfferCouponView | null {
  if (offer == null) {
    return null
  }
  const title = offer.title.trim()
  if (title === "") {
    return null
  }
  return {
    title,
    description: offer.description.trim(),
    redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
    expiryLabel: formatGuestPreviewOfferExpiryLabel(
      offer.validity,
      offer.expiryDate
    ),
    copyLabel: GUEST_PREVIEW_OFFER_COPY_LABEL,
  }
}

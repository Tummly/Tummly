/** Review right-rail + overlay Guest preview chrome (Figma recovery Review). */

export const GUEST_PREVIEW_HEADING = "Guest preview"

export const GUEST_PREVIEW_CONTROL_LABEL = "Preview"

export const GUEST_PREVIEW_EDIT_TEXT_LABEL = "Edit text"

export const GUEST_PREVIEW_SEND_TEST_LABEL = "Send test"

export const GUEST_PREVIEW_DESKTOP_LABEL = "Desktop"

export const GUEST_PREVIEW_MOBILE_LABEL = "Mobile"

export const GUEST_PREVIEW_CLOSE_LABEL = "Close"

export const GUEST_PREVIEW_FOOTER_UNSUBSCRIBE = "Unsubscribe"

export const GUEST_PREVIEW_FOOTER_TERMS = "Terms"

export const GUEST_PREVIEW_FOOTER_PRIVACY = "Privacy"

export const GUEST_PREVIEW_FOOTER_COOKIE = "Cookie settings"

export const GUEST_PREVIEW_POWERED_BY_LABEL = "Powered by"

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
  return `You're receiving this because you joined ${displayName} guests list after visiting or giving feedback.`
}

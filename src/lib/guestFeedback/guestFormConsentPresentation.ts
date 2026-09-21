/** Guest-form consent checkbox copy (GF-01 / GF-02 hard opt-in). */

import { z } from "zod"

import { tryNormalizePhoneToE164 } from "@/lib/phoneNumber"

export type GuestFormConsentConfig = {
  emailMarketingEnabled: boolean
  smsMarketingEnabled: boolean
  feedbackFollowUpEnabled: boolean
  emailConsentWording?: string | null
  smsConsentWording?: string | null
  feedbackFollowUpWording: string
}

export type GuestFormMarketingChannel = "email" | "sms"

export const GUEST_FORM_CONSENT_DEMO: GuestFormConsentConfig = {
  emailMarketingEnabled: true,
  smsMarketingEnabled: true,
  feedbackFollowUpEnabled: true,
  emailConsentWording:
    "may send you offers and updates by email using the contact details you provide",
  smsConsentWording:
    "may send you offers and updates by SMS using the contact details you provide",
  feedbackFollowUpWording:
    "They may contact you about this feedback using the details you provide.",
}

/** Top copy under the form header — Feedback follow-up notice (not a checkbox). */
export function buildGuestFormIntroCopy(restaurantName: string): string {
  const display = restaurantName.trim() || "this restaurant"
  return (
    `Your feedback is shared privately with ${display}. `
    + "They may contact you about this feedback using the details you provide."
  )
}

function isValidEmailContact(value: string): boolean {
  const trimmed = value.trim()
  if (!trimmed.includes("@")) {
    return false
  }
  return z.string().email().safeParse(trimmed).success
}

function isValidUkMobileContact(value: string): boolean {
  const trimmed = value.trim()
  if (trimmed.includes("@")) {
    return false
  }
  return tryNormalizePhoneToE164(trimmed) !== null
}

/**
 * Which single marketing checkbox to show for the current contact value.
 * Never returns both — Email and SMS are mutually exclusive by contact type.
 * Null when contact is empty/invalid or the matching restaurant channel is off.
 */
export function resolveGuestFormMarketingChannel(
  guestContact: string,
  config: GuestFormConsentConfig | null | undefined
): GuestFormMarketingChannel | null {
  if (config == null) {
    return null
  }

  if (isValidEmailContact(guestContact)) {
    return config.emailMarketingEnabled ? "email" : null
  }

  if (isValidUkMobileContact(guestContact)) {
    return config.smsMarketingEnabled ? "sms" : null
  }

  return null
}

/** Hard opt-in label for the single visible marketing checkbox (GF-01 / GF-02). */
export function buildGuestFormConsentCheckboxLabel(
  restaurantName: string,
  channel: GuestFormMarketingChannel
): string {
  const display = restaurantName.trim() || "this restaurant"
  if (channel === "email") {
    return (
      `Yes, email me occasional offers and updates from ${display}. `
      + "You can unsubscribe at any time."
    )
  }
  return (
    `Yes, text me occasional offers and updates from ${display}. `
    + "You can opt out at any time."
  )
}

export function parseGuestFormConsentFromScanMetadata(
  raw: unknown
): GuestFormConsentConfig | null {
  if (raw == null || typeof raw !== "object") {
    return null
  }

  const value = raw as Record<string, unknown>
  if (typeof value.feedbackFollowUpWording !== "string") {
    return null
  }

  return {
    emailMarketingEnabled: value.emailMarketingEnabled === true,
    smsMarketingEnabled: value.smsMarketingEnabled === true,
    feedbackFollowUpEnabled: value.feedbackFollowUpEnabled === true,
    emailConsentWording:
      typeof value.emailConsentWording === "string"
        ? value.emailConsentWording
        : null,
    smsConsentWording:
      typeof value.smsConsentWording === "string"
        ? value.smsConsentWording
        : null,
    feedbackFollowUpWording: value.feedbackFollowUpWording,
  }
}

/** Guest-form consent checkbox copy (ticket 04 / PRD §3). */

export type GuestFormConsentConfig = {
  emailMarketingEnabled: boolean
  smsMarketingEnabled: boolean
  feedbackFollowUpEnabled: boolean
  emailConsentWording?: string | null
  smsConsentWording?: string | null
  feedbackFollowUpWording: string
}

export const GUEST_FORM_CONSENT_DEMO: GuestFormConsentConfig = {
  emailMarketingEnabled: true,
  smsMarketingEnabled: true,
  feedbackFollowUpEnabled: true,
  emailConsentWording:
    "may send you offers and updates by email using the contact details you provide",
  smsConsentWording:
    "may send you offers and updates by SMS using the contact details you provide",
  feedbackFollowUpWording:
    "They may contact you about your feedback using the contact details you provide.",
}

export const GUEST_FORM_MARKETING_OPT_OUT_HINT =
  "Untick here if you would prefer not to receive offers."

export function guestFormConsentHasAnyEnabled(
  config: GuestFormConsentConfig
): boolean {
  return (
    config.emailMarketingEnabled
    || config.smsMarketingEnabled
    || config.feedbackFollowUpEnabled
  )
}

export function buildGuestFormConsentCheckboxLabel(
  locationName: string,
  config: GuestFormConsentConfig
): string {
  const displayLocation = locationName.trim() || "this location"
  const parts: string[] = []

  if (config.feedbackFollowUpEnabled) {
    parts.push(config.feedbackFollowUpWording.trim())
  }

  if (config.emailMarketingEnabled) {
    const wording = config.emailConsentWording?.trim()
    parts.push(
      wording
        ? `${displayLocation} ${wording}.`
        : `${displayLocation} may send you offers and updates by email using the contact details you provide.`
    )
  }

  if (config.smsMarketingEnabled) {
    const wording = config.smsConsentWording?.trim()
    parts.push(
      wording
        ? `${displayLocation} ${wording}.`
        : `${displayLocation} may send you offers and updates by SMS using the contact details you provide.`
    )
  }

  const body = parts.join(" ")
  const hasMarketing =
    config.emailMarketingEnabled || config.smsMarketingEnabled

  if (hasMarketing) {
    return `${body} ${GUEST_FORM_MARKETING_OPT_OUT_HINT}`
  }

  return body
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

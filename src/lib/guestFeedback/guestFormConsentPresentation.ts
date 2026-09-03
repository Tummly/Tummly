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

const DEFAULT_MARKETING_WORDING =
  "may send you offers and updates by {channels} using the contact details you provide"

/** Checkbox is for marketing offers only — follow-up copy lives in the form intro. */
export function guestFormConsentHasAnyEnabled(
  config: GuestFormConsentConfig
): boolean {
  return config.emailMarketingEnabled || config.smsMarketingEnabled
}

function ensureSentence(text: string): string {
  const trimmed = text.trim()
  if (trimmed === "") {
    return ""
  }
  return trimmed.endsWith(".") ? trimmed : `${trimmed}.`
}

function defaultMarketingWording(channels: string): string {
  return DEFAULT_MARKETING_WORDING.replace("{channels}", channels)
}

function coupleChannelIntoWording(wording: string, channels: string): string {
  return wording
    .replace(/\bby email\b/gi, `by ${channels}`)
    .replace(/\bby SMS\b/gi, `by ${channels}`)
}

function buildMarketingConsentSentence(
  displayLocation: string,
  config: GuestFormConsentConfig
): string | null {
  const emailOn = config.emailMarketingEnabled
  const smsOn = config.smsMarketingEnabled

  if (!emailOn && !smsOn) {
    return null
  }

  if (emailOn && smsOn) {
    const channels = "Email and SMS"
    const emailWording = config.emailConsentWording?.trim()
    const smsWording = config.smsConsentWording?.trim()
    const base = emailWording || smsWording
    const wording = base
      ? coupleChannelIntoWording(base, channels)
      : defaultMarketingWording(channels)
    return ensureSentence(`${displayLocation} ${wording}`)
  }

  if (emailOn) {
    const wording =
      config.emailConsentWording?.trim()
      || defaultMarketingWording("email")
    return ensureSentence(`${displayLocation} ${wording}`)
  }

  const wording =
    config.smsConsentWording?.trim() || defaultMarketingWording("SMS")
  return ensureSentence(`${displayLocation} ${wording}`)
}

export function buildGuestFormConsentCheckboxLabel(
  locationName: string,
  config: GuestFormConsentConfig
): string {
  const displayLocation = locationName.trim() || "this location"
  const marketingSentence = buildMarketingConsentSentence(
    displayLocation,
    config
  )

  if (marketingSentence == null) {
    return ""
  }

  return `${marketingSentence} ${GUEST_FORM_MARKETING_OPT_OUT_HINT}`
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

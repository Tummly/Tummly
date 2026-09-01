import { describe, expect, it } from "vitest"

import {
  buildGuestFormConsentCheckboxLabel,
  GUEST_FORM_CONSENT_DEMO,
  guestFormConsentHasAnyEnabled,
} from "@/lib/guestFeedback/guestFormConsentPresentation"

describe("guestFormConsentPresentation", () => {
  it("builds combined label from enabled channels and operator wording", () => {
    const label = buildGuestFormConsentCheckboxLabel(
      "Camden Street",
      GUEST_FORM_CONSENT_DEMO
    )

    expect(label).toContain("contact you about your feedback")
    expect(label).toContain("offers and updates by email")
    expect(label).toContain("offers and updates by SMS")
    expect(label).toContain("prefer not to receive offers")
  })

  it("omits marketing opt-out hint when only feedback follow-up is enabled", () => {
    const label = buildGuestFormConsentCheckboxLabel("Main", {
      ...GUEST_FORM_CONSENT_DEMO,
      emailMarketingEnabled: false,
      smsMarketingEnabled: false,
    })

    expect(label).toContain("contact you about your feedback")
    expect(label).not.toContain("prefer not to receive offers")
  })

  it("detects when no permissions are enabled", () => {
    expect(
      guestFormConsentHasAnyEnabled({
        ...GUEST_FORM_CONSENT_DEMO,
        emailMarketingEnabled: false,
        smsMarketingEnabled: false,
        feedbackFollowUpEnabled: false,
      })
    ).toBe(false)
  })
})

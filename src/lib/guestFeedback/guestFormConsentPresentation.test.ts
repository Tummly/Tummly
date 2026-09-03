import { describe, expect, it } from "vitest"

import {
  buildGuestFormConsentCheckboxLabel,
  GUEST_FORM_CONSENT_DEMO,
  guestFormConsentHasAnyEnabled,
} from "@/lib/guestFeedback/guestFormConsentPresentation"

describe("guestFormConsentPresentation", () => {
  it("couples email and SMS into one sentence and omits feedback follow-up", () => {
    const label = buildGuestFormConsentCheckboxLabel(
      "Camden Street",
      GUEST_FORM_CONSENT_DEMO
    )

    expect(label).not.toContain("contact you about your feedback")
    expect(label).toBe(
      "Camden Street may send you offers and updates by Email and SMS using the contact details you provide. Untick here if you would prefer not to receive offers."
    )
  })

  it("uses a single channel when only email marketing is enabled", () => {
    const label = buildGuestFormConsentCheckboxLabel("Main", {
      ...GUEST_FORM_CONSENT_DEMO,
      smsMarketingEnabled: false,
    })

    expect(label).toContain("offers and updates by email")
    expect(label).not.toContain("SMS")
    expect(label).toContain("prefer not to receive offers")
  })

  it("returns an empty label when only feedback follow-up is enabled", () => {
    const label = buildGuestFormConsentCheckboxLabel("Main", {
      ...GUEST_FORM_CONSENT_DEMO,
      emailMarketingEnabled: false,
      smsMarketingEnabled: false,
    })

    expect(label).toBe("")
  })

  it("detects when no marketing permissions are enabled", () => {
    expect(
      guestFormConsentHasAnyEnabled({
        ...GUEST_FORM_CONSENT_DEMO,
        emailMarketingEnabled: false,
        smsMarketingEnabled: false,
        feedbackFollowUpEnabled: true,
      })
    ).toBe(false)
  })
})

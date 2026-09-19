import { describe, expect, it } from "vitest"

import {
  buildGuestFormConsentCheckboxLabel,
  buildGuestFormIntroCopy,
  GUEST_FORM_CONSENT_DEMO,
  resolveGuestFormMarketingChannel,
} from "@/lib/guestFeedback/guestFormConsentPresentation"

describe("guestFormConsentPresentation", () => {
  describe("buildGuestFormIntroCopy", () => {
    it("uses the restaurant name in the private-share follow-up notice", () => {
      expect(buildGuestFormIntroCopy("Camden Street")).toBe(
        "Your feedback is shared privately with Camden Street. They may contact you about this feedback using the details you provide."
      )
    })
  })

  describe("resolveGuestFormMarketingChannel", () => {
    it("returns email only after a valid email when Email marketing is enabled", () => {
      expect(
        resolveGuestFormMarketingChannel("alex@example.com", GUEST_FORM_CONSENT_DEMO)
      ).toBe("email")
    })

    it("returns sms only after a valid UK mobile when SMS marketing is enabled", () => {
      expect(
        resolveGuestFormMarketingChannel("07123456789", GUEST_FORM_CONSENT_DEMO)
      ).toBe("sms")
    })

    it("never returns both channels for one contact value", () => {
      const emailChannel = resolveGuestFormMarketingChannel(
        "alex@example.com",
        GUEST_FORM_CONSENT_DEMO
      )
      const smsChannel = resolveGuestFormMarketingChannel(
        "07123456789",
        GUEST_FORM_CONSENT_DEMO
      )
      expect(emailChannel).toBe("email")
      expect(smsChannel).toBe("sms")
      expect(emailChannel).not.toBe(smsChannel)
    })

    it("hides the checkbox until the contact is valid", () => {
      expect(
        resolveGuestFormMarketingChannel("", GUEST_FORM_CONSENT_DEMO)
      ).toBeNull()
      expect(
        resolveGuestFormMarketingChannel("not-an-email", GUEST_FORM_CONSENT_DEMO)
      ).toBeNull()
      expect(
        resolveGuestFormMarketingChannel("123", GUEST_FORM_CONSENT_DEMO)
      ).toBeNull()
    })

    it("hides email checkbox when Email marketing is disabled", () => {
      expect(
        resolveGuestFormMarketingChannel("alex@example.com", {
          ...GUEST_FORM_CONSENT_DEMO,
          emailMarketingEnabled: false,
        })
      ).toBeNull()
    })

    it("hides sms checkbox when SMS marketing is disabled", () => {
      expect(
        resolveGuestFormMarketingChannel("07123456789", {
          ...GUEST_FORM_CONSENT_DEMO,
          smsMarketingEnabled: false,
        })
      ).toBeNull()
    })

    it("returns null when config is missing", () => {
      expect(resolveGuestFormMarketingChannel("alex@example.com", null)).toBeNull()
    })
  })

  describe("buildGuestFormConsentCheckboxLabel", () => {
    it("builds the GF-01 hard opt-in email label", () => {
      expect(buildGuestFormConsentCheckboxLabel("Camden Street", "email")).toBe(
        "Yes, email me occasional offers and updates from Camden Street. You can unsubscribe at any time."
      )
    })

    it("builds the GF-02 hard opt-in SMS label", () => {
      expect(buildGuestFormConsentCheckboxLabel("Camden Street", "sms")).toBe(
        "Yes, text me occasional offers and updates from Camden Street. You can opt out at any time."
      )
    })
  })
})

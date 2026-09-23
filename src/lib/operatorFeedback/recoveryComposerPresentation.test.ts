import { describe, expect, it } from "vitest"

import {
  RECOVERY_COMPOSER_COPY,
  RECOVERY_COMPOSER_ELIGIBILITY_NOTICE_CLASS,
  RECOVERY_COMPOSER_STATUS_BANNER_CLASS,
  buildRecoveryComposerStatusBanner,
  mapRecoveryComposerSendFailure,
  resolveRecoveryComposerMarketingChannel,
  resolveRecoveryComposerMarketingChannelFromDetails,
  shouldDetachOfferOnEligibilityLoss,
} from "./recoveryComposerPresentation"
import {
  emptyFeedbackDetailPermissionStates,
  emptyFeedbackDetailRestaurantPermissions,
} from "./feedbackDetailRecoveryPresentation"

const allRestaurantOn = emptyFeedbackDetailRestaurantPermissions()
allRestaurantOn["email-marketing"] = true
allRestaurantOn["sms-marketing"] = true
allRestaurantOn["feedback-follow-up"] = true

describe("recoveryComposerPresentation", () => {
  it("uses theme-aware Main Bg tokens for status and eligibility banners", () => {
    expect(RECOVERY_COMPOSER_STATUS_BANNER_CLASS).toContain(
      "bg-op-background-primary"
    )
    expect(RECOVERY_COMPOSER_STATUS_BANNER_CLASS).not.toContain(
      "op-color-gray-995"
    )
    expect(RECOVERY_COMPOSER_ELIGIBILITY_NOTICE_CLASS).toContain(
      "bg-op-background-primary"
    )
    expect(RECOVERY_COMPOSER_ELIGIBILITY_NOTICE_CLASS).not.toContain(
      "op-color-gray-995"
    )
  })

  it("RC-01: service-only banner when marketing is not granted", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"

    const channel = resolveRecoveryComposerMarketingChannel({
      contactType: "Email",
      guestContact: "guest@example.com",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(channel).toBeNull()
    expect(buildRecoveryComposerStatusBanner(channel)).toEqual({
      kind: "service-only",
      message: RECOVERY_COMPOSER_COPY.serviceOnlyBanner,
    })
  })

  it("RC-02 email: marketing eligible banner for Email", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"
    permissionStates["email-marketing"] = "granted"

    const channel = resolveRecoveryComposerMarketingChannel({
      contactType: "Email",
      guestContact: "guest@example.com",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(channel).toBe("email")
    expect(buildRecoveryComposerStatusBanner(channel)).toEqual({
      kind: "marketing-eligible",
      message: RECOVERY_COMPOSER_COPY.marketingEligibleEmail,
    })
  })

  it("RC-02 sms: marketing eligible banner for SMS", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"
    permissionStates["sms-marketing"] = "granted"

    const channel = resolveRecoveryComposerMarketingChannel({
      contactType: "Phone",
      guestContact: "07123456789",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(channel).toBe("sms")
    expect(buildRecoveryComposerStatusBanner(channel)).toEqual({
      kind: "marketing-eligible",
      message: RECOVERY_COMPOSER_COPY.marketingEligibleSms,
    })
  })

  it("parses marketing channel from Feedback details wire fields", () => {
    expect(
      resolveRecoveryComposerMarketingChannelFromDetails({
        contactType: "Email",
        guestContact: "guest@example.com",
        permissionStates: {
          "feedback-follow-up": "granted",
          "email-marketing": "granted",
        },
        restaurantPermissionEnabled: {
          "email-marketing": true,
          "sms-marketing": true,
          "feedback-follow-up": true,
        },
      })
    ).toBe("email")
  })

  it("falls back to marketingPreference when ledger wire fields are omitted", () => {
    expect(
      resolveRecoveryComposerMarketingChannelFromDetails({
        contactType: "Email",
        guestContact: "guest@example.com",
        marketingPreference: "allowed",
      })
    ).toBe("email")
  })

  it("RC-03: detaches Offer when marketing eligibility is lost", () => {
    expect(
      shouldDetachOfferOnEligibilityLoss({
        previousChannel: "email",
        nextChannel: null,
        hasAttachedOffer: true,
      })
    ).toBe(true)

    expect(
      shouldDetachOfferOnEligibilityLoss({
        previousChannel: "email",
        nextChannel: "email",
        hasAttachedOffer: true,
      })
    ).toBe(false)

    expect(
      shouldDetachOfferOnEligibilityLoss({
        previousChannel: "email",
        nextChannel: null,
        hasAttachedOffer: false,
      })
    ).toBe(false)
  })

  it("RC-03: maps opted-out send failure to no permission", () => {
    expect(
      mapRecoveryComposerSendFailure(new Error("Guest has opted out of offers."))
    ).toEqual({
      kind: "no_permission",
      message: RECOVERY_COMPOSER_COPY.noPermission,
    })
  })

  it("RC-03: maps provider unavailable failures", () => {
    expect(
      mapRecoveryComposerSendFailure(
        new Error("Billing Reserve is not available. Recovery SMS send stays blocked.")
      )
    ).toEqual({
      kind: "provider_unavailable",
      message: RECOVERY_COMPOSER_COPY.providerUnavailable,
    })
  })

  it("RC-03: maps unknown failures to generic send failed", () => {
    expect(mapRecoveryComposerSendFailure(new Error("boom"))).toEqual({
      kind: "failed",
      message: RECOVERY_COMPOSER_COPY.sendFailed,
    })
  })
})

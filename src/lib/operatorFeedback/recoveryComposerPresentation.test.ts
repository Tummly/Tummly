import { describe, expect, it } from "vitest"

import {
  RECOVERY_COMPOSER_COPY,
  RECOVERY_COMPOSER_ELIGIBILITY_NOTICE_CLASS,
  RECOVERY_COMPOSER_STATUS_BANNER_CLASS,
  buildRecoveryComposerStatusBanner,
  mapRecoveryComposerSendFailure,
  resolveRecoveryComposerMarketingChannel,
  resolveRecoveryComposerMarketingChannelFromDetails,
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

  it("status banner is always service-reply (marketing not required)", () => {
    expect(buildRecoveryComposerStatusBanner()).toEqual({
      kind: "service-only",
      message: RECOVERY_COMPOSER_COPY.serviceReply,
    })
  })

  it("still resolves marketing channel for informational ledger reads", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"
    permissionStates["email-marketing"] = "granted"

    expect(
      resolveRecoveryComposerMarketingChannel({
        contactType: "Email",
        guestContact: "guest@example.com",
        permissionStates,
        restaurantPermissions: allRestaurantOn,
      })
    ).toBe("email")
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

  it("server follow-up denial maps to no_permission", () => {
    expect(
      mapRecoveryComposerSendFailure(
        new Error("Guest has not granted feedback follow-up permission.")
      )
    ).toEqual({
      kind: "no_permission",
      message: RECOVERY_COMPOSER_COPY.noPermission,
    })
  })

  it("maps feedback follow-up permission substring case-insensitively", () => {
    expect(
      mapRecoveryComposerSendFailure(
        new Error("Denied: Feedback Follow-Up Permission required")
      )
    ).toMatchObject({ kind: "no_permission" })
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

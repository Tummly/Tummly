import { describe, expect, it } from "vitest"

import {
  FEEDBACK_DETAIL_RECOVERY_COPY,
  buildFeedbackDetailPermissionSummary,
  deriveFeedbackDetailRecoveryActions,
  emptyFeedbackDetailPermissionStates,
  emptyFeedbackDetailRestaurantPermissions,
  resolveMarketingGrantedChannel,
} from "./feedbackDetailRecoveryPresentation"

const allRestaurantOn = emptyFeedbackDetailRestaurantPermissions()
allRestaurantOn["email-marketing"] = true
allRestaurantOn["sms-marketing"] = true
allRestaurantOn["feedback-follow-up"] = true

describe("feedbackDetailRecoveryPresentation", () => {
  it("FD-01: follow-up Available + Marketing Not granted; Respond on, Add Offer off", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"

    const summary = buildFeedbackDetailPermissionSummary({
      contactType: "Email",
      guestContact: "guest@example.com",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(summary).toEqual([
      {
        id: "feedback-follow-up",
        label: "Feedback follow-up",
        value: "Available",
      },
      {
        id: "marketing",
        label: "Marketing",
        value: "Not granted",
      },
    ])

    const actions = deriveFeedbackDetailRecoveryActions({
      contactType: "Email",
      guestContact: "guest@example.com",
      workflowStatus: "in_progress",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(actions).toEqual({
      respondEnabled: true,
      respondDisableReason: null,
      addOfferEnabled: false,
      addOfferHelper: FEEDBACK_DETAIL_RECOVERY_COPY.marketingNotAvailableHelper,
    })
  })

  it("FD-02 email: shows Email marketing Granted and enables Add Offer", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"
    permissionStates["email-marketing"] = "granted"

    const summary = buildFeedbackDetailPermissionSummary({
      contactType: "Email",
      guestContact: "guest@example.com",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(summary).toEqual([
      {
        id: "feedback-follow-up",
        label: "Feedback follow-up",
        value: "Available",
      },
      {
        id: "email-marketing",
        label: "Email marketing",
        value: "Granted",
      },
    ])

    const actions = deriveFeedbackDetailRecoveryActions({
      contactType: "Email",
      guestContact: "guest@example.com",
      workflowStatus: "new",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(actions).toMatchObject({
      respondEnabled: true,
      addOfferEnabled: true,
      addOfferHelper: null,
    })
  })

  it("FD-02 sms: shows SMS marketing Granted for phone contact", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"
    permissionStates["sms-marketing"] = "granted"

    expect(
      resolveMarketingGrantedChannel({
        contactType: "Phone",
        guestContact: "07123456789",
        permissionStates,
        restaurantPermissions: allRestaurantOn,
      })
    ).toBe("sms-marketing")

    const summary = buildFeedbackDetailPermissionSummary({
      contactType: "Phone",
      guestContact: "07123456789",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(summary.map((row) => row.id)).toEqual([
      "feedback-follow-up",
      "sms-marketing",
    ])
  })

  it("does not treat SMS grant as marketing eligible for an email contact", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"
    permissionStates["sms-marketing"] = "granted"

    expect(
      resolveMarketingGrantedChannel({
        contactType: "Email",
        guestContact: "guest@example.com",
        permissionStates,
        restaurantPermissions: allRestaurantOn,
      })
    ).toBeNull()

    const actions = deriveFeedbackDetailRecoveryActions({
      contactType: "Email",
      guestContact: "guest@example.com",
      workflowStatus: "in_progress",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(actions.addOfferEnabled).toBe(false)
  })

  it("FD-03: disables Respond when no valid contact", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"

    const actions = deriveFeedbackDetailRecoveryActions({
      contactType: "Unknown",
      guestContact: "",
      workflowStatus: "in_progress",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(actions).toMatchObject({
      respondEnabled: false,
      respondDisableReason: FEEDBACK_DETAIL_RECOVERY_COPY.noContactReason,
      addOfferEnabled: false,
    })
  })

  it("FD-03: disables Respond when follow-up is withdrawn", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "withdrawn"
    permissionStates["email-marketing"] = "granted"

    const actions = deriveFeedbackDetailRecoveryActions({
      contactType: "Email",
      guestContact: "guest@example.com",
      workflowStatus: "in_progress",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(actions).toMatchObject({
      respondEnabled: false,
      respondDisableReason:
        FEEDBACK_DETAIL_RECOVERY_COPY.followUpUnavailableReason,
      addOfferEnabled: false,
    })
  })

  it("FD-03: disables Respond when restaurant feedback follow-up is off", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"
    const restaurantPermissions = emptyFeedbackDetailRestaurantPermissions()
    restaurantPermissions["feedback-follow-up"] = false
    restaurantPermissions["email-marketing"] = true

    const actions = deriveFeedbackDetailRecoveryActions({
      contactType: "Email",
      guestContact: "guest@example.com",
      workflowStatus: "in_progress",
      permissionStates,
      restaurantPermissions,
    })

    expect(actions.respondEnabled).toBe(false)
    expect(actions.respondDisableReason).toBe(
      FEEDBACK_DETAIL_RECOVERY_COPY.followUpUnavailableReason
    )
  })

  it("FD-03: disables Respond when account is restricted", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"

    const actions = deriveFeedbackDetailRecoveryActions({
      contactType: "Email",
      guestContact: "guest@example.com",
      workflowStatus: "in_progress",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
      accountRestricted: true,
    })

    expect(actions).toMatchObject({
      respondEnabled: false,
      respondDisableReason:
        FEEDBACK_DETAIL_RECOVERY_COPY.accountRestrictedReason,
    })
  })
})

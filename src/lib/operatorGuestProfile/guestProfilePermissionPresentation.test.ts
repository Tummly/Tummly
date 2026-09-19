import { describe, expect, it } from "vitest"

import {
  GUEST_PROFILE_PERMISSION_COPY,
  buildGuestProfilePermissionSummary,
} from "./guestProfilePermissionPresentation"
import {
  emptyFeedbackDetailPermissionStates,
  emptyFeedbackDetailRestaurantPermissions,
} from "@/lib/operatorFeedback/feedbackDetailRecoveryPresentation"

const allRestaurantOn = emptyFeedbackDetailRestaurantPermissions()
allRestaurantOn["email-marketing"] = true
allRestaurantOn["sms-marketing"] = true
allRestaurantOn["feedback-follow-up"] = true

describe("guestProfilePermissionPresentation", () => {
  it("shows three separate rows; follow-up only is not Granted marketing", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"

    const rows = buildGuestProfilePermissionSummary({
      email: "guest@example.com",
      mobile: null,
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(rows).toEqual([
      {
        id: "feedback-follow-up",
        label: GUEST_PROFILE_PERMISSION_COPY.feedbackFollowUp,
        state: "available",
        value: "Available",
      },
      {
        id: "email-marketing",
        label: GUEST_PROFILE_PERMISSION_COPY.emailMarketing,
        state: "not_granted",
        value: "Not granted",
      },
      {
        id: "sms-marketing",
        label: GUEST_PROFILE_PERMISSION_COPY.smsMarketing,
        state: "invalid_contact",
        value: "Invalid contact",
      },
    ])
  })

  it("shows Granted for channel marketing with contact", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["feedback-follow-up"] = "granted"
    permissionStates["email-marketing"] = "granted"

    const rows = buildGuestProfilePermissionSummary({
      email: "guest@example.com",
      mobile: "07123456789",
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(rows.find((row) => row.id === "email-marketing")).toMatchObject({
      state: "granted",
      value: "Granted",
    })
    expect(rows.find((row) => row.id === "sms-marketing")).toMatchObject({
      state: "not_granted",
      value: "Not granted",
    })
  })

  it("shows Withdrawn when ledger is withdrawn", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["email-marketing"] = "withdrawn"

    const rows = buildGuestProfilePermissionSummary({
      email: "guest@example.com",
      mobile: null,
      permissionStates,
      restaurantPermissions: allRestaurantOn,
    })

    expect(rows.find((row) => row.id === "email-marketing")).toMatchObject({
      state: "withdrawn",
      value: "Withdrawn",
    })
  })

  it("shows Suppressed when restaurant marketing is off", () => {
    const permissionStates = emptyFeedbackDetailPermissionStates()
    permissionStates["email-marketing"] = "granted"
    const restaurant = emptyFeedbackDetailRestaurantPermissions()
    restaurant["feedback-follow-up"] = true
    restaurant["email-marketing"] = false
    restaurant["sms-marketing"] = true

    const rows = buildGuestProfilePermissionSummary({
      email: "guest@example.com",
      mobile: null,
      permissionStates,
      restaurantPermissions: restaurant,
    })

    expect(rows.find((row) => row.id === "email-marketing")).toMatchObject({
      state: "suppressed",
      value: "Suppressed",
    })
  })
})

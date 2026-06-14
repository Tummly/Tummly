import { describe, expect, it } from "vitest"

import { validationMessages } from "@/schemas/messages"
import {
  accountSetupSingleDefaultValues,
  accountSetupSingleSchema,
  accountSetupSingleStep1Fields,
  accountSetupSingleStep2Fields,
  accountSetupSingleStep3Fields,
  toSingleLocationSetupPayload,
} from "@/schemas/accountSetupSingle"

const validAccountSetup = {
  ...accountSetupSingleDefaultValues,
  token: "setup-token",
  email: "operator@example.com",
  fullName: "Alex Operator",
  password: "secure-pass",
  confirmPassword: "secure-pass",
  agree: true,
  restaurantName: "The Golden Fork",
  locationName: "Main Street",
  address: "1 High Street",
  postcode: "AB1 2CD",
  phone: "07700900123",
  businessLink: "https://example.com",
  businessCategory: "Restaurant",
  thankYouMessage: "Thanks for your feedback!",
  offerHeadline: "10% off",
  offerDetails: "Your next visit",
  offerExpiry: "2026-12-31",
  offerRedemption: "Show at counter",
  offerUsageLimit: "Once per guest",
}

describe("accountSetupSingleSchema", () => {
  it("accepts a valid single-location setup form", () => {
    const result = accountSetupSingleSchema.safeParse(validAccountSetup)
    expect(result.success).toBe(true)
  })

  it("rejects unchecked terms on step 1", () => {
    const result = accountSetupSingleSchema.safeParse({
      ...validAccountSetup,
      agree: false,
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      const agreeIssue = result.error.issues.find(
        (issue) => issue.path[0] === "agree"
      )
      expect(agreeIssue?.message).toBe(
        validationMessages.accountSetup.terms.required
      )
    }
  })

  it("rejects mismatched passwords on step 1", () => {
    const result = accountSetupSingleSchema.safeParse({
      ...validAccountSetup,
      confirmPassword: "different-pass",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      const confirmIssue = result.error.issues.find(
        (issue) => issue.path[0] === "confirmPassword"
      )
      expect(confirmIssue?.message).toBe(validationMessages.password.mismatch)
    }
  })

  it("rejects missing restaurant details on step 2", () => {
    const result = accountSetupSingleSchema.safeParse({
      ...validAccountSetup,
      restaurantName: "",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      const issue = result.error.issues.find(
        (issue) => issue.path[0] === "restaurantName"
      )
      expect(issue?.message).toBe(
        validationMessages.accountSetup.restaurantName.required
      )
    }
  })

  it("rejects an invalid phone number on step 2", () => {
    const result = accountSetupSingleSchema.safeParse({
      ...validAccountSetup,
      phone: "123",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      const issue = result.error.issues.find((issue) => issue.path[0] === "phone")
      expect(issue?.message).toBe(validationMessages.accountSetup.phone.invalid)
    }
  })

  it("rejects a missing thank-you message on step 3", () => {
    const result = accountSetupSingleSchema.safeParse({
      ...validAccountSetup,
      thankYouMessage: "",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      const issue = result.error.issues.find(
        (issue) => issue.path[0] === "thankYouMessage"
      )
      expect(issue?.message).toBe(
        validationMessages.accountSetup.thankYouMessage.required
      )
    }
  })
})

describe("account setup step field slices", () => {
  it("defines step 1 account fields", () => {
    expect(accountSetupSingleStep1Fields).toEqual([
      "email",
      "fullName",
      "password",
      "confirmPassword",
      "agree",
    ])
  })

  it("defines step 2 restaurant fields", () => {
    expect(accountSetupSingleStep2Fields).toContain("restaurantName")
    expect(accountSetupSingleStep2Fields).toContain("businessCategory")
  })

  it("defines step 3 guest-loop required fields", () => {
    expect(accountSetupSingleStep3Fields).toEqual(["thankYouMessage"])
  })
})

describe("toSingleLocationSetupPayload", () => {
  it("maps form values to the complete-setup API DTO shape", () => {
    const payload = toSingleLocationSetupPayload(validAccountSetup)

    expect(payload).toEqual({
      token: "setup-token",
      password: "secure-pass",
      confirmPassword: "secure-pass",
      groupName: "The Golden Fork",
      businessCategory: "Restaurant",
      primaryPhone: "07700900123",
      businessLink: "https://example.com",
      locations: [
        {
          locationName: "Main Street",
          address: "1 High Street",
          postcode: "AB1 2CD",
          locationPhone: "07700900123",
          localContact: "Alex Operator",
          includeInRollout: true,
        },
      ],
      rolloutApproach: "Single",
      guestPrompt: "Please leave feedback",
      thankYouMessage: "Thanks for your feedback!",
      offerType: "Single",
      offerTitle: "10% off",
      offerMessage: "Your next visit",
      offerExpiry: "2026-12-31",
      redemptionMethod: "Show at counter",
      usageLimit: "Once per guest",
    })
  })
})

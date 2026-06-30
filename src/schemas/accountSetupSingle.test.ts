import { describe, expect, it } from "vitest"

import { validationMessages } from "@/schemas/messages"
import {
  accountSetupSingleDefaultValues,
  accountSetupSingleSchema,
  accountSetupSingleStep1Fields,
  accountSetupSingleStep2Fields,
  accountSetupSingleStep2Schema,
  toSingleLocationSetupPayload,
} from "@/schemas/accountSetupSingle"

const validAccountSetup = {
  ...accountSetupSingleDefaultValues,
  token: "setup-token",
  email: "operator@example.com",
  fullName: "Alex Operator",
  password: "Password1",
  confirmPassword: "Password1",
  agree: true,
  restaurantName: "The Golden Fork",
  locationName: "Main Street",
  address: "1 High Street",
  postcode: "SW1A 1AA",
  phone: "07911123456",
  businessLink: "https://example.com",
  businessCategory: "takeaway",
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
      confirmPassword: "different-pass-12",
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

  it("rejects an invalid UK postcode on step 2", () => {
    const result = accountSetupSingleStep2Schema.safeParse({
      restaurantName: "The Golden Fork",
      locationName: "Main Street",
      address: "1 High Street",
      postcode: "not-a-postcode",
      phone: "07911123456",
      businessLink: "",
      businessCategory: "takeaway",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      const issue = result.error.issues.find((entry) => entry.path[0] === "postcode")
      expect(issue?.message).toBe(
        validationMessages.accountSetup.postcode.invalid
      )
    }
  })

  it("accepts setup without restaurant phone", () => {
    const result = accountSetupSingleSchema.safeParse({
      ...validAccountSetup,
      phone: "",
    })
    expect(result.success).toBe(true)
  })

  it("rejects an invalid phone number when provided", () => {
    const result = accountSetupSingleSchema.safeParse({
      ...validAccountSetup,
      phone: "123",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      const issue = result.error.issues.find((issue) => issue.path[0] === "phone")
      expect(issue?.message).toBe(validationMessages.mobile.invalid)
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
    expect(accountSetupSingleStep2Fields).toContain("phone")
  })
})

describe("toSingleLocationSetupPayload", () => {
  it("maps form values to the complete-setup API DTO shape", () => {
    const payload = toSingleLocationSetupPayload(validAccountSetup)

    expect(payload).toEqual({
      token: "setup-token",
      fullName: "Alex Operator",
      password: "Password1",
      confirmPassword: "Password1",
      groupName: "The Golden Fork",
      businessCategory: "takeaway",
      primaryPhone: "+447911123456",
      businessLink: "https://example.com",
      locations: [
        {
          locationName: "Main Street",
          address: "1 High Street",
          postcode: "SW1A 1AA",
          locationPhone: "+447911123456",
          localContact: "Alex Operator",
        },
      ],
    })
  })

  it("includes addressOverridden when the operator kept manual text", () => {
    const payload = toSingleLocationSetupPayload({
      ...validAccountSetup,
      addressOverridden: true,
    })

    expect(payload.locations[0]?.addressOverridden).toBe(true)
  })

  it("omits phone fields from the payload when empty", () => {
    const payload = toSingleLocationSetupPayload({
      ...validAccountSetup,
      phone: "",
    })

    expect(payload.primaryPhone).toBeUndefined()
    expect(payload.locations[0]?.locationPhone).toBeUndefined()
  })
})

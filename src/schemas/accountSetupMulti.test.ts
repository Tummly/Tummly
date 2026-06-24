import { describe, expect, it } from "vitest"

import { validationMessages } from "@/schemas/messages"
import {
  accountSetupMultiDefaultValues,
  accountSetupMultiSchema,
  accountSetupMultiStep1Fields,
  accountSetupMultiStep1Schema,
  accountSetupMultiStep2Fields,
  accountSetupMultiStep2Schema,
  accountSetupMultiStep3Schema,
  emptyLocationItem,
  getAccountSetupMultiStep3FieldNames,
  toMultiLocationSetupPayload,
} from "@/schemas/accountSetupMulti"

const validAccountSetup = {
  ...accountSetupMultiDefaultValues,
  token: "setup-token",
  email: "operator@example.com",
  fullName: "Alex Operator",
  password: "secure-pass-12",
  confirmPassword: "secure-pass-12",
  agree: true,
  groupName: "Golden Fork Group",
  businessCategory: "multi-site",
  numLocations: "2-5",
  primaryPhone: "07911123456",
  locations: [
    {
      ...emptyLocationItem,
      locationName: "Main Street",
      address: "1 High Street",
      postcode: "AB1 2CD",
    },
    {
      ...emptyLocationItem,
      locationName: "Harbour Side",
      address: "2 Pier Road",
      postcode: "CD3 4EF",
    },
  ],
}

describe("accountSetupMultiSchema", () => {
  it("accepts a valid multi-location setup form", () => {
    const result = accountSetupMultiSchema.safeParse(validAccountSetup)
    expect(result.success).toBe(true)
  })

  it("rejects a location row missing required fields", () => {
    const result = accountSetupMultiSchema.safeParse({
      ...validAccountSetup,
      locations: [
        {
          ...emptyLocationItem,
          locationName: "",
          address: "1 High Street",
          postcode: "AB1 2CD",
        },
      ],
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      const issue = result.error.issues.find(
        (entry) => entry.path.join(".") === "locations.0.locationName"
      )
      expect(issue?.message).toBe(
        validationMessages.accountSetup.locationName.required
      )
    }
  })

  it("requires number of locations on step 2", () => {
    const result = accountSetupMultiStep2Schema.safeParse({
      groupName: "Golden Fork Group",
      businessCategory: "multi-site",
      numLocations: "",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.accountSetup.numLocations.required
      )
    }
  })
})

describe("account setup multi step field slices", () => {
  it("defines step 1 account fields", () => {
    expect(accountSetupMultiStep1Fields).toContain("agree")
  })

  it("defines step 2 group fields including number of locations", () => {
    expect(accountSetupMultiStep2Fields).toEqual([
      "groupName",
      "businessCategory",
      "numLocations",
    ])
  })

  it("builds per-location field names for step 3", () => {
    expect(getAccountSetupMultiStep3FieldNames(2)).toEqual([
      "locations.0.locationName",
      "locations.0.address",
      "locations.0.postcode",
      "locations.1.locationName",
      "locations.1.address",
      "locations.1.postcode",
    ])
  })

  it("validates step 3 location rows", () => {
    const result = accountSetupMultiStep3Schema.safeParse({
      locations: validAccountSetup.locations,
    })
    expect(result.success).toBe(true)
  })

  it("validates step 1 password match", () => {
    const result = accountSetupMultiStep1Schema.safeParse({
      email: validAccountSetup.email,
      fullName: validAccountSetup.fullName,
      password: validAccountSetup.password,
      confirmPassword: "different-password",
      agree: true,
    })
    expect(result.success).toBe(false)
  })
})

describe("toMultiLocationSetupPayload", () => {
  it("maps form values to the slim complete-setup API DTO shape", () => {
    const payload = toMultiLocationSetupPayload(validAccountSetup)

    expect(payload).toEqual({
      token: "setup-token",
      fullName: "Alex Operator",
      password: "secure-pass-12",
      confirmPassword: "secure-pass-12",
      groupName: "Golden Fork Group",
      businessCategory: "multi-site",
      primaryPhone: "+447911123456",
      businessLink: undefined,
      locations: [
        {
          locationName: "Main Street",
          address: "1 High Street",
          postcode: "AB1 2CD",
          locationPhone: undefined,
          localContact: undefined,
        },
        {
          locationName: "Harbour Side",
          address: "2 Pier Road",
          postcode: "CD3 4EF",
          locationPhone: undefined,
          localContact: undefined,
        },
      ],
    })
  })

  it("does not include rollout configuration fields", () => {
    const payload = toMultiLocationSetupPayload(validAccountSetup)

    expect(payload).not.toHaveProperty("guestPrompt")
    expect(payload).not.toHaveProperty("thankYouMessage")
    expect(payload).not.toHaveProperty("offerType")
  })
})

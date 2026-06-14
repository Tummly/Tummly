import { describe, expect, it } from "vitest"

import { validationMessages } from "@/schemas/messages"
import {
  accountSetupMultiDefaultValues,
  accountSetupMultiSchema,
  accountSetupMultiStep1Fields,
  accountSetupMultiStep2Fields,
  accountSetupMultiStep4Fields,
  emptyLocationItem,
  getAccountSetupMultiStep3FieldNames,
  toMultiLocationSetupPayload,
} from "@/schemas/accountSetupMulti"

const validAccountSetup = {
  ...accountSetupMultiDefaultValues,
  token: "setup-token",
  email: "operator@example.com",
  fullName: "Alex Operator",
  password: "secure-pass",
  confirmPassword: "secure-pass",
  agree: true,
  groupName: "Golden Fork Group",
  businessCategory: "Restaurant",
  locations: [
    {
      ...emptyLocationItem,
      locationName: "Main Street",
      address: "1 High Street",
      postcode: "AB1 2CD",
      includeInRollout: true,
    },
    {
      ...emptyLocationItem,
      locationName: "Harbour Side",
      address: "2 Pier Road",
      postcode: "CD3 4EF",
      includeInRollout: false,
    },
  ],
  rolloutApproach: "qr",
  thankYouMessage: "Thanks for your feedback!",
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

  it("requires offer details only when offer type is selected", () => {
    const withoutOffer = accountSetupMultiSchema.safeParse(validAccountSetup)
    expect(withoutOffer.success).toBe(true)

    const withIncompleteOffer = accountSetupMultiSchema.safeParse({
      ...validAccountSetup,
      offerType: "discount",
      offerTitle: "",
      expiry: "",
      redemptionMethod: "",
      usageLimit: "",
    })
    expect(withIncompleteOffer.success).toBe(false)
    if (!withIncompleteOffer.success) {
      expect(
        withIncompleteOffer.error.issues.some(
          (entry) => entry.path[0] === "offerTitle"
        )
      ).toBe(true)
      expect(
        withIncompleteOffer.error.issues.some((entry) => entry.path[0] === "expiry")
      ).toBe(true)
    }
  })

  it("accepts a complete offer configuration", () => {
    const result = accountSetupMultiSchema.safeParse({
      ...validAccountSetup,
      offerType: "discount",
      offerTitle: "10% off",
      expiry: "30days",
      redemptionMethod: "showStaff",
      usageLimit: "one",
    })
    expect(result.success).toBe(true)
  })
})

describe("account setup multi step field slices", () => {
  it("defines step 1 account fields", () => {
    expect(accountSetupMultiStep1Fields).toContain("agree")
  })

  it("defines step 2 group fields", () => {
    expect(accountSetupMultiStep2Fields).toEqual([
      "groupName",
      "businessCategory",
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

  it("defines step 4 rollout fields", () => {
    expect(accountSetupMultiStep4Fields).toEqual(["rolloutApproach"])
  })
})

describe("toMultiLocationSetupPayload", () => {
  it("maps form values to the complete-setup API DTO shape", () => {
    const payload = toMultiLocationSetupPayload(validAccountSetup)

    expect(payload).toEqual({
      token: "setup-token",
      password: "secure-pass",
      confirmPassword: "secure-pass",
      groupName: "Golden Fork Group",
      businessCategory: "Restaurant",
      primaryPhone: undefined,
      businessLink: undefined,
      locations: [
        {
          locationName: "Main Street",
          address: "1 High Street",
          postcode: "AB1 2CD",
          locationPhone: undefined,
          localContact: undefined,
          includeInRollout: true,
        },
        {
          locationName: "Harbour Side",
          address: "2 Pier Road",
          postcode: "CD3 4EF",
          locationPhone: undefined,
          localContact: undefined,
          includeInRollout: false,
        },
      ],
      rolloutApproach: "qr",
      guestPrompt: undefined,
      thankYouMessage: "Thanks for your feedback!",
      offerType: undefined,
      offerTitle: undefined,
      offerMessage: undefined,
      offerExpiry: undefined,
      redemptionMethod: undefined,
      usageLimit: undefined,
    })
  })
})

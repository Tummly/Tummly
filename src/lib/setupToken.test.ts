import { describe, expect, it } from "vitest"

import {
  getSetupAccountPath,
  parseValidateSetupTokenResponse,
} from "@/lib/setupToken"

describe("parseValidateSetupTokenResponse", () => {
  it("reads camelCase trial validation payloads", () => {
    const parsed = parseValidateSetupTokenResponse({
      success: true,
      data: {
        email: "owner@example.com",
        fullName: "Alex Owner",
        businessName: "The Golden Fork",
        accountType: "Single",
      },
    })

    expect(parsed).toEqual({
      email: "owner@example.com",
      fullName: "Alex Owner",
      businessName: "The Golden Fork",
      accountType: "Single",
    })
  })

  it("reads PascalCase trial validation payloads", () => {
    const parsed = parseValidateSetupTokenResponse({
      Success: true,
      Data: {
        Email: "owner@example.com",
        FullName: "Alex Owner",
        BusinessName: "The Golden Fork",
        AccountType: "Multi",
      },
    })

    expect(parsed?.accountType).toBe("Multi")
    expect(parsed?.email).toBe("owner@example.com")
  })

  it("returns null when the payload is missing account type", () => {
    expect(
      parseValidateSetupTokenResponse({
        success: true,
        data: {
          email: "owner@example.com",
        },
      })
    ).toBeNull()
  })
})

describe("getSetupAccountPath", () => {
  it("builds encoded setup routes", () => {
    expect(getSetupAccountPath("Single", "abc 123")).toBe(
      "/setup-account-single?token=abc%20123"
    )
    expect(getSetupAccountPath("Multi", "token-id")).toBe(
      "/setup-account-multi?token=token-id"
    )
  })
})

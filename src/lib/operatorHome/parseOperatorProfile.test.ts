import { describe, expect, it } from "vitest"

import { parseOperatorProfile } from "./parseOperatorProfile"

describe("parseOperatorProfile", () => {
  it("reads display name and Activation expiry from the wrapped /me response", () => {
    expect(
      parseOperatorProfile({
        success: true,
        data: {
          fullName: "Mohamed Mahmoud",
          email: "mohamed@example.com",
          accountType: "Multi",
          activationExpiresAt: "2026-07-26T12:00:00.000Z",
        },
      })
    ).toEqual({
      fullName: "Mohamed Mahmoud",
      activationExpiresAt: "2026-07-26T12:00:00.000Z",
    })
  })

  it("returns null when fullName is missing", () => {
    expect(
      parseOperatorProfile({
        success: true,
        data: {
          accountType: "Single",
          activationExpiresAt: "2026-07-26T12:00:00.000Z",
        },
      })
    ).toBeNull()
  })

  it("allows a null Activation expiry (badge fallback hides)", () => {
    expect(
      parseOperatorProfile({
        success: true,
        data: {
          fullName: "Alex Operator",
          accountType: "Single",
          activationExpiresAt: null,
        },
      })
    ).toEqual({
      fullName: "Alex Operator",
      activationExpiresAt: null,
    })
  })
})

import { describe, expect, it } from "vitest"

import { parseOperatorProfile } from "./parseOperatorProfile"

describe("parseOperatorProfile", () => {
  it("reads display name, Activation expiry, and Self role from the wrapped /me response", () => {
    expect(
      parseOperatorProfile({
        success: true,
        data: {
          fullName: "Mohamed Mahmoud",
          email: "mohamed@example.com",
          accountType: "Multi",
          activationExpiresAt: "2026-07-26T12:00:00.000Z",
          selfRole: "owner-operator",
          role: "Owner",
        },
      })
    ).toEqual({
      fullName: "Mohamed Mahmoud",
      email: "mohamed@example.com",
      activationExpiresAt: "2026-07-26T12:00:00.000Z",
      selfRole: "owner-operator",
      teamPermissionsAccess: "manage",
    })
  })

  it("hides Team & permissions only when /me sends none", () => {
    expect(
      parseOperatorProfile({
        success: true,
        data: {
          fullName: "Salman Shahid",
          email: "owner@example.com",
          role: "Owner",
          teamPermissionsAccess: "none",
        },
      })?.teamPermissionsAccess
    ).toBe("none")
  })

  it("reads view and manage from /me", () => {
    expect(
      parseOperatorProfile({
        success: true,
        data: { fullName: "Alex", teamPermissionsAccess: "view" },
      })?.teamPermissionsAccess
    ).toBe("view")
    expect(
      parseOperatorProfile({
        success: true,
        data: { fullName: "Alex", teamPermissionsAccess: "manage" },
      })?.teamPermissionsAccess
    ).toBe("manage")
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
      email: null,
      activationExpiresAt: null,
      selfRole: null,
      teamPermissionsAccess: "manage",
    })
  })

  it("treats missing Self role as null without reading permission role", () => {
    expect(
      parseOperatorProfile({
        success: true,
        data: {
          fullName: "Alex Operator",
          role: "Owner",
          activationExpiresAt: null,
        },
      })
    ).toEqual({
      fullName: "Alex Operator",
      email: null,
      activationExpiresAt: null,
      selfRole: null,
      teamPermissionsAccess: "manage",
    })
  })
})

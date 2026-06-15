import { describe, expect, it } from "vitest"

import {
  getPostVerifyDashboardPath,
  parseVerifyOtpResponse,
} from "./authHelpers"

describe("parseVerifyOtpResponse", () => {
  it("reads token and accountType from the wrapped API envelope", () => {
    expect(
      parseVerifyOtpResponse({
        success: true,
        message: "OTP verified successfully.",
        data: {
          token: "jwt-token",
          accountType: "Single",
        },
      })
    ).toEqual({
      token: "jwt-token",
      accountType: "Single",
    })
  })

  it("reads token and accountType from an unwrapped payload", () => {
    expect(
      parseVerifyOtpResponse({
        token: "jwt-token",
        accountType: "Multi",
      })
    ).toEqual({
      token: "jwt-token",
      accountType: "Multi",
    })
  })

  it("returns null when required fields are missing", () => {
    expect(parseVerifyOtpResponse({ success: true, data: {} })).toBeNull()
    expect(parseVerifyOtpResponse(null)).toBeNull()
  })
})

describe("getPostVerifyDashboardPath", () => {
  it("routes Single accounts to single-dashboard", () => {
    expect(getPostVerifyDashboardPath("Single")).toBe("/single-dashboard")
  })

  it("routes Multi accounts to multi-dashboard", () => {
    expect(getPostVerifyDashboardPath("Multi")).toBe("/multi-dashboard")
  })
})

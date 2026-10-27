import { beforeEach, describe, expect, it } from "vitest"
import {
  buildSignupVerifyPath,
  clearSignupSessionToken,
  isSignupSessionExpiredError,
  isSignupSessionExpiredState,
  readSignupSessionToken,
  SIGNUP_SESSION_EXPIRED_STATE,
  saveSignupSessionToken,
} from "./signupSession"

describe("buildSignupVerifyPath", () => {
  it("encodes email", () => {
    expect(buildSignupVerifyPath("A@B.com")).toBe(
      "/signup/verify?email=A%40B.com"
    )
  })
})

describe("signupSessionToken", () => {
  beforeEach(() => {
    clearSignupSessionToken()
  })

  it("saves, reads, and clears the session token", () => {
    expect(readSignupSessionToken()).toBeNull()

    saveSignupSessionToken("token-abc")
    expect(readSignupSessionToken()).toBe("token-abc")

    clearSignupSessionToken()
    expect(readSignupSessionToken()).toBeNull()
  })
})

describe("isSignupSessionExpiredError", () => {
  it("is true only for the signup_session_expired code", () => {
    expect(
      isSignupSessionExpiredError(
        Object.assign(new Error("expired"), { code: "signup_session_expired" })
      )
    ).toBe(true)
    expect(
      isSignupSessionExpiredError(
        Object.assign(new Error("invalid"), { code: "otp_invalid" })
      )
    ).toBe(false)
    expect(isSignupSessionExpiredError(new Error("network"))).toBe(false)
    expect(isSignupSessionExpiredError(null)).toBe(false)
  })
})

describe("signupSessionExpiredState", () => {
  it("is set only when the expired flag is present", () => {
    expect(isSignupSessionExpiredState(SIGNUP_SESSION_EXPIRED_STATE)).toBe(true)
    expect(isSignupSessionExpiredState({ signupSessionExpired: false })).toBe(
      false
    )
    expect(isSignupSessionExpiredState(null)).toBe(false)
  })
})

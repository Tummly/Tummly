import { beforeEach, describe, expect, it } from "vitest"
import {
  buildSignupVerifyPath,
  clearSignupPaidIntent,
  clearSignupSessionToken,
  markSignupPaidIntent,
  readSignupPaidIntent,
  readSignupSessionToken,
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

describe("signupPaidIntent", () => {
  beforeEach(() => {
    clearSignupPaidIntent()
  })

  it("marks and reads paid intent", () => {
    expect(readSignupPaidIntent()).toBe(false)
    markSignupPaidIntent()
    expect(readSignupPaidIntent()).toBe(true)
    clearSignupPaidIntent()
    expect(readSignupPaidIntent()).toBe(false)
  })

  it("clears paid intent with session token", () => {
    markSignupPaidIntent()
    clearSignupSessionToken()
    expect(readSignupPaidIntent()).toBe(false)
  })
})

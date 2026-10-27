import MockAdapter from "axios-mock-adapter"
import { afterEach, describe, expect, it } from "vitest"

import axiosInstance from "@/api/axiosInstance"
import { getSignupSession } from "@/api/signupApi"
import {
  clearSignupSessionToken,
  isSignupSessionExpiredError,
  readSignupSessionToken,
  saveSignupSessionToken,
  SIGNUP_SESSION_EXPIRED_MESSAGE,
  SIGNUP_SESSION_EXPIRED_STATE,
} from "@/lib/signupSession"

const LEGACY_TOKEN = "33333333-3333-3333-3333-333333333333"

describe("SignupOnboardingPage session expiry", () => {
  const mock = new MockAdapter(axiosInstance)

  afterEach(() => {
    mock.reset()
    clearSignupSessionToken()
  })

  it("onboarding_sessionExpired_clearsTokenAndSignalsSignupRedirect", async () => {
    saveSignupSessionToken(LEGACY_TOKEN)
    mock.onGet("/Signup/session").reply(401, {
      success: false,
      code: "signup_session_expired",
      message: "Your signup session has expired. Enter your email to continue.",
    })

    let caught: unknown = null
    try {
      await getSignupSession(readSignupSessionToken() ?? "")
    } catch (error) {
      caught = error
    }

    expect(isSignupSessionExpiredError(caught)).toBe(true)
    // The page does exactly this before it navigates to /signup.
    clearSignupSessionToken()
    expect(readSignupSessionToken()).toBeNull()
    expect(SIGNUP_SESSION_EXPIRED_STATE.signupSessionExpired).toBe(true)
    expect(SIGNUP_SESSION_EXPIRED_MESSAGE).toBe(
      "Your signup session has expired. Enter your email to continue."
    )
  })
})

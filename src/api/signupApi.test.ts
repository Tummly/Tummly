import MockAdapter from "axios-mock-adapter"
import { afterEach, describe, expect, it } from "vitest"

import axiosInstance from "@/api/axiosInstance"
import {
  getSignupSession,
  resendSignupOtp,
  saveSignupOnboarding,
  startSignup,
  verifySignupOtp,
} from "@/api/signupApi"

const SESSION_TOKEN = "22222222-2222-2222-2222-222222222222"

describe("signupApi", () => {
  const mock = new MockAdapter(axiosInstance)

  afterEach(() => {
    mock.reset()
  })

  it("getSignupSession_sendsHeader_noQueryParam", async () => {
    mock.onGet("/Signup/session").reply(200, {
      success: true,
      data: { email: "owner@example.com", status: "Verified" },
    })

    const session = await getSignupSession(SESSION_TOKEN)

    expect(session.status).toBe("Verified")
    const request = mock.history.get[0]
    expect(request.headers?.["X-Signup-Session"]).toBe(SESSION_TOKEN)
    expect(request.params).toBeUndefined()
  })

  it("getSignupSession_mapsSavedPlanCadenceAndConfig", async () => {
    mock.onGet("/Signup/session").reply(200, {
      success: true,
      data: {
        email: "owner@example.com",
        status: "Verified",
        chosenPlan: "Custom",
        chosenCadence: "annual",
        planConfig: {
          basePlan: "growth",
          locations: 3,
          packs: { qr_10: 2 },
          quoteHash: "0123456789abcdef0123456789abcdef",
        },
      },
    })

    const session = await getSignupSession(SESSION_TOKEN)

    expect(session.chosenPlan).toBe("Custom")
    expect(session.chosenCadence).toBe("annual")
    expect(session.planConfig).toEqual({
      basePlan: "growth",
      locations: 3,
      packs: { qr_10: 2 },
      quoteHash: "0123456789abcdef0123456789abcdef",
    })
  })

  it("getSignupSession_noSavedPlan_returnsNulls", async () => {
    mock.onGet("/Signup/session").reply(200, {
      success: true,
      data: { email: "owner@example.com", status: "Verified" },
    })

    const session = await getSignupSession(SESSION_TOKEN)

    expect(session.chosenPlan).toBeNull()
    expect(session.chosenCadence).toBeNull()
    expect(session.planConfig).toBeNull()
  })

  it("saveSignupOnboarding_sendsHeader_noQueryParam", async () => {
    mock.onPost("/Signup/onboarding").reply(200, {
      success: true,
      data: { email: "", status: "Complete", sessionToken: "" },
    })

    await saveSignupOnboarding(SESSION_TOKEN, {
      password: "x",
      confirmPassword: "x",
      fullName: "Owner",
      groupName: "Group",
      businessCategory: "Cafe",
      locations: [],
    })

    const request = mock.history.post[0]
    expect(request.headers?.["X-Signup-Session"]).toBe(SESSION_TOKEN)
    expect(request.params).toBeUndefined()
  })

  it("startSignup_returnsNoToken", async () => {
    mock.onPost("/Signup/start").reply(200, {
      success: true,
      code: "code_sent",
      message: "If this email can be used, we sent a 6-digit code.",
      data: { email: "owner@example.com", status: "code_sent" },
    })

    const result = await startSignup({
      email: "owner@example.com",
      termsAccepted: true,
    })

    expect(result).toEqual({ email: "owner@example.com", status: "code_sent" })
    expect(result).not.toHaveProperty("sessionToken")
  })

  it("verifySignupOtp_returnsSessionTokenForTheVerifyPageToSave", async () => {
    mock.onPost("/Signup/verify-otp").reply(200, {
      success: true,
      data: {
        sessionToken: SESSION_TOKEN,
        email: "owner@example.com",
        status: "Verified",
      },
    })

    const session = await verifySignupOtp({
      email: "owner@example.com",
      otpCode: "012345",
    })

    expect(session.sessionToken).toBe(SESSION_TOKEN)
    expect(session.status).toBe("Verified")
  })

  it("signupErrors_carryCodeAndRetryAfterUtc", async () => {
    mock.onPost("/Signup/resend-otp").reply(429, {
      success: false,
      code: "challenge_locked",
      message: "Too many attempts for this email. Try again later.",
      retryAfterUtc: "2026-10-27T11:00:00Z",
    })

    await expect(resendSignupOtp("owner@example.com")).rejects.toMatchObject({
      code: "challenge_locked",
      retryAfterUtc: "2026-10-27T11:00:00Z",
    })
  })

  it("getSignupSession_expiredToken_carriesSignupSessionExpiredCode", async () => {
    mock.onGet("/Signup/session").reply(401, {
      success: false,
      code: "signup_session_expired",
      message: "Your signup session has expired. Enter your email to continue.",
    })

    await expect(getSignupSession(SESSION_TOKEN)).rejects.toMatchObject({
      code: "signup_session_expired",
    })
  })
})

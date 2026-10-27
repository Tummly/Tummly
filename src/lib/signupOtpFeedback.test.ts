import { describe, expect, it } from "vitest"

import { mapSignupOtpError } from "./signupOtpFeedback"

function withCode(code: string, extra: Record<string, unknown> = {}) {
  return Object.assign(new Error("server text"), { code, ...extra })
}

describe("mapSignupOtpError", () => {
  it("maps otp_invalid to the wrong-code text", () => {
    expect(mapSignupOtpError(withCode("otp_invalid"))).toEqual({
      kind: "error",
      message: "That code doesn't match. Check your email and try again.",
      showRecoveryLinks: false,
      retryAtLabel: null,
    })
  })

  it("maps otp_expired to the expired text", () => {
    expect(mapSignupOtpError(withCode("otp_expired")).message).toBe(
      "That code has expired. Resend a new code to continue."
    )
  })

  it("maps otp_attempts_exceeded to the request-a-new-code text", () => {
    expect(mapSignupOtpError(withCode("otp_attempts_exceeded")).message).toBe(
      "Too many wrong codes. Request a new code."
    )
  })

  it("maps challenge_locked with the local retry time and recovery links", () => {
    const iso = "2026-10-27T11:00:00Z"
    const feedback = mapSignupOtpError(
      withCode("challenge_locked", { retryAfterUtc: iso })
    )

    expect(feedback.message).toBe(
      "Too many attempts for this email. Try again later."
    )
    expect(feedback.showRecoveryLinks).toBe(true)
    expect(feedback.retryAtLabel).toBe(
      new Date(iso).toLocaleString(undefined, {
        dateStyle: "medium",
        timeStyle: "short",
      })
    )
  })

  it("maps challenge_locked without a retry time to no time label", () => {
    const feedback = mapSignupOtpError(withCode("challenge_locked"))

    expect(feedback.retryAtLabel).toBeNull()
    expect(feedback.showRecoveryLinks).toBe(true)
  })

  it("maps rate_limited to the wait text with recovery links", () => {
    const feedback = mapSignupOtpError(withCode("rate_limited"))

    expect(feedback.message).toBe(
      "Too many attempts. Wait a moment and try again."
    )
    expect(feedback.showRecoveryLinks).toBe(true)
  })

  it("maps resend_cooldown to the wait-before-resend text", () => {
    expect(mapSignupOtpError(withCode("resend_cooldown")).message).toBe(
      "Please wait before requesting another code."
    )
  })

  it("maps code_delivery_failed to the try-again text", () => {
    expect(mapSignupOtpError(withCode("code_delivery_failed")).message).toBe(
      "We could not send the code. Try again in a minute."
    )
  })

  it("maps an unknown or missing code to the generic signup text", () => {
    expect(mapSignupOtpError(withCode("signup_error")).message).toBe(
      "Something went wrong. Try again."
    )
    expect(mapSignupOtpError(new Error("Network Error")).message).toBe(
      "Something went wrong. Try again."
    )
  })
})

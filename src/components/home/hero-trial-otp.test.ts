import { describe, expect, it } from "vitest"

import {
  mapResendApiMessage,
  mapTrialSubmitApiError,
  mapVerifyApiMessage,
  OTP_MESSAGES,
} from "@/components/home/hero-trial-otp"

describe("mapTrialSubmitApiError", () => {
  it("maps email-in-use responses to a field-level error", () => {
    const mapped = mapTrialSubmitApiError("Email already in use.")
    expect(mapped).toEqual({
      kind: "email_in_use",
      message: OTP_MESSAGES.email_in_use,
    })
  })

  it("maps verified-trial responses to already_verified feedback", () => {
    const mapped = mapTrialSubmitApiError("Trial request already verified.")
    expect(mapped).toEqual({
      kind: "already_verified",
      feedback: {
        kind: "info",
        code: "already_verified",
        message: OTP_MESSAGES.already_verified,
      },
    })
  })

  it("maps legacy already-registered responses to already_verified feedback", () => {
    const mapped = mapTrialSubmitApiError("This email is already registered.")
    expect(mapped.kind).toBe("already_verified")
  })

  it("falls back to a root error for unknown failures", () => {
    const mapped = mapTrialSubmitApiError("Server unavailable")
    expect(mapped).toEqual({
      kind: "root",
      message: "Server unavailable",
    })
  })
})

describe("mapVerifyApiMessage", () => {
  it("maps already verified responses to info feedback", () => {
    const feedback = mapVerifyApiMessage("Trial request already verified.")
    expect(feedback).toEqual({
      kind: "info",
      code: "already_verified",
      message: OTP_MESSAGES.already_verified,
    })
  })

  it("maps expired responses to expired feedback", () => {
    const feedback = mapVerifyApiMessage("OTP has expired")
    expect(feedback).toEqual({
      kind: "error",
      code: "expired",
      message: OTP_MESSAGES.expired,
    })
  })

  it("maps attempt-limit responses to too_many_attempts feedback", () => {
    const feedback = mapVerifyApiMessage("Too many attempts")
    expect(feedback).toEqual({
      kind: "error",
      code: "too_many_attempts",
      message: OTP_MESSAGES.too_many_attempts,
    })
  })

  it("falls back to invalid feedback for unknown errors", () => {
    const feedback = mapVerifyApiMessage("Something else went wrong")
    expect(feedback).toEqual({
      kind: "error",
      code: "invalid",
      message: "Something else went wrong",
    })
  })
})

describe("mapResendApiMessage", () => {
  it("maps resend-limit responses to too_many_attempts feedback", () => {
    const feedback = mapResendApiMessage("Resend limit reached")
    expect(feedback).toEqual({
      kind: "error",
      code: "too_many_attempts",
      message: OTP_MESSAGES.too_many_attempts,
    })
  })

  it("falls back to a generic resend error", () => {
    const feedback = mapResendApiMessage("")
    expect(feedback).toEqual({
      kind: "error",
      code: "invalid",
      message: "We couldn't resend the code. Try again shortly.",
    })
  })
})

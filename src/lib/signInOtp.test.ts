import { describe, expect, it } from "vitest"

import {
  getOtpDestinationCopy,
  getOtpResentMessage,
  getOtpSentMessage,
  maskEmail,
  parseOtpChallengeResponse,
  parseSendOtpResponse,
} from "./signInOtp"

describe("maskEmail", () => {
  it("masks the local part of an email address", () => {
    expect(maskEmail("owner@restaurant.com")).toBe("o••••@restaurant.com")
  })
})

describe("getOtpDestinationCopy", () => {
  it("uses masked phone copy for sms channel", () => {
    expect(getOtpDestinationCopy("sms", "owner@restaurant.com", "••••1234")).toBe(
      "••••1234"
    )
  })

  it("falls back when sms channel has no masked phone", () => {
    expect(getOtpDestinationCopy("sms", "owner@restaurant.com", null)).toBe(
      "your phone"
    )
  })

  it("uses masked email copy for email channel", () => {
    expect(
      getOtpDestinationCopy("email", "owner@restaurant.com", "••••1234")
    ).toBe("o••••@restaurant.com")
  })
})

describe("parseOtpChallengeResponse", () => {
  it("reads otp challenge metadata from universal-login", () => {
    expect(
      parseOtpChallengeResponse({
        loginType: "USER",
        otpChannel: "email",
        hasVerifiedPhone: true,
        maskedPhone: "••••1234",
      })
    ).toEqual({
      otpChannel: "email",
      hasVerifiedPhone: true,
      maskedPhone: "••••1234",
    })
  })

  it("returns null when a trust skip token is present", () => {
    expect(
      parseOtpChallengeResponse({
        loginType: "USER",
        token: "jwt-token",
      })
    ).toBeNull()
  })
})

describe("parseSendOtpResponse", () => {
  it("reads skipped resend metadata", () => {
    expect(
      parseSendOtpResponse({
        success: true,
        skipped: true,
        otpChannel: "sms",
        maskedPhone: "••••1234",
        message: "Your current verification code is still valid.",
      })
    ).toEqual({
      skipped: true,
      otpChannel: "sms",
      maskedPhone: "••••1234",
      message: "Your current verification code is still valid.",
    })
  })
})

describe("channel messages", () => {
  it("uses sms-specific sent and resent copy", () => {
    expect(getOtpSentMessage("sms")).toContain("phone")
    expect(getOtpResentMessage("sms")).toContain("phone")
  })
})

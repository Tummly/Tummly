import { afterEach, describe, expect, it } from "vitest"

import {
  OAUTH_OTP_HANDOFF_KEY,
  OAUTH_OTP_HANDOFF_STORAGE_KEY,
  cancelScheduledOAuthOtpHandoffClear,
  clearOAuthOtpHandoff,
  peekOAuthOtpHandoff,
  readOAuthOtpHandoff,
  scheduleOAuthOtpHandoffClearOnLeave,
  writeOAuthOtpHandoff,
} from "./oauthOtpHandoff"

const sample = {
  email: "op@example.com",
  rememberDevice: true,
  challenge: {
    otpChannel: "email" as const,
    hasVerifiedPhone: false,
    maskedPhone: null,
  },
}

describe("readOAuthOtpHandoff", () => {
  it("reads a valid handoff", () => {
    expect(
      readOAuthOtpHandoff({
        [OAUTH_OTP_HANDOFF_KEY]: sample,
      })
    ).toEqual(sample)
  })

  it("returns null when email is missing", () => {
    expect(
      readOAuthOtpHandoff({
        [OAUTH_OTP_HANDOFF_KEY]: {
          email: "  ",
          rememberDevice: true,
          challenge: {
            otpChannel: "sms",
            hasVerifiedPhone: true,
            maskedPhone: "••••1234",
          },
        },
      })
    ).toBeNull()
  })
})

describe("oauth OTP sessionStorage handoff", () => {
  afterEach(() => {
    cancelScheduledOAuthOtpHandoffClear()
    sessionStorage.removeItem(OAUTH_OTP_HANDOFF_STORAGE_KEY)
  })

  it("peek survives without consuming", () => {
    writeOAuthOtpHandoff(sample)
    expect(peekOAuthOtpHandoff()).toEqual(sample)
    expect(peekOAuthOtpHandoff()).toEqual(sample)
    clearOAuthOtpHandoff()
    expect(peekOAuthOtpHandoff()).toBeNull()
  })

  it("leave clear can be cancelled on remount", async () => {
    writeOAuthOtpHandoff(sample)
    scheduleOAuthOtpHandoffClearOnLeave(sessionStorage, 0)
    cancelScheduledOAuthOtpHandoffClear()
    await Promise.resolve()
    expect(peekOAuthOtpHandoff()).toEqual(sample)

    scheduleOAuthOtpHandoffClearOnLeave(sessionStorage, 0)
    await new Promise((resolve) => setTimeout(resolve, 10))
    expect(peekOAuthOtpHandoff()).toBeNull()
  })
})

export type OtpErrorCode = "invalid" | "expired" | "too_many_attempts"

export type OtpInfoCode = "code_resent" | "email_changed" | "already_verified"

export type OtpFeedback =
  | { kind: "error"; code: OtpErrorCode; message: string }
  | { kind: "info"; code: OtpInfoCode; message: string }

export const OTP_LENGTH = 6

export const RESEND_COOLDOWN_SECONDS = 60

export const MAX_VERIFY_ATTEMPTS = 5

export const OTP_MESSAGES = {
  invalid: "That code doesn't match. Check your email and try again.",
  expired: "That code has expired. Resend a new code to continue.",
  too_many_attempts:
    "Too many attempts. Wait a few minutes, then request a new code.",
  code_resent: "A new verification code has been sent to your email.",
  email_changed: "We've sent a new code to your updated email address.",
  already_verified:
    "This request is already verified. We'll be in touch about your setup.",
  incomplete: "Enter the full 6-digit verification code.",
} as const

function includesAny(text: string, phrases: string[]) {
  return phrases.some((phrase) => text.includes(phrase))
}

export function mapVerifyApiMessage(message: string): OtpFeedback {
  const normalized = message.trim().toLowerCase()

  if (
    includesAny(normalized, [
      "already verified",
      "already registered",
      "email already",
    ])
  ) {
    return {
      kind: "info",
      code: "already_verified",
      message: OTP_MESSAGES.already_verified,
    }
  }

  if (includesAny(normalized, ["expired", "expire"])) {
    return {
      kind: "error",
      code: "expired",
      message: OTP_MESSAGES.expired,
    }
  }

  if (
    includesAny(normalized, [
      "too many",
      "limit reached",
      "maximum",
      "attempt",
    ])
  ) {
    return {
      kind: "error",
      code: "too_many_attempts",
      message: OTP_MESSAGES.too_many_attempts,
    }
  }

  if (includesAny(normalized, ["invalid or expired"])) {
    return {
      kind: "error",
      code: "invalid",
      message: OTP_MESSAGES.invalid,
    }
  }

  return {
    kind: "error",
    code: "invalid",
    message: message.trim() || OTP_MESSAGES.invalid,
  }
}

export function mapResendApiMessage(message: string): OtpFeedback {
  const normalized = message.trim().toLowerCase()

  if (
    includesAny(normalized, [
      "limit reached",
      "too many",
      "resend limit",
    ])
  ) {
    return {
      kind: "error",
      code: "too_many_attempts",
      message: OTP_MESSAGES.too_many_attempts,
    }
  }

  return {
    kind: "error",
    code: "invalid",
    message: message.trim() || "We couldn't resend the code. Try again shortly.",
  }
}

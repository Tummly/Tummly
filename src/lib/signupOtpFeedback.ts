/**
 * Signup-only copy for verify-otp and resend-otp errors. Keyed by the server
 * `code` (see the signup error contract). The shared trial and login texts in
 * `components/home/hero-trial-otp.ts` are not used here.
 */

export type SignupOtpFeedback = {
  kind: "error" | "info"
  message: string
  /** Sign in, Forgot password and Contact us links under the message. */
  showRecoveryLinks: boolean
  /** Local time the lock lifts, from `retryAfterUtc`. Null when unknown. */
  retryAtLabel: string | null
}

export const SIGNUP_OTP_MESSAGES = {
  otp_invalid: "That code doesn't match. Check your email and try again.",
  otp_expired: "That code has expired. Resend a new code to continue.",
  otp_attempts_exceeded: "Too many wrong codes. Request a new code.",
  challenge_locked: "Too many attempts for this email. Try again later.",
  rate_limited: "Too many attempts. Wait a moment and try again.",
  resend_cooldown: "Please wait before requesting another code.",
  code_delivery_failed: "We could not send the code. Try again in a minute.",
  signup_error: "Something went wrong. Try again.",
} as const

type SignupOtpErrorFields = { code?: unknown; retryAfterUtc?: unknown }

function isKnownOtpCode(
  code: string
): code is keyof typeof SIGNUP_OTP_MESSAGES {
  return Object.prototype.hasOwnProperty.call(SIGNUP_OTP_MESSAGES, code)
}

function formatRetryAt(value: unknown): string | null {
  if (typeof value !== "string") return null
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return null
  return date.toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  })
}

export function mapSignupOtpError(error: unknown): SignupOtpFeedback {
  const fields = (error ?? {}) as SignupOtpErrorFields
  const code = typeof fields.code === "string" ? fields.code : ""

  if (code === "challenge_locked") {
    return {
      kind: "error",
      message: SIGNUP_OTP_MESSAGES.challenge_locked,
      showRecoveryLinks: true,
      retryAtLabel: formatRetryAt(fields.retryAfterUtc),
    }
  }

  if (code === "rate_limited") {
    return {
      kind: "error",
      message: SIGNUP_OTP_MESSAGES.rate_limited,
      showRecoveryLinks: true,
      retryAtLabel: null,
    }
  }

  const message = isKnownOtpCode(code)
    ? SIGNUP_OTP_MESSAGES[code]
    : SIGNUP_OTP_MESSAGES.signup_error

  return {
    kind: "error",
    message,
    showRecoveryLinks: false,
    retryAtLabel: null,
  }
}

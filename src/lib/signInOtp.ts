import { AUTH_API_BASE_URL } from "@/config/api"

export type OtpChannel = "email" | "sms"

export type OtpSendPurpose = "resend" | "switch-to-email"

export interface SignInOtpChallenge {
  otpChannel: OtpChannel
  hasVerifiedPhone: boolean
  maskedPhone: string | null
}

export interface SendOtpApiResult {
  skipped: boolean
  otpChannel: OtpChannel
  message: string
  maskedPhone: string | null
}

function readStringField(
  source: Record<string, unknown>,
  keys: string[]
): string | null {
  for (const key of keys) {
    const value = source[key]
    if (typeof value === "string" && value.trim()) {
      return value
    }
  }

  return null
}

function readBooleanField(
  source: Record<string, unknown>,
  keys: string[]
): boolean | undefined {
  for (const key of keys) {
    const value = source[key]
    if (typeof value === "boolean") {
      return value
    }
  }

  return undefined
}

function normalizeOtpChannel(value: string | null): OtpChannel | null {
  if (value === "email" || value === "sms") {
    return value
  }

  return null
}

export function maskEmail(email: string) {
  const [local, domain] = email.split("@")
  if (!local || !domain) {
    return email
  }

  const maskedLocal = local.length <= 1 ? "•" : `${local[0]}••••`
  return `${maskedLocal}@${domain}`
}

export function getOtpDestinationCopy(
  channel: OtpChannel,
  email: string,
  maskedPhone: string | null
) {
  if (channel === "sms") {
    return maskedPhone ?? "your phone"
  }

  return maskEmail(email)
}

export function getOtpSentMessage(channel: OtpChannel) {
  if (channel === "sms") {
    return "Verification code sent to your phone."
  }

  return "Verification code sent to your email."
}

export function getOtpResentMessage(channel: OtpChannel) {
  if (channel === "sms") {
    return "A new verification code has been sent to your phone."
  }

  return "A new verification code has been sent to your email."
}

/** Metadata returned when universal-login requires OTP (no trust skip). */
export function parseOtpChallengeResponse(
  result: unknown
): SignInOtpChallenge | null {
  if (!result || typeof result !== "object") {
    return null
  }

  const data = result as Record<string, unknown>

  if (data.loginType !== "USER" || data.token) {
    return null
  }

  const otpChannel =
    normalizeOtpChannel(
      readStringField(data, ["otpChannel", "OtpChannel"])
    ) ?? "email"

  const hasVerifiedPhone =
    readBooleanField(data, ["hasVerifiedPhone", "HasVerifiedPhone"]) ??
    false

  const maskedPhone = readStringField(data, [
    "maskedPhone",
    "MaskedPhone",
  ])

  return {
    otpChannel,
    hasVerifiedPhone,
    maskedPhone,
  }
}

export function parseSendOtpResponse(result: unknown): SendOtpApiResult | null {
  if (!result || typeof result !== "object") {
    return null
  }

  const data = result as Record<string, unknown>
  const otpChannel =
    normalizeOtpChannel(
      readStringField(data, ["otpChannel", "OtpChannel"])
    ) ?? "email"

  const message =
    readStringField(data, ["message", "Message"]) ??
    "OTP request completed."

  const skipped =
    readBooleanField(data, ["skipped", "Skipped"]) ?? false

  const maskedPhone = readStringField(data, [
    "maskedPhone",
    "MaskedPhone",
  ])

  return {
    skipped,
    otpChannel,
    message,
    maskedPhone,
  }
}

async function postSendOtp(
  email: string,
  purpose: OtpSendPurpose
): Promise<Response> {
  return fetch(`${AUTH_API_BASE_URL}/send-otp`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      email,
      purpose,
    }),
  })
}

export async function requestOtpResend(email: string) {
  const response = await postSendOtp(email, "resend")
  const result = await response.json()

  if (!response.ok) {
    throw new Error(
      typeof result?.message === "string" && result.message.trim()
        ? result.message
        : "We couldn't resend the code. Try again shortly."
    )
  }

  const parsed = parseSendOtpResponse(result)

  if (!parsed) {
    throw new Error("Resend succeeded but response data was missing.")
  }

  return parsed
}

export async function requestSwitchToEmailOtp(email: string) {
  const response = await postSendOtp(email, "switch-to-email")
  const result = await response.json()

  if (!response.ok) {
    throw new Error(
      typeof result?.message === "string" && result.message.trim()
        ? result.message
        : "Unable to continue with email verification."
    )
  }

  const parsed = parseSendOtpResponse(result)

  if (!parsed) {
    throw new Error("Request succeeded but response data was missing.")
  }

  return parsed
}

export async function requestSwitchToSmsOtp(email: string) {
  const response = await fetch(`${AUTH_API_BASE_URL}/send-otp-sms`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ email }),
  })

  const result = await response.json()

  if (!response.ok) {
    throw new Error(
      typeof result?.message === "string" && result.message.trim()
        ? result.message
        : "Unable to send SMS verification code."
    )
  }

  const parsed = parseSendOtpResponse(result)

  if (!parsed) {
    throw new Error("SMS request succeeded but response data was missing.")
  }

  return parsed
}

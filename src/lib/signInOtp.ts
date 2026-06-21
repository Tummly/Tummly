import axiosInstance from "@/api/axiosInstance"
import {
  getFetchErrorMessage,
  readBoolean,
  readString,
} from "@/lib/apiEnvelope"
import { isAxiosError } from "axios"

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
    normalizeOtpChannel(readString(data, "otpChannel")) ?? "email"

  const hasVerifiedPhone =
    readBoolean(data, "hasVerifiedPhone") ?? false

  const maskedPhone = readString(data, "maskedPhone")

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
    normalizeOtpChannel(readString(data, "otpChannel")) ?? "email"

  const message =
    readString(data, "message") ?? "OTP request completed."

  const skipped = readBoolean(data, "skipped") ?? false

  const maskedPhone = readString(data, "maskedPhone")

  return {
    skipped,
    otpChannel,
    message,
    maskedPhone,
  }
}

const preAuthConfig = { skipAuthRedirect: true } as const

async function postSendOtp(
  email: string,
  purpose: OtpSendPurpose
): Promise<unknown> {
  const response = await axiosInstance.post(
    "/auth/send-otp",
    { email, purpose },
    preAuthConfig
  )
  return response.data
}

export async function requestOtpResend(email: string) {
  try {
    const result = await postSendOtp(email, "resend")
    const parsed = parseSendOtpResponse(result)

    if (!parsed) {
      throw new Error("Resend succeeded but response data was missing.")
    }

    return parsed
  } catch (error) {
    if (isAxiosError(error)) {
      throw new Error(
        getFetchErrorMessage(
          error.response?.data,
          "We couldn't resend the code. Try again shortly."
        )
      )
    }
    throw error
  }
}

export async function requestSwitchToEmailOtp(email: string) {
  try {
    const result = await postSendOtp(email, "switch-to-email")
    const parsed = parseSendOtpResponse(result)

    if (!parsed) {
      throw new Error("Request succeeded but response data was missing.")
    }

    return parsed
  } catch (error) {
    if (isAxiosError(error)) {
      throw new Error(
        getFetchErrorMessage(
          error.response?.data,
          "Unable to continue with email verification."
        )
      )
    }
    throw error
  }
}

export async function requestSwitchToSmsOtp(email: string) {
  try {
    const response = await axiosInstance.post(
      "/auth/send-otp-sms",
      { email },
      preAuthConfig
    )
    const parsed = parseSendOtpResponse(response.data)

    if (!parsed) {
      throw new Error("SMS request succeeded but response data was missing.")
    }

    return parsed
  } catch (error) {
    if (isAxiosError(error)) {
      throw new Error(
        getFetchErrorMessage(
          error.response?.data,
          "Unable to send SMS verification code."
        )
      )
    }
    throw error
  }
}

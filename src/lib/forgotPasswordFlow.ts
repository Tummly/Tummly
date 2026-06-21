import axiosInstance from "@/api/axiosInstance"
import { getFetchErrorMessage } from "@/lib/apiEnvelope"
import { isAxiosError } from "axios"
import { toSignInEmailPayload, type SignInEmailValues } from "@/schemas/signIn"

export const FORGOT_PASSWORD_STEPS = {
  REQUEST_EMAIL: "REQUEST_EMAIL",
  EMAIL_SENT: "EMAIL_SENT",
} as const

export type ForgotPasswordStep =
  (typeof FORGOT_PASSWORD_STEPS)[keyof typeof FORGOT_PASSWORD_STEPS]

export function maskEmailForDisplay(email: string) {
  const [local, domain] = email.split("@")
  if (!local || !domain) {
    return email
  }

  const maskedLocal = local.length <= 1 ? "•" : `${local[0]}••••`
  return `${maskedLocal}@${domain}`
}

export async function requestPasswordReset(values: SignInEmailValues) {
  const payload = toSignInEmailPayload(values)

  try {
    await axiosInstance.post("/auth/forgot-password", payload, {
      skipAuthRedirect: true,
    })
  } catch (error) {
    if (isAxiosError(error)) {
      throw new Error(
        getFetchErrorMessage(error.response?.data, "Unable to send reset link.")
      )
    }
    throw error
  }

  return payload.email
}

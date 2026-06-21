import axiosInstance from "@/api/axiosInstance"
import { getFetchErrorMessage } from "@/lib/apiEnvelope"
import { isAxiosError } from "axios"
import {
  toResetPasswordPayload,
  type ResetPasswordFormValues,
} from "@/schemas/resetPassword"

export const RESET_PASSWORD_STEPS = {
  CREATE_PASSWORD: "CREATE_PASSWORD",
  SUCCESS: "SUCCESS",
  INVALID_TOKEN: "INVALID_TOKEN",
} as const

export type ResetPasswordStep =
  (typeof RESET_PASSWORD_STEPS)[keyof typeof RESET_PASSWORD_STEPS]

export function isResetTokenError(message: string) {
  const normalized = message.toLowerCase()

  return (
    normalized.includes("invalid reset token") ||
    normalized.includes("reset token expired") ||
    normalized.includes("invalid or missing token")
  )
}

export async function submitPasswordReset(
  values: ResetPasswordFormValues,
  token: string
) {
  const payload = toResetPasswordPayload(values, token)

  try {
    const response = await axiosInstance.post(
      "/auth/reset-password",
      payload,
      { skipAuthRedirect: true }
    )
    return getFetchErrorMessage(response.data, "Password reset successful.")
  } catch (error) {
    if (isAxiosError(error)) {
      throw new Error(
        getFetchErrorMessage(error.response?.data, "Unable to reset password.")
      )
    }
    throw error
  }
}

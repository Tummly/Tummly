import { AUTH_API_BASE_URL } from "@/config/api"
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

function getFetchErrorMessage(
  result: { message?: string },
  fallback: string
) {
  return result.message?.trim() || fallback
}

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
  const response = await fetch(`${AUTH_API_BASE_URL}/reset-password`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  })

  const result = await response.json()

  if (!response.ok) {
    throw new Error(
      getFetchErrorMessage(result, "Unable to reset password.")
    )
  }

  return getFetchErrorMessage(result, "Password reset successful.")
}

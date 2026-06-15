import { AUTH_API_BASE_URL } from "@/config/api"
import { toSignInEmailPayload, type SignInEmailValues } from "@/schemas/signIn"

export const FORGOT_PASSWORD_STEPS = {
  REQUEST_EMAIL: "REQUEST_EMAIL",
  EMAIL_SENT: "EMAIL_SENT",
} as const

export type ForgotPasswordStep =
  (typeof FORGOT_PASSWORD_STEPS)[keyof typeof FORGOT_PASSWORD_STEPS]

function getFetchErrorMessage(
  result: { message?: string },
  fallback: string
) {
  return result.message?.trim() || fallback
}

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
  const response = await fetch(`${AUTH_API_BASE_URL}/forgot-password`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  })

  const result = await response.json()

  if (!response.ok) {
    throw new Error(
      getFetchErrorMessage(result, "Unable to send reset link.")
    )
  }

  return payload.email
}

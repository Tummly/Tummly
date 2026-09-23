import axiosInstance from "./axiosInstance"
import type { SignupSession } from "./signupApi"
import { unwrapDataObject } from "@/lib/apiEnvelope"

const skipAuth = { skipAuthRedirect: true } as const

function requireDataObject(payload: unknown): Record<string, unknown> {
  const data = unwrapDataObject(payload)
  if (!data) {
    throw new Error("Invalid signup response.")
  }
  return data
}

function asSignupSession(data: Record<string, unknown>): SignupSession {
  return {
    sessionToken: String(data.sessionToken ?? ""),
    email: String(data.email ?? ""),
    status: String(data.status ?? ""),
  }
}

export async function exchangeExternalAuth(payload: {
  token: string
  rememberDevice: boolean
  deviceToken?: string
}) {
  const response = await axiosInstance.post(
    "/auth/external/exchange",
    payload,
    skipAuth
  )
  return response.data
}

export async function acceptExternalAuthTerms(payload: {
  token: string
  termsAccepted: boolean
}): Promise<SignupSession> {
  const response = await axiosInstance.post(
    "/auth/external/accept-terms",
    payload,
    skipAuth
  )
  return asSignupSession(requireDataObject(response.data))
}

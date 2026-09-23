import axiosInstance from "./axiosInstance"
import { unwrapDataObject } from "@/lib/apiEnvelope"
import type {
  SignupStartPayload,
  SignupVerifyOtpPayload,
} from "@/schemas/signup"

const skipAuth = { skipAuthRedirect: true } as const

export type SignupSession = {
  sessionToken: string
  email: string
  status: string
}

export type SignupResume = SignupSession & {
  lastStepHint: string
  fullName: string | null
  accountType: string | null
  groupName: string | null
  businessCategory: string | null
  primaryPhone: string | null
  businessLink: string | null
  onboardingJson: string | null
  /** Google | Microsoft when social pending; null for email signup. */
  authProvider: string | null
}

export type SignupOnboardingLocation = {
  locationName: string
  address: string
  city?: string | null
  postcode?: string | null
  locationPhone?: string | null
  localContact?: string | null
  addressOverridden?: boolean
}

export type SignupOnboardingPayload = {
  password: string
  confirmPassword: string
  fullName: string
  groupName: string
  businessCategory: string
  primaryPhone?: string | null
  businessLink?: string | null
  locations: SignupOnboardingLocation[]
}

export type SignupProvisioningStatus = {
  status: string
  ready: boolean
}

function asSignupSession(data: Record<string, unknown>): SignupSession {
  return {
    sessionToken: String(data.sessionToken ?? ""),
    email: String(data.email ?? ""),
    status: String(data.status ?? ""),
  }
}

function asSignupResume(data: Record<string, unknown>): SignupResume {
  const session = asSignupSession(data)
  return {
    ...session,
    lastStepHint: String(data.lastStepHint ?? ""),
    fullName: typeof data.fullName === "string" ? data.fullName : null,
    accountType: typeof data.accountType === "string" ? data.accountType : null,
    groupName: typeof data.groupName === "string" ? data.groupName : null,
    businessCategory:
      typeof data.businessCategory === "string" ? data.businessCategory : null,
    primaryPhone:
      typeof data.primaryPhone === "string" ? data.primaryPhone : null,
    businessLink:
      typeof data.businessLink === "string" ? data.businessLink : null,
    onboardingJson:
      typeof data.onboardingJson === "string" ? data.onboardingJson : null,
    authProvider:
      typeof data.authProvider === "string" ? data.authProvider : null,
  }
}

function requireDataObject(payload: unknown): Record<string, unknown> {
  const data = unwrapDataObject(payload)
  if (!data) {
    throw new Error("Invalid signup response.")
  }
  return data
}

export async function startSignup(
  data: SignupStartPayload
): Promise<SignupSession> {
  const response = await axiosInstance.post("/Signup/start", data, skipAuth)
  return asSignupSession(requireDataObject(response.data))
}

export async function verifySignupOtp(
  data: SignupVerifyOtpPayload
): Promise<SignupSession> {
  const response = await axiosInstance.post(
    "/Signup/verify-otp",
    data,
    skipAuth
  )
  return asSignupSession(requireDataObject(response.data))
}

export async function resendSignupOtp(email: string): Promise<unknown> {
  const response = await axiosInstance.post(
    "/Signup/resend-otp",
    { email },
    skipAuth
  )
  return response.data
}

export async function saveSignupOnboarding(
  sessionToken: string,
  data: SignupOnboardingPayload
): Promise<SignupSession> {
  const response = await axiosInstance.post("/Signup/onboarding", data, {
    params: { sessionToken },
    ...skipAuth,
  })
  return asSignupSession(requireDataObject(response.data))
}

export async function getSignupSession(
  sessionToken: string
): Promise<SignupResume> {
  const response = await axiosInstance.get("/Signup/session", {
    params: { sessionToken },
    ...skipAuth,
  })
  return asSignupResume(requireDataObject(response.data))
}

export async function retrySignupProvision(
  sessionToken: string
): Promise<void> {
  await axiosInstance.post(
    "/Signup/retry-provision",
    null,
    {
      params: { sessionToken },
      ...skipAuth,
    }
  )
}

export async function getSignupProvisioningStatus(
  sessionToken: string
): Promise<SignupProvisioningStatus> {
  const response = await axiosInstance.get("/Signup/provisioning-status", {
    params: { sessionToken },
    ...skipAuth,
  })
  const payload = requireDataObject(response.data)
  return {
    status: String(payload.status ?? ""),
    ready: payload.ready === true,
  }
}

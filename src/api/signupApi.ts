import { isAxiosError } from "axios"

import axiosInstance from "./axiosInstance"
import { unwrapDataObject } from "@/lib/apiEnvelope"
import type {
  SignupStartPayload,
  SignupVerifyOtpPayload,
} from "@/schemas/signup"

const skipAuth = { skipAuthRedirect: true } as const

/** Signup endpoints take the proof-of-email token only in this header. */
const SIGNUP_SESSION_HEADER = "X-Signup-Session"

function signupSessionConfig(sessionToken: string) {
  return {
    headers: { [SIGNUP_SESSION_HEADER]: sessionToken },
    ...skipAuth,
  } as const
}

/**
 * Keep the server's `code` (and `retryAfterUtc` when locked) on the thrown
 * error so callers can map it. Axios errors without a body code pass through.
 */
async function withSignupErrorFields<T>(request: Promise<T>): Promise<T> {
  try {
    return await request
  } catch (error) {
    if (isAxiosError<{ code?: unknown; retryAfterUtc?: unknown }>(error)) {
      const data = error.response?.data
      const fields: { code?: string; retryAfterUtc?: string } = {}
      if (typeof data?.code === "string") {
        fields.code = data.code
      }
      if (typeof data?.retryAfterUtc === "string") {
        fields.retryAfterUtc = data.retryAfterUtc
      }
      Object.assign(error, fields)
    }
    throw error
  }
}

export type SignupSessionLoginResult = {
  token: string
  refreshToken: string | null
  accountType: string
  workspaceCount: number
  restaurantId: number | null
  selectedLocationId: number | null
  activationRequired: boolean
  ownerActivation: "ok" | "pending" | "expired"
}

export type SignupSessionLoginErrorCode =
  | "payment_pending"
  | "session_login_used"
  | "signup_session_expired"

export class SignupSessionLoginError extends Error {
  readonly code: SignupSessionLoginErrorCode

  constructor(
    code: SignupSessionLoginErrorCode,
    message: string,
    options?: ErrorOptions
  ) {
    super(message, options)
    this.name = "SignupSessionLoginError"
    this.code = code
  }
}

export type SignupStartResult = {
  email: string
  status: "code_sent"
}

/** Returned by verify-otp and onboarding. Only verify-otp carries a usable token. */
export type SignupSession = {
  sessionToken: string
  email: string
  status: string
}

/** `GET /Signup/session` never returns the token. A Complete row returns status only. */
export type SignupResume = Omit<SignupSession, "sessionToken"> & {
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
  chosenPlan?: string | null
  chosenCadence?: string | null
  /** Base64 or JSON custom plan snapshot from `/custom-plan`. */
  customPlanConfigJson?: string | null
}

export type SignupProvisioningStatus = {
  status: string
  ready: boolean
  paymentRedirectUrl?: string | null
  /** With status `PaymentFailed`: the paid checkout did not open. */
  paymentFailureCode?: string | null
}

function asSignupSession(data: Record<string, unknown>): SignupSession {
  return {
    sessionToken: String(data.sessionToken ?? ""),
    email: String(data.email ?? ""),
    status: String(data.status ?? ""),
  }
}

function asSignupResume(data: Record<string, unknown>): SignupResume {
  return {
    email: String(data.email ?? ""),
    status: String(data.status ?? ""),
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

/** Start sends a code only. It never returns a token; verify-otp does. */
export async function startSignup(
  data: SignupStartPayload
): Promise<SignupStartResult> {
  const response = await withSignupErrorFields(
    axiosInstance.post("/Signup/start", data, skipAuth)
  )
  const payload = requireDataObject(response.data)
  return {
    email: String(payload.email ?? ""),
    status: "code_sent",
  }
}

export async function verifySignupOtp(
  data: SignupVerifyOtpPayload
): Promise<SignupSession> {
  const response = await withSignupErrorFields(
    axiosInstance.post("/Signup/verify-otp", data, skipAuth)
  )
  return asSignupSession(requireDataObject(response.data))
}

export async function resendSignupOtp(email: string): Promise<unknown> {
  const response = await withSignupErrorFields(
    axiosInstance.post("/Signup/resend-otp", { email }, skipAuth)
  )
  return response.data
}

export async function saveSignupOnboarding(
  sessionToken: string,
  data: SignupOnboardingPayload
): Promise<SignupSession> {
  const response = await withSignupErrorFields(
    axiosInstance.post(
      "/Signup/onboarding",
      data,
      signupSessionConfig(sessionToken)
    )
  )
  return asSignupSession(requireDataObject(response.data))
}

export async function getSignupSession(
  sessionToken: string
): Promise<SignupResume> {
  const response = await withSignupErrorFields(
    axiosInstance.get("/Signup/session", signupSessionConfig(sessionToken))
  )
  return asSignupResume(requireDataObject(response.data))
}

export async function retrySignupProvision(
  sessionToken: string
): Promise<void> {
  await withSignupErrorFields(
    axiosInstance.post(
      "/Signup/retry-provision",
      null,
      signupSessionConfig(sessionToken)
    )
  )
}

export async function getSignupProvisioningStatus(
  sessionToken: string
): Promise<SignupProvisioningStatus> {
  const response = await withSignupErrorFields(
    axiosInstance.get(
      "/Signup/provisioning-status",
      signupSessionConfig(sessionToken)
    )
  )
  const payload = requireDataObject(response.data)
  return {
    status: String(payload.status ?? ""),
    ready: payload.ready === true,
    paymentRedirectUrl:
      typeof payload.paymentRedirectUrl === "string"
        ? payload.paymentRedirectUrl
        : null,
    paymentFailureCode:
      typeof payload.paymentFailureCode === "string"
        ? payload.paymentFailureCode
        : null,
  }
}

function asSignupSessionLogin(
  data: Record<string, unknown>
): SignupSessionLoginResult {
  const ownerActivationRaw = String(data.ownerActivation ?? "ok")
  const ownerActivation =
    ownerActivationRaw === "pending" || ownerActivationRaw === "expired"
      ? ownerActivationRaw
      : "ok"

  return {
    token: String(data.token ?? ""),
    refreshToken:
      typeof data.refreshToken === "string" ? data.refreshToken : null,
    accountType: String(data.accountType ?? ""),
    workspaceCount:
      typeof data.workspaceCount === "number" ? data.workspaceCount : 0,
    restaurantId:
      typeof data.restaurantId === "number" ? data.restaurantId : null,
    selectedLocationId:
      typeof data.selectedLocationId === "number"
        ? data.selectedLocationId
        : null,
    activationRequired: data.activationRequired === true,
    ownerActivation,
  }
}

function readConflictCode(payload: unknown): string | null {
  if (payload == null || typeof payload !== "object" || Array.isArray(payload)) {
    return null
  }
  const code = (payload as { code?: unknown }).code
  return typeof code === "string" && code.trim() !== "" ? code.trim() : null
}

/**
 * Exchange a completed signup session for an operator auth session.
 * 409 `payment_pending` / `session_login_used` and 401 `signup_session_expired`
 * → SignupSessionLoginError (the 401 keeps its code so the page can send the user
 * to sign in: the account exists).
 */
export async function signupSessionLogin(
  sessionToken: string,
  options: { continueOnFree?: boolean } = {}
): Promise<SignupSessionLoginResult> {
  try {
    const response = await axiosInstance.post(
      "/Signup/session-login",
      { sessionToken, continueOnFree: options.continueOnFree === true },
      skipAuth
    )
    const result = asSignupSessionLogin(requireDataObject(response.data))
    if (!result.token) {
      throw new Error("Invalid session-login response.")
    }
    return result
  } catch (error) {
    if (error instanceof SignupSessionLoginError) {
      throw error
    }
    if (isAxiosError(error) && error.response?.status === 409) {
      const code = readConflictCode(error.response.data)
      if (code === "payment_pending") {
        throw new SignupSessionLoginError(
          "payment_pending",
          "Payment is still confirming. Please wait a moment.",
          { cause: error }
        )
      }
      if (code === "session_login_used") {
        throw new SignupSessionLoginError(
          "session_login_used",
          "This setup link was already used. Please sign in.",
          { cause: error }
        )
      }
    }
    if (isAxiosError(error) && error.response?.status === 401) {
      if (readConflictCode(error.response.data) === "signup_session_expired") {
        throw new SignupSessionLoginError(
          "signup_session_expired",
          "This setup link has expired. Please sign in.",
          { cause: error }
        )
      }
    }
    if (isAxiosError<{ message?: string }>(error)) {
      throw new Error(
        error.response?.data?.message ?? "Could not finish sign-in.",
        { cause: error }
      )
    }
    throw error
  }
}

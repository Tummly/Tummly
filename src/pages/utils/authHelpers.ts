import {
  getAuthToken,
  hasAuthSession,
  useAuthStore,
} from "@/stores/authStore"
import type { AuthSessionRole } from "@/types/auth"
import {
  readBoolean,
  readNumber,
  readString,
  unwrapDataObject,
} from "@/lib/apiEnvelope"

export type { AuthSessionRole }

export const DEVICE_TOKEN_KEY = "deviceToken"
export const SELECTED_LOCATION_KEY = "selectedLocationId"

export const WORKSPACE_SETUP_PATH = "/login?step=workspace-setup"

export interface UniversalLoginResponse {
  loginType?: "ADMIN" | "USER"
  token?: string
  accountType?: string
  workspaceSetupRequired?: boolean
  message?: string
}

export interface VerifyOtpPayload {
  token: string
  accountType: string
  workspaceSetupRequired?: boolean
  selectedLocationId?: number | null
  deviceToken?: string
}

export interface UserSessionPayload {
  token: string
  accountType: string
  workspaceSetupRequired?: boolean
  selectedLocationId?: number | null
}

/** Persist JWT and role for ProtectedRoute / RoleRoute. */
export function persistAuthSession(token: string, role: AuthSessionRole, accountType?: string) {
  useAuthStore.getState().setSession(token, role, accountType)
}

/** Persist the opaque trusted-device token (30-day browser trust). */
export function persistDeviceToken(deviceToken: string) {
  localStorage.setItem(DEVICE_TOKEN_KEY, deviceToken)
}

export function persistSelectedLocation(locationId: number) {
  localStorage.setItem(SELECTED_LOCATION_KEY, String(locationId))
}

export function getSelectedLocationId(): number | null {
  const value = localStorage.getItem(SELECTED_LOCATION_KEY)
  if (!value?.trim()) {
    return null
  }

  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) ? parsed : null
}

export function getDeviceToken(): string | null {
  const token = localStorage.getItem(DEVICE_TOKEN_KEY)
  return token?.trim() ? token : null
}

/** Sign out — clears session only; device trust is retained (decision #13). */
export function clearAuthSession() {
  useAuthStore.getState().clearSession()
}

/** Non-React session read — prefer `useAuthStore` in components. */
export { getAuthToken, hasAuthSession }

/**
 * Auth verify-otp wraps the payload in `{ success, data: { token, accountType } }`.
 * Accepts unwrapped shapes too for resilience.
 */
export function parseVerifyOtpResponse(result: unknown): VerifyOtpPayload | null {
  const data = unwrapDataObject(result)

  if (!data) {
    return null
  }

  const token = readString(data, "token")
  const accountType = readString(data, "accountType")

  if (!token || !accountType) {
    return null
  }

  const workspaceSetupRequired = readBoolean(data, "workspaceSetupRequired")

  const selectedLocationId = readNumber(data, "selectedLocationId")

  const deviceToken = readString(data, "deviceToken")

  return {
    token,
    accountType,
    ...(workspaceSetupRequired !== undefined ? { workspaceSetupRequired } : {}),
    ...(selectedLocationId != null ? { selectedLocationId } : {}),
    ...(deviceToken ? { deviceToken } : {}),
  }
}

/**
 * Trust skip on universal-login returns JWT + routing fields without OTP.
 * Returns null when the client should proceed to A2.
 */
export function parseTrustSkipLoginResponse(
  result: unknown
): UserSessionPayload | null {
  if (!result || typeof result !== "object") {
    return null
  }

  const data = result as Record<string, unknown>

  if (data.loginType !== "USER") {
    return null
  }

  const token = readString(data, "token")
  const accountType = readString(data, "accountType")

  if (!token || !accountType) {
    return null
  }

  const workspaceSetupRequired = readBoolean(data, "workspaceSetupRequired")

  const selectedLocationId = readNumber(data, "selectedLocationId")

  return {
    token,
    accountType,
    ...(workspaceSetupRequired !== undefined ? { workspaceSetupRequired } : {}),
    ...(selectedLocationId != null ? { selectedLocationId } : {}),
  }
}

export function completeUserSession(
  session: UserSessionPayload,
  deviceToken?: string | null
) {
  persistAuthSession(session.token, "USER", session.accountType)

  if (deviceToken) {
    persistDeviceToken(deviceToken)
  }

  if (session.selectedLocationId != null) {
    persistSelectedLocation(session.selectedLocationId)
  }

  return getPostLoginDestination(
    session.accountType,
    session.workspaceSetupRequired,
    session.selectedLocationId
  )
}

export function getMultiDashboardPath(locationId?: number | null) {
  if (locationId == null) {
    return "/multi-dashboard"
  }

  return `/multi-dashboard?location=${locationId}`
}

/** Route after OTP or trust skip — dashboard or in-wizard workspace setup. */
export function getPostLoginDestination(
  accountType: string,
  workspaceSetupRequired = false,
  selectedLocationId?: number | null
): string {
  if (workspaceSetupRequired) {
    return WORKSPACE_SETUP_PATH
  }

  if (accountType === "Single") {
    return "/single-dashboard"
  }

  return getMultiDashboardPath(
    selectedLocationId ?? getSelectedLocationId()
  )
}

export function isWorkspaceSetupDestination(path: string) {
  return path === WORKSPACE_SETUP_PATH
}

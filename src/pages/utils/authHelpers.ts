export type AuthSessionRole = "ADMIN" | "USER"

export interface UniversalLoginResponse {
  loginType?: "ADMIN" | "USER"
  token?: string
  message?: string
}

export interface VerifyOtpPayload {
  token: string
  accountType: string
}

/** Persist JWT and role for ProtectedRoute / RoleRoute. */
export function persistAuthSession(token: string, role: AuthSessionRole) {
  localStorage.setItem("token", token)
  localStorage.setItem("role", role)
}

/**
 * Auth verify-otp wraps the payload in `{ success, data: { token, accountType } }`.
 * Accepts unwrapped shapes too for resilience.
 */
export function parseVerifyOtpResponse(result: unknown): VerifyOtpPayload | null {
  if (!result || typeof result !== "object") {
    return null
  }

  const envelope = result as Record<string, unknown>
  const data =
    envelope.data && typeof envelope.data === "object"
      ? (envelope.data as Record<string, unknown>)
      : envelope

  const token = data.token
  const accountType = data.accountType

  if (typeof token !== "string" || !token.trim()) {
    return null
  }

  if (typeof accountType !== "string" || !accountType.trim()) {
    return null
  }

  return { token, accountType }
}

/** Route after OTP — workspace-setup step (A5) not implemented yet. */
export function getPostVerifyDashboardPath(accountType: string): string {
  if (accountType === "Single") {
    return "/single-dashboard"
  }

  return "/multi-dashboard"
}

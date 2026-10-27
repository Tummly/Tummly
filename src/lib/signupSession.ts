import { isAxiosError } from "axios"
import type { NavigateFunction } from "react-router-dom"

const SIGNUP_SESSION_KEY = "tummly.signupSession"

export const SIGNUP_SESSION_EXPIRED_CODE = "signup_session_expired"

export const SIGNUP_SESSION_EXPIRED_MESSAGE =
  "Your signup session has expired. Enter your email to continue."

/** Router state for `/signup` after a legacy or stale session is cleared. */
export const SIGNUP_SESSION_EXPIRED_STATE = {
  signupSessionExpired: true,
} as const

export function buildSignupVerifyPath(email: string) {
  const trimmed = email.trim()
  return `/signup/verify?email=${encodeURIComponent(trimmed)}`
}

export function saveSignupSessionToken(token: string) {
  sessionStorage.setItem(SIGNUP_SESSION_KEY, token)
}

export function readSignupSessionToken(): string | null {
  const value = sessionStorage.getItem(SIGNUP_SESSION_KEY)
  if (!value?.trim()) return null
  return value
}

export function clearSignupSessionToken() {
  sessionStorage.removeItem(SIGNUP_SESSION_KEY)
}

/** True when the server rejected the signup token (401 `signup_session_expired`). */
export function isSignupSessionExpiredError(error: unknown): boolean {
  if (error == null || typeof error !== "object") return false
  if ((error as { code?: unknown }).code === SIGNUP_SESSION_EXPIRED_CODE) {
    return true
  }
  return (
    isAxiosError<{ code?: unknown }>(error) &&
    error.response?.data?.code === SIGNUP_SESSION_EXPIRED_CODE
  )
}

/**
 * On an expired signup token: clear it and send the user to `/signup` with the
 * expired message. Returns false and does nothing for any other error.
 */
export function redirectIfSignupSessionExpired(
  error: unknown,
  navigate: NavigateFunction
): boolean {
  if (!isSignupSessionExpiredError(error)) return false
  clearSignupSessionToken()
  navigate("/signup", { replace: true, state: SIGNUP_SESSION_EXPIRED_STATE })
  return true
}

export function isSignupSessionExpiredState(state: unknown): boolean {
  return (
    (state as { signupSessionExpired?: unknown } | null)?.signupSessionExpired ===
    true
  )
}

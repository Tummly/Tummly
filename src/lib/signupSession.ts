const SIGNUP_SESSION_KEY = "tummly.signupSession"
const SIGNUP_PAID_INTENT_KEY = "tummly.signupPaidIntent"

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

export function markSignupPaidIntent() {
  sessionStorage.setItem(SIGNUP_PAID_INTENT_KEY, "1")
}

export function readSignupPaidIntent(): boolean {
  return sessionStorage.getItem(SIGNUP_PAID_INTENT_KEY) === "1"
}

export function clearSignupPaidIntent() {
  sessionStorage.removeItem(SIGNUP_PAID_INTENT_KEY)
}

export function clearSignupSessionToken() {
  sessionStorage.removeItem(SIGNUP_SESSION_KEY)
  clearSignupPaidIntent()
}

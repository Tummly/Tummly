const SIGNUP_SESSION_KEY = "tummly.signupSession"

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

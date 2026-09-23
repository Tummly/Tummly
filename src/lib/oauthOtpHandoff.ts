import type { SignInOtpChallenge } from "@/lib/signInOtp"

/** React Router location.state key used by LoginOAuthCompletePage → LoginPage. */
export const OAUTH_OTP_HANDOFF_KEY = "oauthOtp" as const

/** sessionStorage key so Strict Mode remount does not lose the OTP step. */
export const OAUTH_OTP_HANDOFF_STORAGE_KEY = "tummly.oauth.otpHandoff"

const HANDOFF_TTL_MS = 5 * 60 * 1000

export type OAuthOtpHandoff = {
  email: string
  rememberDevice: boolean
  challenge: SignInOtpChallenge
}

type StoredHandoff = OAuthOtpHandoff & { storedAt: number }

function parseHandoff(raw: unknown): OAuthOtpHandoff | null {
  if (!raw || typeof raw !== "object") {
    return null
  }

  const data = raw as Record<string, unknown>
  const email = typeof data.email === "string" ? data.email.trim() : ""
  if (!email) {
    return null
  }

  const challenge = data.challenge
  if (!challenge || typeof challenge !== "object") {
    return null
  }

  const c = challenge as Record<string, unknown>
  const otpChannel = c.otpChannel === "sms" ? "sms" : "email"

  return {
    email,
    rememberDevice: data.rememberDevice === true,
    challenge: {
      otpChannel,
      hasVerifiedPhone: c.hasVerifiedPhone === true,
      maskedPhone:
        typeof c.maskedPhone === "string" && c.maskedPhone.trim()
          ? c.maskedPhone.trim()
          : null,
    },
  }
}

export function readOAuthOtpHandoff(state: unknown): OAuthOtpHandoff | null {
  if (!state || typeof state !== "object") {
    return null
  }

  const raw = (state as Record<string, unknown>)[OAUTH_OTP_HANDOFF_KEY]
  return parseHandoff(raw)
}

export function writeOAuthOtpHandoff(
  handoff: OAuthOtpHandoff,
  storage: Pick<Storage, "setItem"> = sessionStorage
): void {
  const payload: StoredHandoff = {
    ...handoff,
    storedAt: Date.now(),
  }
  storage.setItem(OAUTH_OTP_HANDOFF_STORAGE_KEY, JSON.stringify(payload))
}

/**
 * Peeks sessionStorage without removing. Survives Strict Mode remount;
 * call {@link clearOAuthOtpHandoff} after OTP completes or user leaves
 * (see {@link scheduleOAuthOtpHandoffClearOnLeave} / credential submit).
 */
export function peekOAuthOtpHandoff(
  storage: Pick<Storage, "getItem" | "removeItem"> = sessionStorage
): OAuthOtpHandoff | null {
  const raw = storage.getItem(OAUTH_OTP_HANDOFF_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as StoredHandoff
    if (
      typeof parsed.storedAt !== "number" ||
      Date.now() - parsed.storedAt > HANDOFF_TTL_MS
    ) {
      storage.removeItem(OAUTH_OTP_HANDOFF_STORAGE_KEY)
      return null
    }
    return parseHandoff(parsed)
  } catch {
    storage.removeItem(OAUTH_OTP_HANDOFF_STORAGE_KEY)
    return null
  }
}

export function clearOAuthOtpHandoff(
  storage: Pick<Storage, "removeItem"> = sessionStorage
): void {
  storage.removeItem(OAUTH_OTP_HANDOFF_STORAGE_KEY)
}

/** Pending leave-clear so Strict Mode remount can cancel before TTL wipe. */
let pendingLeaveClearTimer: ReturnType<typeof setTimeout> | null = null

/**
 * Schedule handoff clear after the login OTP path unmounts.
 * Cancel with {@link cancelScheduledOAuthOtpHandoffClear} on remount so
 * Strict Mode does not wipe the stash before the remount peeks it.
 */
export function scheduleOAuthOtpHandoffClearOnLeave(
  storage: Pick<Storage, "removeItem"> = sessionStorage,
  delayMs = 0
): void {
  if (pendingLeaveClearTimer != null) {
    clearTimeout(pendingLeaveClearTimer)
  }
  pendingLeaveClearTimer = setTimeout(() => {
    pendingLeaveClearTimer = null
    clearOAuthOtpHandoff(storage)
  }, delayMs)
}

export function cancelScheduledOAuthOtpHandoffClear(): void {
  if (pendingLeaveClearTimer != null) {
    clearTimeout(pendingLeaveClearTimer)
    pendingLeaveClearTimer = null
  }
}

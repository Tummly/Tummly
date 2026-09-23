export type OAuthErrorSurface = "login" | "signup"

/** sessionStorage key so Strict Mode remount does not lose the error banner. */
export const OAUTH_ERROR_STORAGE_KEY = "tummly.oauth.errorBanner"

const ERROR_TTL_MS = 5 * 60 * 1000

type StoredOAuthError = {
  surface: OAuthErrorSurface
  message: string
  storedAt: number
}

/**
 * Maps `oauthError` query values from the OAuth callback redirect.
 */
export function getOAuthErrorMessage(
  code: string | null | undefined,
  surface: OAuthErrorSurface
): string | null {
  if (!code?.trim()) {
    return null
  }

  switch (code.trim()) {
    case "account_exists":
      return "An account with this email already exists. Sign in with email and password."
    case "cancelled":
      return surface === "signup"
        ? "Sign-up was cancelled. You can try again when you are ready."
        : "Sign-in was cancelled. You can try again when you are ready."
    case "staff_password_only":
      return "Staff accounts must use email and password Sign-in."
    case "provider_failed":
      return surface === "signup"
        ? "Google or Microsoft did not complete Sign-up. Please try again."
        : "Google or Microsoft did not complete Sign-in. Please try again."
    case "failed":
    default:
      return surface === "signup"
        ? "We could not complete social sign-up. If you already have an account, Sign in with email and password."
        : "We could not complete social sign-in. Please try again."
  }
}

export function stashOAuthErrorMessage(
  surface: OAuthErrorSurface,
  message: string,
  storage: Pick<Storage, "setItem"> = sessionStorage
): void {
  const trimmed = message.trim()
  if (!trimmed) {
    return
  }

  const payload: StoredOAuthError = {
    surface,
    message: trimmed,
    storedAt: Date.now(),
  }
  storage.setItem(OAUTH_ERROR_STORAGE_KEY, JSON.stringify(payload))
}

/**
 * Peeks a stashed banner without removing. Survives Strict Mode remount;
 * call {@link clearOAuthErrorMessage} after the form is submitted or dismissed.
 */
export function peekOAuthErrorMessage(
  surface: OAuthErrorSurface,
  storage: Pick<Storage, "getItem" | "removeItem"> = sessionStorage
): string | null {
  const raw = storage.getItem(OAUTH_ERROR_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as StoredOAuthError
    if (
      parsed.surface !== surface ||
      typeof parsed.message !== "string" ||
      !parsed.message.trim() ||
      typeof parsed.storedAt !== "number" ||
      Date.now() - parsed.storedAt > ERROR_TTL_MS
    ) {
      if (
        typeof parsed.storedAt === "number" &&
        Date.now() - parsed.storedAt > ERROR_TTL_MS
      ) {
        storage.removeItem(OAUTH_ERROR_STORAGE_KEY)
      }
      return null
    }
    return parsed.message.trim()
  } catch {
    storage.removeItem(OAUTH_ERROR_STORAGE_KEY)
    return null
  }
}

export function clearOAuthErrorMessage(
  storage: Pick<Storage, "removeItem"> = sessionStorage
): void {
  storage.removeItem(OAUTH_ERROR_STORAGE_KEY)
}

/**
 * Resolve the OAuth error banner.
 *
 * When `oauthError` or `oauthMessage` is present in the URL, use those only
 * (ignore a stale sessionStorage stash). Use `stashed` only when both query
 * params are absent — e.g. Strict Mode remount after the URL was stripped.
 */
export function resolveOAuthErrorBanner(
  surface: OAuthErrorSurface,
  options: {
    oauthError?: string | null
    oauthMessage?: string | null
    stashed?: string | null
  }
): string | null {
  const hasQuery =
    Boolean(options.oauthError?.trim()) ||
    Boolean(options.oauthMessage?.trim())

  if (hasQuery) {
    const fromMessage = options.oauthMessage?.trim()
    if (fromMessage) {
      return fromMessage
    }
    return getOAuthErrorMessage(options.oauthError, surface)
  }

  const fromStash = options.stashed?.trim()
  return fromStash || null
}

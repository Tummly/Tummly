import { isAxiosError } from "axios"

import { GuestPreferencesSessionExpiredError } from "@/api/publicGuestPreferencesApi"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"

/** Where a failed request sends the page. */
export type GuestPreferencesRequestErrorRoute =
  | { view: "entry" }
  | { view: "error"; message: string }

/**
 * A signed link failed. Only a 400 means the link is invalid, used or expired.
 * Anything else (network, 5xx) means the link may still be valid.
 */
export type LinkFailure = "invalid" | "unavailable"

export function classifyLinkFailure(error: unknown): LinkFailure {
  return isAxiosError(error) && error.response?.status === 400
    ? "invalid"
    : "unavailable"
}

/** Runs a link action. `ok` on success, else the link failure class. */
export async function confirmLink(
  action: () => Promise<unknown>
): Promise<"ok" | LinkFailure> {
  try {
    await action()
    return "ok"
  } catch (error) {
    return classifyLinkFailure(error)
  }
}

/** What a `?t=` session link resolved to. */
export type LinkSessionOutcome = "valid" | LinkFailure

/**
 * Runs the `?t=` session call. A reply with `valid: false` and a 400 both mean
 * the link is dead. Any other failure leaves the link possibly valid.
 */
export async function resolveLinkSession(
  open: () => Promise<{ valid: boolean }>
): Promise<LinkSessionOutcome> {
  try {
    return (await open()).valid ? "valid" : "invalid"
  } catch (error) {
    return classifyLinkFailure(error)
  }
}

/** The API error code (`error.response.data.error`), or null. */
function apiErrorCode(error: unknown): string | null {
  if (!isAxiosError(error)) {
    return null
  }
  const data: unknown = error.response?.data
  if (
    typeof data === "object" &&
    data !== null &&
    "error" in data &&
    typeof data.error === "string" &&
    data.error.trim() !== ""
  ) {
    return data.error
  }
  return null
}

/**
 * Guest copy for an API error. The API sends codes, never sentences, so only a
 * known code maps to copy. Any other code shows the fallback, never the raw code.
 */
export function guestPreferencesErrorMessage(
  error: unknown,
  fallback: string
): string {
  return apiErrorCode(error) === "invalid"
    ? GUEST_PREFERENCES_COPY.invalidEntry
    : fallback
}

/** A 404 with `{ error: "not-found" }`. The guest or location is no longer on the list. */
export function isNotFoundError(error: unknown): boolean {
  return isAxiosError(error) && error.response?.status === 404 && apiErrorCode(error) === "not-found"
}

/**
 * Session expiry (401) routes to the entry view. Any other failure routes to
 * the error view with the API message, or the fallback copy.
 */
export function routeGuestPreferencesRequestError(
  error: unknown,
  fallback: string
): GuestPreferencesRequestErrorRoute {
  if (error instanceof GuestPreferencesSessionExpiredError) {
    return { view: "entry" }
  }
  return {
    view: "error",
    message: guestPreferencesErrorMessage(error, fallback),
  }
}

/**
 * A failed write on the overview. A not-found means the guest was merged away after
 * the session was issued, so the session is stale and the entry view is shown.
 */
export function routeOverviewWriteError(
  error: unknown,
  fallback: string
): GuestPreferencesRequestErrorRoute {
  if (isNotFoundError(error)) {
    return { view: "entry" }
  }
  return routeGuestPreferencesRequestError(error, fallback)
}

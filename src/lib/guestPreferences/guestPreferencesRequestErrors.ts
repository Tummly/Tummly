import { isAxiosError } from "axios"

import { GuestPreferencesSessionExpiredError } from "@/api/publicGuestPreferencesApi"

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

/** Reads `error.response.data.error` from an Axios error, else the fallback. */
export function guestPreferencesErrorMessage(
  error: unknown,
  fallback: string
): string {
  if (isAxiosError(error)) {
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
  }
  return fallback
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

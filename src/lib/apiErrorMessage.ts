import { isAxiosError } from "axios"

/** Shown when the API returns 403 without a usable body message. */
export const API_PERMISSION_DENIED_MESSAGE = "You don't have permission."

const AXIOS_STATUS_MESSAGE =
  /^Request failed with status code \d+$/i

function readResponseMessage(data: unknown): string | null {
  if (data == null || typeof data !== "object" || Array.isArray(data)) {
    return null
  }
  const message = (data as { message?: unknown }).message
  if (typeof message === "string" && message.trim() !== "") {
    return message.trim()
  }
  return null
}

function isAxiosStatusMessage(message: string): boolean {
  return AXIOS_STATUS_MESSAGE.test(message.trim())
}

/**
 * User-facing copy for API / Axios failures.
 * Prefers `response.data.message`. Maps bare 403 to permission copy.
 * Never returns Axios defaults like "Request failed with status code 403".
 */
export function getUserFacingApiErrorMessage(
  error: unknown,
  fallback: string
): string {
  if (isAxiosError(error)) {
    const bodyMessage = readResponseMessage(error.response?.data)
    if (bodyMessage != null) {
      return bodyMessage
    }
    if (error.response?.status === 403) {
      return API_PERMISSION_DENIED_MESSAGE
    }
  }

  if (error instanceof Error && error.message.trim() !== "") {
    const message = error.message.trim()
    if (!isAxiosStatusMessage(message)) {
      return message
    }
    if (isAxiosError(error) && error.response?.status === 403) {
      return API_PERMISSION_DENIED_MESSAGE
    }
  }

  return fallback
}

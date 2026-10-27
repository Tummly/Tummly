import { isAxiosError } from "axios"

/**
 * Reads the machine-readable `code` from a Shop API error body
 * (`{ success: false, code, message, ... }`). Returns null for anything else.
 */
export function readShopApiErrorCode(error: unknown): string | null {
  if (!isAxiosError(error)) {
    return null
  }
  const data = error.response?.data as { code?: unknown } | null | undefined
  if (data == null || typeof data !== "object") {
    return null
  }
  return typeof data.code === "string" ? data.code : null
}

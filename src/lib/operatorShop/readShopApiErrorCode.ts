import { isAxiosError } from "axios"

/**
 * Reads the JSON body of a Shop API error (`{ success: false, code, message, ... }`).
 * Returns null for anything that is not an Axios error with an object body.
 */
export function readShopApiErrorBody(
  error: unknown
): Record<string, unknown> | null {
  if (!isAxiosError(error)) {
    return null
  }
  const data: unknown = error.response?.data
  if (data == null || typeof data !== "object") {
    return null
  }
  return data as Record<string, unknown>
}

/**
 * Reads the machine-readable `code` from a Shop API error body.
 * Returns null for anything else.
 */
export function readShopApiErrorCode(error: unknown): string | null {
  const code = readShopApiErrorBody(error)?.code
  return typeof code === "string" ? code : null
}

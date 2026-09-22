import { API_BASE_URL } from "@/config/api"

export type ExternalAuthProvider = "google" | "microsoft"

/**
 * Builds the backend OAuth start URL for Google / Microsoft.
 * Pass `apiBase` in tests; production uses `VITE_API_BASE_URL` (already includes `/api`).
 */
export function buildExternalAuthStartUrl(
  provider: ExternalAuthProvider,
  returnPath: string,
  apiBase: string = API_BASE_URL
): string {
  const base = apiBase.replace(/\/$/, "")
  const url = new URL(`${base}/auth/external/${provider}/start`)
  url.searchParams.set("returnPath", returnPath)
  return url.toString()
}

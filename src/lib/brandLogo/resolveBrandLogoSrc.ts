import { API_BASE_URL } from "@/config/api"

/**
 * Resolve a Brand logo public path (`/api/public/brand-logos/...`) to an
 * absolute URL the browser can load from the API host.
 */
export function resolveBrandLogoSrc(
  publicUrl: string | null | undefined
): string | null {
  if (publicUrl == null || publicUrl.trim() === "") {
    return null
  }

  const trimmed = publicUrl.trim()
  if (/^https?:\/\//i.test(trimmed)) {
    return trimmed
  }

  const apiOrigin = API_BASE_URL.replace(/\/api\/?$/, "")
  if (trimmed.startsWith("/")) {
    return `${apiOrigin}${trimmed}`
  }

  return `${apiOrigin}/${trimmed}`
}

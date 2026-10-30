import type { PublicTrustIndicatorsState } from "@/hooks/usePublicTrustIndicators"

/**
 * Public trust indicators stay withheld until the Director approves the
 * publication (CONTRACT §9). Keep this a literal `false`; the dev preview below
 * is the only way to see the indicators before approval.
 */
export const PUBLIC_TRUST_CLAIMS_APPROVED = false

/** localStorage key that turns the dev-only trust preview on (value "1"). */
const TRUST_PREVIEW_KEY = "tummly.trustPreview"

function readTrustPreviewFlag(): string | null {
  try {
    if (typeof localStorage === "undefined") return null
    return localStorage.getItem(TRUST_PREVIEW_KEY)
  } catch {
    // Storage can be blocked by browser settings; treat as no preview.
    return null
  }
}

/**
 * Whether public trust indicators may render. A production build never
 * previews, even with the local key set.
 */
export function trustClaimsVisible(): boolean {
  if (PUBLIC_TRUST_CLAIMS_APPROVED) return true
  if (!import.meta.env.DEV) return false
  return readTrustPreviewFlag() === "1"
}

/**
 * The items a public surface may render (CONTRACT §9): nothing unless the gate
 * is open and the request is `ready`. Callers still treat an empty list as
 * "render no trust block".
 */
export function visibleTrustIndicators(
  trust: PublicTrustIndicatorsState,
): PublicTrustIndicatorsState["items"] {
  if (!trustClaimsVisible() || trust.state !== "ready") return []
  return trust.items
}

import {
  FOOTER_TRUST_BADGES,
  type FooterTrustBadge,
} from "@/content/marketing/footerTrustBadges"
import {
  PRODUCT_TRUST,
  type ProductCertification,
} from "@/content/marketing/productPage"

/**
 * Public certification / scheme badges stay withheld until the Director
 * approves each claim with a working proof link (ENG01, E3). The 15-indicator
 * trust model (ENG10.3) replaces this gate. Keep this a literal `false`; the
 * dev preview below is the only way to see the claims before approval.
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

/** The gate every public helper goes through; exported so tests can flip it. */
export function gateTrustClaims<T>(
  approved: boolean,
  claims: readonly T[],
): readonly T[] {
  return approved ? claims : []
}

/** Home carousel shows the same 15 badges as the footer grid. */
export function publicHomeTrustBadges(): readonly FooterTrustBadge[] {
  return gateTrustClaims(PUBLIC_TRUST_CLAIMS_APPROVED, FOOTER_TRUST_BADGES)
}

export function publicFooterTrustBadges(): readonly FooterTrustBadge[] {
  return gateTrustClaims(PUBLIC_TRUST_CLAIMS_APPROVED, FOOTER_TRUST_BADGES)
}

/** Product Trust & privacy section carousel (Figma `6937:13422`). */
export function publicProductTrustCertifications(): readonly ProductCertification[] {
  return gateTrustClaims(
    PUBLIC_TRUST_CLAIMS_APPROVED,
    PRODUCT_TRUST.certifications,
  )
}

/** For Groups hero trust strip (Figma `6424:16986`) shows the Product badges. */
export function publicForGroupsTrustBadges(): readonly ProductCertification[] {
  return gateTrustClaims(
    PUBLIC_TRUST_CLAIMS_APPROVED,
    PRODUCT_TRUST.certifications,
  )
}

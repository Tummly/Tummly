import {
  FOOTER_TRUST_BADGES,
  type FooterTrustBadge,
} from "@/content/marketing/footerTrustBadges"

/**
 * Public certification / scheme badges stay withheld until the Director
 * approves each claim with a working proof link (ENG01, E3). The 15-indicator
 * trust model (ENG10.3) replaces this gate.
 */
const PUBLIC_TRUST_CLAIMS_APPROVED = false

/** Home carousel shows the same 15 badges as the footer grid. */
export function publicHomeTrustBadges(): readonly FooterTrustBadge[] {
  return PUBLIC_TRUST_CLAIMS_APPROVED ? FOOTER_TRUST_BADGES : []
}

export function publicFooterTrustBadges(): readonly FooterTrustBadge[] {
  return PUBLIC_TRUST_CLAIMS_APPROVED ? FOOTER_TRUST_BADGES : []
}

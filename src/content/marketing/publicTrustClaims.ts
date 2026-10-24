import {
  MARKETING_FOOTER_TRUST_CREDENTIALS,
  type MarketingTrustCredential,
} from "@/constants/marketingNav"
import {
  FOR_GROUPS_HERO_TRUST,
  type ForGroupsHeroTrustBadge,
} from "@/content/marketing/forGroupsPage"
import {
  HOME_TRUST_CONTROL,
  type HomeTrustBadge,
} from "@/content/marketing/homePage"

/**
 * Public certification / scheme badges stay withheld until the Director
 * approves each claim with a working proof link (ENG01, E3). The 15-indicator
 * trust model (ENG10.3) replaces this gate.
 */
const PUBLIC_TRUST_CLAIMS_APPROVED = false

export function publicHomeTrustBadges(): readonly HomeTrustBadge[] {
  return PUBLIC_TRUST_CLAIMS_APPROVED ? HOME_TRUST_CONTROL.badges : []
}

export function publicForGroupsTrustBadges(): readonly ForGroupsHeroTrustBadge[] {
  return PUBLIC_TRUST_CLAIMS_APPROVED ? FOR_GROUPS_HERO_TRUST.badges : []
}

export function publicFooterTrustCredentials(): readonly MarketingTrustCredential[] {
  return PUBLIC_TRUST_CLAIMS_APPROVED ? MARKETING_FOOTER_TRUST_CREDENTIALS : []
}

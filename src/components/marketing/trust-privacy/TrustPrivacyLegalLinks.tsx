import { LegalRelatedLinks } from "@/components/legal/LegalRelatedLinks"
import { LEGAL_RELATED_LINKS } from "@/content/legal/legalRelatedLinks"

/**
 * Figma “Want the detail?” (`4974:26935`).
 * Reuses the shared legal related-link rows with a section heading.
 */
export function TrustPrivacyLegalLinks() {
  return (
    <LegalRelatedLinks
      title="Want the detail?"
      links={LEGAL_RELATED_LINKS}
      className="bg-white"
    />
  )
}

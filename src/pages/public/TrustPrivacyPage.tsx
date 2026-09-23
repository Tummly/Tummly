import { useEffect } from "react"

import Footer from "@/components/home/Footer"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { TrustPrivacyHero } from "@/components/marketing/trust-privacy/TrustPrivacyHero"
import { TrustPrivacyLegalLinks } from "@/components/marketing/trust-privacy/TrustPrivacyLegalLinks"
import { TrustPrivacyPermissionSection } from "@/components/marketing/trust-privacy/TrustPrivacyPermissionSection"
import { TrustPrivacyProseBlock } from "@/components/marketing/trust-privacy/TrustPrivacyProseBlock"
import {
  TRUST_PRIVACY_PROSE_SECTIONS,
  TRUST_PRIVACY_SIGN_UP_CTA,
} from "@/content/marketing/trustPrivacyPage"
import { warmCtaLaunchBg } from "@/lib/prefetchCtaLaunchBg"

/** Figma Trust & privacy (`4974:26874`). */
export default function TrustPrivacyPage() {
  useEffect(() => {
    warmCtaLaunchBg({ priority: true })
  }, [])

  return (
    <>
      <TrustPrivacyHero />
      <TrustPrivacyPermissionSection />
      {TRUST_PRIVACY_PROSE_SECTIONS.map((section) => (
        <TrustPrivacyProseBlock key={section.id} section={section} />
      ))}
      <TrustPrivacyLegalLinks />
      <MarketingSignUpCta content={TRUST_PRIVACY_SIGN_UP_CTA} />
      <Footer />
    </>
  )
}

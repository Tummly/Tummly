import signUpBackground from "@/assets/images/marketing/for-groups/sign-up-bg.webp"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { FOR_GROUPS_SIGN_UP_CTA } from "@/content/marketing/forGroupsPage"

/**
 * For Groups bottom CTA — Figma Sign up `6405:36178` (1668×615).
 * Editorial variant (dark type on a heat-map plate), "read" legal note.
 */
export function ForGroupsSignUpCta() {
  return (
    <MarketingSignUpCta
      content={FOR_GROUPS_SIGN_UP_CTA}
      variant="editorial"
      editorialBackgroundSrc={signUpBackground}
      editorialBackgroundWidth={2160}
      editorialBackgroundHeight={2160}
      editorialAspectClass="lg:aspect-[1668/615] lg:min-h-[615px]"
      bodyClassName="md:max-w-[580px]"
    />
  )
}

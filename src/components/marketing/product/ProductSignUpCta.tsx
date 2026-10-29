import signUpBackground from "@/assets/images/marketing/product/sign-up-bg.webp"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { PRODUCT_SIGN_UP_CTA } from "@/content/marketing/productPage"

/**
 * Product bottom CTA — Figma Sign up `6824:12527` (1668×595).
 * Editorial variant (dark type on lime marble), no trust badge, "read" legal note.
 */
export function ProductSignUpCta() {
  return (
    <MarketingSignUpCta
      content={PRODUCT_SIGN_UP_CTA}
      variant="editorial"
      editorialBackgroundSrc={signUpBackground}
      editorialBackgroundWidth={2400}
      editorialBackgroundHeight={1000}
      editorialAspectClass="lg:aspect-[1668/595] lg:min-h-[576px]"
    />
  )
}

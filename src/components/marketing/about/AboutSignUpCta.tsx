import signUpBackground from "@/assets/images/marketing/about/signup-cta-bg-v2.webp"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { ABOUT_SIGN_UP_CTA } from "@/content/marketing/aboutPage"

/**
 * About page bottom CTA (Figma Sign up `5661:24423`, 1668×525).
 * Editorial blue-green marble variant of MarketingSignUpCta, "read" legal note.
 */
export function AboutSignUpCta() {
  return (
    <MarketingSignUpCta
      content={ABOUT_SIGN_UP_CTA}
      variant="editorial"
      editorialBackgroundSrc={signUpBackground}
      editorialBackgroundWidth={2400}
      editorialBackgroundHeight={1350}
      editorialAspectClass="lg:aspect-[1668/525] lg:min-h-[525px]"
      editorialButtonVariant="primary"
      editorialButtonColor="#14a946"
      socialButtonsClassName="bg-[#fafafa] hover:bg-[#fafafa]/90"
      legalNoteClassName="max-w-[348px]"
      titleClassName="md:max-w-[544px]"
      bodyClassName="md:max-w-[397px]"
    />
  )
}

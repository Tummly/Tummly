import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { ABOUT_SIGN_UP_CTA } from "@/content/marketing/aboutPage"

/**
 * About page bottom CTA (Figma Sign up `5661:24424`).
 * Editorial green-marble variant of MarketingSignUpCta.
 */
export function AboutSignUpCta() {
  return (
    <MarketingSignUpCta content={ABOUT_SIGN_UP_CTA} variant="editorial" />
  )
}

import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { FAQS_PAGE_SIGN_UP_CTA } from "@/content/marketing/faqsPage"

/**
 * FAQ page bottom CTA (Figma `Sign up` / `5661:14510`).
 * Layout lives in MarketingSignUpCta; copy follows the FAQ frame.
 */
export function FaqsSignUpCta() {
  return <MarketingSignUpCta content={FAQS_PAGE_SIGN_UP_CTA} />
}

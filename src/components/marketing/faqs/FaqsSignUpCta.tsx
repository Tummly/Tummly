import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { FAQS_PAGE_SIGN_UP_CTA } from "@/content/marketing/faqsPage"
import { MARKETING_PRICING_PATH } from "@/constants/marketingNav"

/**
 * FAQ page bottom CTA (Figma `Sign up` / `4974:26424`).
 * Layout lives in MarketingSignUpCta; copy follows the FAQ frame.
 */
export function FaqsSignUpCta() {
  return (
    <MarketingSignUpCta
      content={{
        ...FAQS_PAGE_SIGN_UP_CTA,
        secondaryTo: MARKETING_PRICING_PATH,
      }}
    />
  )
}

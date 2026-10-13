import { useEffect } from "react"

import Footer from "@/components/home/Footer"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { ProductAiAssistant } from "@/components/marketing/product/ProductAiAssistant"
import { ProductCampaigns } from "@/components/marketing/product/ProductCampaigns"
import { ProductCapture } from "@/components/marketing/product/ProductCapture"
import { ProductControl } from "@/components/marketing/product/ProductControl"
import { ProductFaqs } from "@/components/marketing/product/ProductFaqs"
import { ProductFeedback } from "@/components/marketing/product/ProductFeedback"
import { ProductFeedbackRecovery } from "@/components/marketing/product/ProductFeedbackRecovery"
import { ProductGroups } from "@/components/marketing/product/ProductGroups"
import { ProductGuestLoop } from "@/components/marketing/product/ProductGuestLoop"
import { ProductGuests } from "@/components/marketing/product/ProductGuests"
import { ProductHero } from "@/components/marketing/product/ProductHero"
import { ProductReports } from "@/components/marketing/product/ProductReports"
import { ProductSystems } from "@/components/marketing/product/ProductSystems"
import { PRODUCT_SIGN_UP_CTA } from "@/content/marketing/productPage"
import { scheduleWarmSignUpCtaMedia } from "@/lib/prefetchSignUpCtaMedia"

/**
 * Figma marketing Product page —
 * hero `5661:22625` through FAQs `5661:24202` and Sign-up CTA.
 */
export default function ProductPage() {
  useEffect(() => scheduleWarmSignUpCtaMedia(), [])

  return (
    <>
      <ProductHero />
      <ProductGuestLoop />
      <ProductCapture />
      <ProductFeedback />
      <ProductFeedbackRecovery />
      <ProductGuests />
      <ProductCampaigns />
      <ProductReports />
      <ProductAiAssistant />
      <ProductSystems />
      <ProductGroups />
      <ProductControl />
      <ProductFaqs />
      <MarketingSignUpCta content={PRODUCT_SIGN_UP_CTA} />
      <Footer />
    </>
  )
}

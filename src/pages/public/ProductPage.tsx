import Footer from "@/components/home/Footer"
import { ProductAiAssistant } from "@/components/marketing/product/ProductAiAssistant"
import { ProductCampaigns } from "@/components/marketing/product/ProductCampaigns"
import { ProductCapture } from "@/components/marketing/product/ProductCapture"
import { ProductFaqs } from "@/components/marketing/product/ProductFaqs"
import { ProductFeedback } from "@/components/marketing/product/ProductFeedback"
import { ProductFeedbackRecovery } from "@/components/marketing/product/ProductFeedbackRecovery"
import { ProductGroups } from "@/components/marketing/product/ProductGroups"
import { ProductGuests } from "@/components/marketing/product/ProductGuests"
import { ProductHero } from "@/components/marketing/product/ProductHero"
import { ProductOutcomes } from "@/components/marketing/product/ProductOutcomes"
import { ProductPricing } from "@/components/marketing/product/ProductPricing"
import { ProductReports } from "@/components/marketing/product/ProductReports"
import { ProductSignUpCta } from "@/components/marketing/product/ProductSignUpCta"
import { ProductTrust } from "@/components/marketing/product/ProductTrust"

/**
 * Figma marketing Product page — hero `5661:22625` through FAQs `5661:24202`
 * and Sign up `6824:12527`.
 */
export default function ProductPage() {
  return (
    <>
      <ProductHero />
      <ProductCapture />
      <ProductFeedback />
      <ProductFeedbackRecovery />
      <ProductGuests />
      <ProductCampaigns />
      <ProductReports />
      <ProductAiAssistant />
      <ProductOutcomes />
      <ProductGroups />
      <ProductTrust />
      <ProductPricing />
      <ProductFaqs />
      <ProductSignUpCta />
      <Footer />
    </>
  )
}

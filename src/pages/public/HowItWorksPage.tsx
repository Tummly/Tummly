import { useEffect } from "react"

import signupCtaBgImage from "@/assets/images/marketing/how-it-works/signup-cta-bg.webp"
import Footer from "@/components/home/Footer"
import { HomeServeStrip } from "@/components/home/HomeServeStrip"
import { AiAssistantPlans } from "@/components/marketing/ai-assistant/AiAssistantPlans"
import { HowItWorksFaqs } from "@/components/marketing/how-it-works/HowItWorksFaqs"
import { HowItWorksFeatures } from "@/components/marketing/how-it-works/HowItWorksFeatures"
import { HowItWorksGuestForm } from "@/components/marketing/how-it-works/HowItWorksGuestForm"
import { HowItWorksHero } from "@/components/marketing/how-it-works/HowItWorksHero"
import { HowItWorksSteps } from "@/components/marketing/how-it-works/HowItWorksSteps"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import {
  HOW_IT_WORKS_PLANS,
  HOW_IT_WORKS_SIGN_UP_CTA,
  HOW_IT_WORKS_STRIP,
} from "@/content/marketing/howItWorksPage"
import { scheduleWarmSignUpCtaMedia } from "@/lib/prefetchSignUpCtaMedia"

/**
 * Figma marketing How it works —
 * hero `5661:19524`, strip `6863:12786`, steps `6865:35312`,
 * Guest Form `5887:20699`, product areas `6870:35827`–`6871:35965`,
 * plans `5661:19770`, FAQs `6874:12909`, Sign-up CTA `6874:12985`.
 */
export default function HowItWorksPage() {
  useEffect(() => scheduleWarmSignUpCtaMedia(), [])

  return (
    <>
      <HowItWorksHero />
      <HomeServeStrip
        eyebrow={HOW_IT_WORKS_STRIP.eyebrow}
        eyebrowClassName="font-medium text-[#676767]"
        headingId="how-it-works-strip-heading"
      />
      <HowItWorksSteps />
      <HowItWorksGuestForm />
      <HowItWorksFeatures />
      <AiAssistantPlans
        content={HOW_IT_WORKS_PLANS}
        showAiAllowance={false}
        splitHeader
        compareBelow
        sublineSource="card"
        titleClassName="max-w-[503px]"
        eyebrowClassName="font-sans font-semibold text-[#4a4a4a]"
        bodyClassName="max-w-[422px]"
        figmaColumns
      />
      <HowItWorksFaqs />
      <MarketingSignUpCta
        content={HOW_IT_WORKS_SIGN_UP_CTA}
        variant="editorial"
        editorialBackgroundSrc={signupCtaBgImage}
        editorialBackgroundWidth={2504}
        editorialBackgroundHeight={802}
        editorialAspectClass="lg:aspect-[1668/534] lg:min-h-[534px]"
        titleClassName="md:max-w-[583px]"
        bodyClassName="md:max-w-[590px]"
        socialButtonsClassName="bg-[#fafafa] hover:bg-white"
        frameClassName="rounded-[12px]"
        legalNoteClassName="max-w-[400px]"
      />
      <Footer />
    </>
  )
}

import { useEffect } from "react"

import Footer from "@/components/home/Footer"
import { HomeQrMaterials } from "@/components/home/HomeQrMaterials"
import { AiAssistantPlans } from "@/components/marketing/ai-assistant/AiAssistantPlans"
import { HowItWorksFaqs } from "@/components/marketing/how-it-works/HowItWorksFaqs"
import { HowItWorksFeatures } from "@/components/marketing/how-it-works/HowItWorksFeatures"
import { HowItWorksGuestForm } from "@/components/marketing/how-it-works/HowItWorksGuestForm"
import { HowItWorksHero } from "@/components/marketing/how-it-works/HowItWorksHero"
import { HowItWorksLoopIntro } from "@/components/marketing/how-it-works/HowItWorksLoopIntro"
import { HowItWorksSteps } from "@/components/marketing/how-it-works/HowItWorksSteps"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import {
  HOW_IT_WORKS_PLANS,
  HOW_IT_WORKS_SIGN_UP_CTA,
} from "@/content/marketing/howItWorksPage"
import { scheduleWarmSignUpCtaMedia } from "@/lib/prefetchSignUpCtaMedia"

/**
 * Figma marketing How it works —
 * hero `5661:19524`, loop intro `5661:19534`, steps `5661:19539`–`5667:32320`,
 * QR materials (home `4902:19649`), Guest Form `5887:20697` (+ variants),
 * product areas `5661:19613`–`5667:32617`, plans `5661:19770`,
 * FAQs `5661:19821`, Sign-up CTA.
 */
export default function HowItWorksPage() {
  useEffect(() => scheduleWarmSignUpCtaMedia(), [])

  return (
    <>
      <HowItWorksHero />
      <HowItWorksLoopIntro />
      <HowItWorksSteps />
      <HomeQrMaterials />
      <HowItWorksGuestForm />
      <HowItWorksFeatures />
      <AiAssistantPlans
        content={HOW_IT_WORKS_PLANS}
        showAiAllowance={false}
        surfaceClassName="bg-white"
      />
      <HowItWorksFaqs />
      <MarketingSignUpCta content={HOW_IT_WORKS_SIGN_UP_CTA} />
      <Footer />
    </>
  )
}

import { useEffect } from "react"

import Footer from "@/components/home/Footer"
import { AiAssistantFaqs } from "@/components/marketing/ai-assistant/AiAssistantFaqs"
import { AiAssistantGuestLoop } from "@/components/marketing/ai-assistant/AiAssistantGuestLoop"
import { AiAssistantHero } from "@/components/marketing/ai-assistant/AiAssistantHero"
import { AiAssistantHowItWorks } from "@/components/marketing/ai-assistant/AiAssistantHowItWorks"
import { AiAssistantMidCta } from "@/components/marketing/ai-assistant/AiAssistantMidCta"
import { AiAssistantPlans } from "@/components/marketing/ai-assistant/AiAssistantPlans"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { AI_ASSISTANT_SIGN_UP_CTA } from "@/content/marketing/aiAssistantPage"
import { scheduleWarmAiAssistantMedia } from "@/lib/prefetchAiAssistantMedia"
import { scheduleWarmSignUpCtaMedia } from "@/lib/prefetchSignUpCtaMedia"

/** Figma marketing AI Assistant (`5661:19330`). */
export default function AiAssistantPage() {
  useEffect(() => {
    const cancelAi = scheduleWarmAiAssistantMedia()
    const cancelCta = scheduleWarmSignUpCtaMedia()
    return () => {
      cancelAi()
      cancelCta()
    }
  }, [])

  return (
    <>
      <AiAssistantHero />
      <AiAssistantGuestLoop />
      <AiAssistantHowItWorks />
      <AiAssistantMidCta />
      <AiAssistantPlans />
      <AiAssistantFaqs />
      <MarketingSignUpCta content={AI_ASSISTANT_SIGN_UP_CTA} />
      <Footer />
    </>
  )
}

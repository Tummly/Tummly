import { useEffect } from "react"

import Footer from "@/components/home/Footer"
import { AiAssistantPlans } from "@/components/marketing/ai-assistant/AiAssistantPlans"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { PilotActivationJourney } from "@/components/marketing/pilot/PilotActivationJourney"
import { PilotDay30 } from "@/components/marketing/pilot/PilotDay30"
import { PilotEvidence } from "@/components/marketing/pilot/PilotEvidence"
import { PilotFaqs } from "@/components/marketing/pilot/PilotFaqs"
import { PilotHero } from "@/components/marketing/pilot/PilotHero"
import { PilotPhysicalDeployment } from "@/components/marketing/pilot/PilotPhysicalDeployment"
import { PilotPurpose } from "@/components/marketing/pilot/PilotPurpose"
import { PilotSetup } from "@/components/marketing/pilot/PilotSetup"
import { PilotTesting } from "@/components/marketing/pilot/PilotTesting"
import { PilotWhyZero } from "@/components/marketing/pilot/PilotWhyZero"
import { PILOT_PLANS, PILOT_SIGN_UP_CTA } from "@/content/marketing/pilotPage"
import { scheduleWarmSignUpCtaMedia } from "@/lib/prefetchSignUpCtaMedia"

/**
 * Figma marketing 30-day Pilot page —
 * hero `5661:19874`, purpose `5661:19885`, testing `5661:19910`,
 * setup `5661:19947`, activation `5681:46039`, physical `5681:46057`,
 * evidence `5661:20056`, day-30 `5661:20456`, plans `5679:34414`,
 * why-£0 `5681:46128`, FAQs `5661:20468`, Sign-up CTA (media/video).
 */
export default function PilotPage() {
  useEffect(() => scheduleWarmSignUpCtaMedia(), [])

  return (
    <>
      <PilotHero />
      <PilotPurpose />
      <PilotTesting />
      <PilotSetup />
      <PilotActivationJourney />
      <PilotPhysicalDeployment />
      <PilotEvidence />
      <PilotDay30 />
      <AiAssistantPlans content={PILOT_PLANS} showAiAllowance={false} />
      <PilotWhyZero />
      <PilotFaqs />
      <MarketingSignUpCta content={PILOT_SIGN_UP_CTA} variant="media" />
      <Footer />
    </>
  )
}

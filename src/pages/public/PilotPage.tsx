import Footer from "@/components/home/Footer"
import { AiAssistantPlans } from "@/components/marketing/ai-assistant/AiAssistantPlans"
import { PilotFaqs } from "@/components/marketing/pilot/PilotFaqs"
import { PilotFirstThirtyDays } from "@/components/marketing/pilot/PilotFirstThirtyDays"
import { PilotGettingStarted } from "@/components/marketing/pilot/PilotGettingStarted"
import { PilotHero } from "@/components/marketing/pilot/PilotHero"
import { PilotInAction } from "@/components/marketing/pilot/PilotInAction"
import { PilotSignUpCta } from "@/components/marketing/pilot/PilotSignUpCta"
import { PilotWhyTummly } from "@/components/marketing/pilot/PilotWhyTummly"
import { PilotWhyZero } from "@/components/marketing/pilot/PilotWhyZero"
import { PILOT_PLANS } from "@/content/marketing/pilotPage"

/**
 * Figma marketing 30-day Pilot page —
 * hero `5661:19874`, why Tummly `6543:18855`,
 * in action `6495:18145`–`6495:18254`, getting started `6476:16855`,
 * 30-day panel `5681:46128`, first 30 days `6476:16898`,
 * plans `5679:34414`, FAQs `5661:20468`, split sign-up CTA `6464:35738`.
 */
export default function PilotPage() {
  return (
    <>
      <PilotHero />
      <PilotWhyTummly />
      <PilotInAction />
      <PilotGettingStarted />
      <PilotWhyZero />
      <PilotFirstThirtyDays />
      <AiAssistantPlans
        content={PILOT_PLANS}
        showAiAllowance={false}
        surfaceClassName="bg-[#ffffff]"
      />
      <PilotFaqs />
      <PilotSignUpCta />
      <Footer />
    </>
  )
}

import { useEffect } from "react"

import Footer from "@/components/home/Footer"
import { ForGroupsBuiltFor } from "@/components/marketing/for-groups/ForGroupsBuiltFor"
import { ForGroupsFaqs } from "@/components/marketing/for-groups/ForGroupsFaqs"
import {
  ForGroupsAi,
  ForGroupsCampaigns,
  ForGroupsFeedback,
  ForGroupsReporting,
  ForGroupsTeams,
} from "@/components/marketing/for-groups/ForGroupsFeatureSections"
import { ForGroupsHero } from "@/components/marketing/for-groups/ForGroupsHero"
import { ForGroupsPricing } from "@/components/marketing/for-groups/ForGroupsPricing"
import { ForGroupsProduct } from "@/components/marketing/for-groups/ForGroupsProduct"
import { ForGroupsQrMaterials } from "@/components/marketing/for-groups/ForGroupsQrMaterials"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { FOR_GROUPS_SIGN_UP_CTA } from "@/content/marketing/forGroupsPage"
import { scheduleWarmSignUpCtaMedia } from "@/lib/prefetchSignUpCtaMedia"

/**
 * Figma marketing For Groups page —
 * hero `5661:20525`, Product `6320:19475`, Built for `5661:20535`,
 * QR materials `5661:20711`, feature bands `5661:20730` … `5661:22127`,
 * pricing `5661:22463`, FAQs `5661:22571`, Sign-up CTA `5661:22602`.
 */
export default function ForGroupsPage() {
  useEffect(() => scheduleWarmSignUpCtaMedia(), [])

  return (
    <>
      <ForGroupsHero />
      <ForGroupsProduct />
      <ForGroupsBuiltFor />
      <ForGroupsQrMaterials />
      <ForGroupsReporting />
      <ForGroupsFeedback />
      <ForGroupsTeams />
      <ForGroupsCampaigns />
      <ForGroupsAi />
      <ForGroupsPricing />
      <ForGroupsFaqs />
      <MarketingSignUpCta content={FOR_GROUPS_SIGN_UP_CTA} variant="media" />
      <Footer />
    </>
  )
}

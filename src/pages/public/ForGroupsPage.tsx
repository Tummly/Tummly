import Footer from "@/components/home/Footer"
import { ForGroupsFaqs } from "@/components/marketing/for-groups/ForGroupsFaqs"
import {
  ForGroupsAi,
  ForGroupsCampaigns,
  ForGroupsLocationManagement,
  ForGroupsReporting,
  ForGroupsTeams,
} from "@/components/marketing/for-groups/ForGroupsFeatureSections"
import { ForGroupsHero } from "@/components/marketing/for-groups/ForGroupsHero"
import { ForGroupsManaging } from "@/components/marketing/for-groups/ForGroupsManaging"
import { ForGroupsPricing } from "@/components/marketing/for-groups/ForGroupsPricing"
import { ForGroupsProduct } from "@/components/marketing/for-groups/ForGroupsProduct"
import { ForGroupsSignUpCta } from "@/components/marketing/for-groups/ForGroupsSignUpCta"

/**
 * Figma marketing For Groups page —
 * hero `5661:20525`, Managing multiple restaurants `5661:20535`, Keep the
 * Guest Loop working `6405:36122`, Group-wide visibility `5661:20730`,
 * Adding your restaurants `5661:21077`, Team access `5661:21424`, Offers &
 * campaigns `5661:21775`, AI assistant `5661:22127`, pricing `5661:22463`,
 * FAQs `5661:22571`, Sign-up CTA `6405:36178`.
 */
export default function ForGroupsPage() {
  return (
    <>
      <ForGroupsHero />
      <ForGroupsManaging />
      <ForGroupsProduct />
      <ForGroupsReporting />
      <ForGroupsLocationManagement />
      <ForGroupsTeams />
      <ForGroupsCampaigns />
      <ForGroupsAi />
      <ForGroupsPricing />
      <ForGroupsFaqs />
      <ForGroupsSignUpCta />
      <Footer />
    </>
  )
}

import { useState } from "react"

import Footer from "@/components/home/Footer"
import { PricingComparePlans } from "@/components/marketing/pricing/PricingComparePlans"
import { PricingCustomisePlanSection } from "@/components/marketing/pricing/PricingCustomisePlanSection"
import { PricingFaqs } from "@/components/marketing/pricing/PricingFaqs"
import { PricingGuestLoop } from "@/components/marketing/pricing/PricingGuestLoop"
import { PricingGuestRelationships } from "@/components/marketing/pricing/PricingGuestRelationships"
import { PricingHeroPlans } from "@/components/marketing/pricing/PricingHeroPlans"
import { PricingHowUsageWorks } from "@/components/marketing/pricing/PricingHowUsageWorks"
import { PricingQrMaterials } from "@/components/marketing/pricing/PricingQrMaterials"
import { PricingSectorsStrip } from "@/components/marketing/pricing/PricingSectorsStrip"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"

/** Figma marketing Pricing (`4974:29048`): FAQs, then the footer (no sign-up CTA). */
export default function PricingPage() {
  const [cadence, setCadence] = useState<BillingCadence>("monthly")

  return (
    <>
      <PricingHeroPlans cadence={cadence} onCadenceChange={setCadence} />
      <PricingCustomisePlanSection />
      <PricingSectorsStrip />
      <PricingGuestLoop />
      <PricingComparePlans cadence={cadence} />
      <PricingHowUsageWorks />
      <PricingQrMaterials />
      <PricingGuestRelationships />
      <PricingFaqs />
      <Footer />
    </>
  )
}

import { useEffect, useState } from "react"

import Footer from "@/components/home/Footer"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { PricingComparePlans } from "@/components/marketing/pricing/PricingComparePlans"
import { PricingFaqs } from "@/components/marketing/pricing/PricingFaqs"
import { PricingGuestLoop } from "@/components/marketing/pricing/PricingGuestLoop"
import { PricingHeroPlans } from "@/components/marketing/pricing/PricingHeroPlans"
import { PricingQrMaterials } from "@/components/marketing/pricing/PricingQrMaterials"
import { PricingUsage } from "@/components/marketing/pricing/PricingUsage"
import {
  PRICING_SIGN_UP_CTA,
} from "@/content/marketing/pricingPage"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import { warmCtaLaunchBg } from "@/lib/prefetchCtaLaunchBg"

/** Figma marketing Pricing (`4974:29048`). */
export default function PricingPage() {
  const [cadence, setCadence] = useState<BillingCadence>("monthly")

  useEffect(() => {
    warmCtaLaunchBg({ priority: true })
  }, [])

  return (
    <>
      <PricingHeroPlans cadence={cadence} onCadenceChange={setCadence} />
      <PricingGuestLoop />
      <PricingComparePlans cadence={cadence} />
      <PricingUsage />
      <PricingQrMaterials />
      <PricingFaqs />
      <MarketingSignUpCta content={PRICING_SIGN_UP_CTA} />
      <Footer />
    </>
  )
}

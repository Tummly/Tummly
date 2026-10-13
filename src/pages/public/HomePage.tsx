import { useEffect } from "react"

import Footer from "@/components/home/Footer"
import { HomeHowItWorks } from "@/components/home/HomeHowItWorks"
import { HomeHowItWorksSteps } from "@/components/home/HomeHowItWorksSteps"
import { HomeIndustries } from "@/components/home/HomeIndustries"
import { HomePricing } from "@/components/home/HomePricing"
import { HomeProduct } from "@/components/home/HomeProduct"
import { HomeQrMaterials } from "@/components/home/HomeQrMaterials"
import { HomeTrustControl } from "@/components/home/HomeTrustControl"
import { HomeValueProps } from "@/components/home/HomeValueProps"
import Hero from "@/components/home/Hero"
import { MarketingFixedDemoQr } from "@/components/marketing/MarketingFixedDemoQr"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { HOME_SIGN_UP_CTA } from "@/content/marketing/homePage"
import { scheduleWarmSignUpCtaMedia } from "@/lib/prefetchSignUpCtaMedia"

function HomePage() {
  useEffect(() => scheduleWarmSignUpCtaMedia(), [])

  return (
    <>
      <Hero />
      <HomeValueProps />
      <HomeHowItWorks />
      <HomeHowItWorksSteps />
      <HomeQrMaterials />
      <HomeIndustries />
      <HomeProduct />
      <HomeTrustControl />
      <HomePricing />
      <MarketingSignUpCta content={HOME_SIGN_UP_CTA} />
      <Footer />
      <MarketingFixedDemoQr />
    </>
  )
}

export default HomePage

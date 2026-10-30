import { useEffect } from "react"

import Footer from "@/components/home/Footer"
import { HomeHowItWorks } from "@/components/home/HomeHowItWorks"
import { HomeHowItWorksSteps } from "@/components/home/HomeHowItWorksSteps"
import { HomeIndustries } from "@/components/home/HomeIndustries"
import { HomePricing } from "@/components/home/HomePricing"
import { HomePlatform } from "@/components/home/HomePlatform"
import { HomeQrMaterials } from "@/components/home/HomeQrMaterials"
import { HomeServeStrip } from "@/components/home/HomeServeStrip"
import { HomeTrustControl } from "@/components/home/HomeTrustControl"
import Hero from "@/components/home/Hero"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { HOME_SIGN_UP_CTA } from "@/content/marketing/homePage"
import { scheduleWarmSignUpCtaMedia } from "@/lib/prefetchSignUpCtaMedia"

function HomePage() {
  useEffect(() => scheduleWarmSignUpCtaMedia(), [])

  return (
    <>
      <Hero />
      <HomeServeStrip />
      <HomePlatform />
      <HomeHowItWorks />
      <HomeHowItWorksSteps />
      <HomeQrMaterials />
      <HomeIndustries />
      <HomeTrustControl />
      <HomePricing />
      <MarketingSignUpCta content={HOME_SIGN_UP_CTA} />
      <Footer />
    </>
  )
}

export default HomePage

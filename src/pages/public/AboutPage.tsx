import Footer from "@/components/home/Footer"
import { AboutAmbition } from "@/components/marketing/about/AboutAmbition"
import { AboutHero } from "@/components/marketing/about/AboutHero"
import { AboutIndustries } from "@/components/marketing/about/AboutIndustries"
import { AboutPrinciples } from "@/components/marketing/about/AboutPrinciples"
import { AboutReality } from "@/components/marketing/about/AboutReality"
import { AboutSignUpCta } from "@/components/marketing/about/AboutSignUpCta"

/**
 * Figma About Tummly —
 * hero `5673:24144`, industries `5685:35427`, principles `5673:24211`,
 * reality `5673:24194`, ambition `5673:24231`, sign-up `5661:24424`.
 */
export default function AboutPage() {
  return (
    <>
      <AboutHero />
      <AboutIndustries />
      <AboutPrinciples />
      <AboutReality />
      <AboutAmbition />
      <AboutSignUpCta />
      <Footer />
    </>
  )
}

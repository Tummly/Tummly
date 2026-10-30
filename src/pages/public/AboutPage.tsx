import Footer from "@/components/home/Footer"
import { AboutCompany } from "@/components/marketing/about/AboutCompany"
import { AboutHero } from "@/components/marketing/about/AboutHero"
import { AboutIndustries } from "@/components/marketing/about/AboutIndustries"
import { AboutMission } from "@/components/marketing/about/AboutMission"
import { AboutPrinciples } from "@/components/marketing/about/AboutPrinciples"
import { AboutReality } from "@/components/marketing/about/AboutReality"
import { AboutSignUpCta } from "@/components/marketing/about/AboutSignUpCta"

/**
 * Figma About Tummly —
 * hero `5673:24144`, industries `5685:35427`, principles `5673:24211`,
 * reality `5673:24194`, mission `7023:13862`, company `5673:24231`,
 * sign-up `5661:24423`.
 */
export default function AboutPage() {
  return (
    <>
      <AboutHero />
      <AboutIndustries />
      <AboutPrinciples />
      <AboutReality />
      <AboutMission />
      <AboutCompany />
      <AboutSignUpCta />
      <Footer />
    </>
  )
}

import { useEffect } from "react"

import signupCtaBgImage from "@/assets/images/marketing/qr-materials/signup-cta-bg.webp"
import Footer from "@/components/home/Footer"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { QrMaterialsBuiltFor } from "@/components/marketing/qr-materials/QrMaterialsBuiltFor"
import { QrMaterialsCatalogue } from "@/components/marketing/qr-materials/QrMaterialsCatalogue"
import { QrMaterialsCounterDisplay } from "@/components/marketing/qr-materials/QrMaterialsCounterDisplay"
import { QrMaterialsFaqs } from "@/components/marketing/qr-materials/QrMaterialsFaqs"
import { QrMaterialsFeedbackSteps } from "@/components/marketing/qr-materials/QrMaterialsFeedbackSteps"
import { QrMaterialsHero } from "@/components/marketing/qr-materials/QrMaterialsHero"
import { QrMaterialsPricing } from "@/components/marketing/qr-materials/QrMaterialsPricing"
import { QR_MATERIALS_SIGN_UP_CTA } from "@/content/marketing/qrMaterialsPage"
import { scheduleWarmSignUpCtaMedia } from "@/lib/prefetchSignUpCtaMedia"

/**
 * Figma marketing QR materials — hero `5661:19336`, catalogue `5661:19364`,
 * steps `5661:19391`, pricing `6859:12685`, Counter Display `6855:12460`,
 * Built for your restaurant `6853:12456`, FAQs `6858:12612`, Sign up `5661:19499`.
 */
export default function QrMaterialsPage() {
  useEffect(() => scheduleWarmSignUpCtaMedia(), [])

  return (
    <>
      <QrMaterialsHero />
      <QrMaterialsCatalogue />
      <QrMaterialsFeedbackSteps />
      <QrMaterialsPricing />
      <QrMaterialsCounterDisplay />
      <QrMaterialsBuiltFor />
      <QrMaterialsFaqs />
      <MarketingSignUpCta
        content={QR_MATERIALS_SIGN_UP_CTA}
        variant="editorial"
        editorialBackgroundSrc={signupCtaBgImage}
        editorialBackgroundWidth={1920}
        editorialBackgroundHeight={1920}
        editorialAspectClass="lg:aspect-[1668/534] lg:min-h-[534px]"
        editorialBackgroundClassName="object-[18%_center] lg:object-center"
        titleClassName="md:max-w-[536px]"
        bodyClassName="md:max-w-[403px]"
      />
      <Footer />
    </>
  )
}

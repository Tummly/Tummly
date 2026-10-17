import { useEffect } from "react"

import Footer from "@/components/home/Footer"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { QrMaterialsCatalogue } from "@/components/marketing/qr-materials/QrMaterialsCatalogue"
import { QrMaterialsConnectedSteps } from "@/components/marketing/qr-materials/QrMaterialsConnectedSteps"
import { QrMaterialsForGroups } from "@/components/marketing/qr-materials/QrMaterialsForGroups"
import { QrMaterialsGuestExperience } from "@/components/marketing/qr-materials/QrMaterialsGuestExperience"
import { QrMaterialsHero } from "@/components/marketing/qr-materials/QrMaterialsHero"
import { QrMaterialsNoWebsite } from "@/components/marketing/qr-materials/QrMaterialsNoWebsite"
import { QrMaterialsStarterKit } from "@/components/marketing/qr-materials/QrMaterialsStarterKit"
import { QrMaterialsStartingPoint } from "@/components/marketing/qr-materials/QrMaterialsStartingPoint"
import { QrMaterialsTouchpoints } from "@/components/marketing/qr-materials/QrMaterialsTouchpoints"
import { QR_MATERIALS_SIGN_UP_CTA } from "@/content/marketing/qrMaterialsPage"
import { scheduleWarmSignUpCtaMedia } from "@/lib/prefetchSignUpCtaMedia"

/**
 * Figma marketing QR materials —
 * hero `5661:19336` through for-groups `5661:19490` and Sign-up CTA.
 */
export default function QrMaterialsPage() {
  useEffect(() => scheduleWarmSignUpCtaMedia(), [])

  return (
    <>
      <QrMaterialsHero />
      <QrMaterialsTouchpoints />
      <QrMaterialsCatalogue />
      <QrMaterialsConnectedSteps />
      <QrMaterialsStartingPoint />
      <QrMaterialsNoWebsite />
      <QrMaterialsStarterKit />
      <QrMaterialsGuestExperience />
      <QrMaterialsForGroups />
      <MarketingSignUpCta content={QR_MATERIALS_SIGN_UP_CTA} />
      <Footer />
    </>
  )
}

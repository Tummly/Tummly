import Footer from "@/components/home/Footer"
import { LegalPageShell } from "@/components/legal/LegalPageShell"
import { shopPrintTermsContent } from "@/content/legal/legalPlaceholders"

export default function ShopPrintTermsPage() {
  return (
    <>
      <LegalPageShell content={shopPrintTermsContent} />
      <Footer />
    </>
  )
}

import Footer from "@/components/home/Footer"
import { LegalPageShell } from "@/components/legal/LegalPageShell"
import { acceptableUseContent } from "@/content/legal/legalPlaceholders"

export default function AcceptableUsePage() {
  return (
    <>
      <LegalPageShell content={acceptableUseContent} />
      <Footer />
    </>
  )
}

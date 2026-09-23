import Footer from "@/components/home/Footer"
import { LegalPageShell } from "@/components/legal/LegalPageShell"
import { accessibilityContent } from "@/content/legal/legalPlaceholders"

export default function AccessibilityPage() {
  return (
    <>
      <LegalPageShell content={accessibilityContent} />
      <Footer />
    </>
  )
}

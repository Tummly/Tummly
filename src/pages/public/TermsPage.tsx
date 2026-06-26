import Footer from "@/components/home/Footer"
import { LegalPageShell } from "@/components/legal/LegalPageShell"
import { termsOfServiceContent } from "@/content/legal/termsOfService"

export default function TermsPage() {
  return (
    <>
      <LegalPageShell content={termsOfServiceContent} />
      <Footer />
    </>
  )
}

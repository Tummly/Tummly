import Footer from "@/components/home/Footer"
import { LegalPageShell } from "@/components/legal/LegalPageShell"
import { privacyPolicyContent } from "@/content/legal/privacyPolicy.tsx"

export default function PrivacyPage() {
  return (
    <>
      <LegalPageShell content={privacyPolicyContent} />
      <Footer />
    </>
  )
}

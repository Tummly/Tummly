import Footer from "@/components/home/Footer"
import { LegalPageShell } from "@/components/legal/LegalPageShell"
import { cookiePolicyContent } from "@/content/legal/cookiePolicy"

export default function CookieSettingsPage() {
  return (
    <>
      <LegalPageShell content={cookiePolicyContent} />
      <Footer />
    </>
  )
}

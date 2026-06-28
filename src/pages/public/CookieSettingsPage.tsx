import Footer from "@/components/home/Footer"
import { CookiePreferences } from "@/components/legal/CookiePreferences"
import { LegalPageShell } from "@/components/legal/LegalPageShell"
import { cookiePolicyContent } from "@/content/legal/cookiePolicy"

export default function CookieSettingsPage() {
  return (
    <>
      <CookiePreferences />
      <LegalPageShell content={cookiePolicyContent} />
      <Footer />
    </>
  )
}

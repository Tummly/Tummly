import Footer from "@/components/home/Footer"
import { LegalPageShell } from "@/components/legal/LegalPageShell"
import { cookiePolicyContent } from "@/content/legal/cookiePolicy.tsx"

export default function CookiePolicyPage() {
  return (
    <>
      <LegalPageShell content={cookiePolicyContent} />
      <Footer />
    </>
  )
}

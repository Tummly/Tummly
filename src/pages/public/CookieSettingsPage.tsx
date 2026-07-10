import Footer from "@/components/home/Footer"
import { CookiePreferences } from "@/components/legal/CookiePreferences"
import { Link } from "react-router-dom"

import { LEGAL_ROUTES } from "@/constants/legalRoutes"

export default function CookieSettingsPage() {
  return (
    <>
      <main className="w-full bg-white text-[#141414]">
        <div className="mx-auto flex w-full max-w-7xl flex-col gap-2 px-4 pt-10 sm:px-6 sm:pt-12 md:px-10 lg:px-16 lg:pt-16 xl:px-20 2xl:max-w-[108rem] 2xl:px-45">
          <header className="flex max-w-3xl flex-col gap-3">
            <h1 className="m-0 text-[clamp(2rem,5vw,2.875rem)] font-bold leading-normal text-[#141414]">
              Cookie settings
            </h1>
            <p className="m-0 text-base font-medium leading-6 text-[#141414] sm:text-lg sm:leading-6">
              Manage optional cookies for this browser. Read the{" "}
              <Link
                to={LEGAL_ROUTES.cookiePolicy}
                className="font-medium underline underline-offset-2"
              >
                Cookie Policy
              </Link>{" "}
              for the full legal explanation.
            </p>
          </header>
        </div>
        <CookiePreferences />
      </main>
      <Footer />
    </>
  )
}

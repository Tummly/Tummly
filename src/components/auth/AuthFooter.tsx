import { Link } from "react-router-dom"

import { CookieSettingsTrigger } from "@/components/common/CookieSettingsDialog"
import { HELP_CENTRE_URL } from "@/config/support"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { prefetchHelpCentreHero } from "@/lib/prefetchHelpCentreHero"

const footerLinkClass =
  "rounded-sm text-sm font-medium text-[#555] no-underline transition-colors hover:text-[#232323] hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"

const footerLinks = [
  { label: "Help Centre", href: HELP_CENTRE_URL },
  { label: "Terms", href: LEGAL_ROUTES.terms },
  { label: "Privacy", href: LEGAL_ROUTES.privacy },
] as const

export function AuthFooter() {
  return (
    <footer className="relative z-10 w-full shrink-0 px-5 pb-6 pt-4 sm:px-6 lg:px-10 lg:pb-10">
      <nav
        aria-label="Auth footer"
        className="flex w-full flex-wrap items-center justify-between gap-x-6 gap-y-3 text-sm font-medium text-[#555]"
      >
        <span>© 2026 Tummly</span>

        <div className="flex flex-wrap items-center gap-x-[29px] gap-y-2">
          {footerLinks.map((item) => {
            if (item.href === HELP_CENTRE_URL) {
              return (
                <Link
                  key={item.label}
                  to={item.href}
                  className={footerLinkClass}
                  onMouseEnter={prefetchHelpCentreHero}
                  onFocus={prefetchHelpCentreHero}
                  onTouchStart={prefetchHelpCentreHero}
                >
                  {item.label}
                </Link>
              )
            }

            return (
              <Link
                key={item.label}
                to={item.href}
                className={footerLinkClass}
              >
                {item.label}
              </Link>
            )
          })}

          <CookieSettingsTrigger className={footerLinkClass}>
            Cookie settings
          </CookieSettingsTrigger>
        </div>
      </nav>
    </footer>
  )
}

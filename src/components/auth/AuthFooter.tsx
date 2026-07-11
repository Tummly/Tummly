import { Link } from "react-router-dom"

import { CookieSettingsTrigger } from "@/components/common/CookieSettingsDialog"
import { HELP_CENTRE_URL } from "@/config/support"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { prefetchHelpCentreHero } from "@/lib/prefetchHelpCentreHero"

const footerLinkClass =
  "rounded-sm text-[#555] no-underline transition-colors hover:text-[#232323] hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"

const footerItems = [
  { label: "© 2026 Tummly", type: "text" as const },
  { label: "Help Centre", type: "link" as const, href: HELP_CENTRE_URL },
  { label: "Terms", type: "link" as const, href: LEGAL_ROUTES.terms },
  { label: "Privacy", type: "link" as const, href: LEGAL_ROUTES.privacy },
  {
    label: "Cookie Policy",
    type: "link" as const,
    href: LEGAL_ROUTES.cookiePolicy,
  },
  { label: "Cookie settings", type: "cookie-settings" as const },
]

export function AuthFooter() {
  return (
    <footer className="relative z-10 w-full shrink-0 px-5 pb-6 pt-4 sm:px-6 lg:px-[clamp(1.5rem,12vw,13.125rem)] lg:pb-5">
      <nav
        aria-label="Auth footer"
        className="mx-auto flex w-full max-w-[490px] flex-wrap items-center justify-center gap-x-3 gap-y-2 text-sm font-medium text-[#555] sm:justify-between"
      >
        {footerItems.map((item) => {
          if (item.type === "text") {
            return <span key={item.label}>{item.label}</span>
          }

          if (item.type === "cookie-settings") {
            return (
              <CookieSettingsTrigger
                key={item.label}
                className={footerLinkClass}
              >
                {item.label}
              </CookieSettingsTrigger>
            )
          }

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

          if (item.href.startsWith("/")) {
            return (
              <Link key={item.label} to={item.href} className={footerLinkClass}>
                {item.label}
              </Link>
            )
          }

          return (
            <a key={item.label} href={item.href} className={footerLinkClass}>
              {item.label}
            </a>
          )
        })}
      </nav>
    </footer>
  )
}

import { Link } from "react-router-dom"

import { HELP_CENTRE_URL } from "@/config/support"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { prefetchHelpCentreHero } from "@/lib/prefetchHelpCentreHero"

const footerLinkClass =
  "rounded-sm text-[#555] no-underline transition-colors hover:text-[#232323] hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"

const footerItems = [
  { label: "© 2026 Tummly", href: undefined },
  { label: "Help Centre", href: HELP_CENTRE_URL },
  { label: "Terms", href: LEGAL_ROUTES.terms },
  { label: "Privacy", href: LEGAL_ROUTES.privacy },
  { label: "Cookie settings", href: LEGAL_ROUTES.cookieSettings },
] as const

export function AuthFooter() {
  return (
    <footer className="relative z-10 w-full shrink-0 px-5 pb-6 pt-4 sm:px-6 lg:px-[clamp(1.5rem,12vw,13.125rem)] lg:pb-5">
      <nav
        aria-label="Auth footer"
        className="mx-auto flex w-full max-w-[490px] flex-wrap items-center justify-between gap-x-3 gap-y-2 text-sm font-medium text-[#555]"
      >
        {footerItems.map((item) => {
          if (!item.href) {
            return <span key={item.label}>{item.label}</span>
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

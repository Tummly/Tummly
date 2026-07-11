import { Link } from "react-router-dom"

import { CookieSettingsTrigger } from "@/components/common/CookieSettingsDialog"
import { HELP_CENTRE_URL } from "@/config/support"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { prefetchHelpCentreHero } from "@/lib/prefetchHelpCentreHero"
import { cn } from "@/lib/utils"

const footerLinkClassName =
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

type GuestLoopLegalFooterProps = {
  className?: string
}

export function GuestLoopLegalFooter({ className }: GuestLoopLegalFooterProps) {
  return (
    <nav
      aria-label="Guest Loop footer"
      className={cn(
        "flex flex-wrap items-center justify-center gap-x-5 gap-y-2 text-xs font-medium text-[#555]",
        className
      )}
    >
      {footerItems.map((item) => {
        if (item.type === "text") {
          return <span key={item.label}>{item.label}</span>
        }

        if (item.type === "cookie-settings") {
          return (
            <CookieSettingsTrigger
              key={item.label}
              className={footerLinkClassName}
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
              className={footerLinkClassName}
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
            <Link
              key={item.label}
              to={item.href}
              className={footerLinkClassName}
            >
              {item.label}
            </Link>
          )
        }

        return (
          <a key={item.label} href={item.href} className={footerLinkClassName}>
            {item.label}
          </a>
        )
      })}
    </nav>
  )
}

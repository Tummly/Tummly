import { Link } from "react-router-dom"

import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { cn } from "@/lib/utils"

const footerLinkClassName =
  "rounded-sm text-[#555] no-underline transition-colors hover:text-[#232323] hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"

const footerItems = [
  { label: "© 2026 Tummly", href: undefined },
  { label: "Help Centre", href: "#" },
  { label: "Terms", href: LEGAL_ROUTES.terms },
  { label: "Privacy", href: LEGAL_ROUTES.privacy },
  { label: "Cookie settings", href: LEGAL_ROUTES.cookieSettings },
] as const

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
        if (!item.href) {
          return <span key={item.label}>{item.label}</span>
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

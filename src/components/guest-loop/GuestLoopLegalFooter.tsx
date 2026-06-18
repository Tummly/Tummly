import { cn } from "@/lib/utils"

const footerLinkClassName =
  "rounded-sm text-[#555] no-underline transition-colors hover:text-[#232323] hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"

const footerItems = [
  { label: "© 2026 Tummly", href: undefined },
  { label: "Help Centre", href: "#" },
  { label: "Terms", href: "#" },
  { label: "Privacy", href: "#" },
  { label: "Cookie settings", href: "#" },
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
      {footerItems.map((item) =>
        item.href ? (
          <a key={item.label} href={item.href} className={footerLinkClassName}>
            {item.label}
          </a>
        ) : (
          <span key={item.label}>{item.label}</span>
        )
      )}
    </nav>
  )
}

import { Link } from "react-router-dom"

import {
  HELP_CENTRE_CONTACT_URL,
  HELP_CENTRE_URL,
} from "@/config/support"
import { prefetchHelpCentreHero } from "@/lib/prefetchHelpCentreHero"
import { cn } from "@/lib/utils"

const linkClassName =
  "rounded-sm font-medium text-[#232323] underline underline-offset-2 transition-colors hover:text-[#141414] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"

type GuestLoopSupportFooterProps = {
  className?: string
}

export function GuestLoopSupportFooter({
  className,
}: GuestLoopSupportFooterProps) {
  return (
    <p
      className={cn(
        "m-0 text-center text-sm font-medium tracking-[0.4px] text-[#232323]",
        className
      )}
    >
      Need help?{" "}
      <Link to={HELP_CENTRE_CONTACT_URL} className={linkClassName}>
        Contact support
      </Link>{" "}
      or visit the{" "}
      <Link
        to={HELP_CENTRE_URL}
        className={linkClassName}
        onMouseEnter={prefetchHelpCentreHero}
        onFocus={prefetchHelpCentreHero}
        onTouchStart={prefetchHelpCentreHero}
      >
        Help Centre
      </Link>
      .
    </p>
  )
}

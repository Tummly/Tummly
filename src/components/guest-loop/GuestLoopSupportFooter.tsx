import { SUPPORT_EMAIL } from "@/config/support"
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
      <a href={`mailto:${SUPPORT_EMAIL}`} className={linkClassName}>
        Contact support
      </a>{" "}
      or visit the{" "}
      <a href="#" className={linkClassName}>
        Help Centre
      </a>
      .
    </p>
  )
}

import logo from "@/assets/images/guest-feedback/tummly-wordmark.png"
import type { CSSProperties } from "react"

import { cn } from "@/lib/utils"

type GuestFeedbackPoweredByProps = {
  className?: string
  style?: CSSProperties
  /**
   * `shell` — page footer.
   * `inline` — form / thank-you flow mid-page placement.
   */
  placement?: "shell" | "inline"
}

export function GuestFeedbackPoweredBy({
  className,
  style,
  placement = "shell",
}: GuestFeedbackPoweredByProps) {
  const isInline = placement === "inline"

  return (
    <footer
      className={cn(
        "relative z-10 flex shrink-0 items-center justify-center gap-[5.54px]",
        isInline ? "px-0 pt-0 pb-0" : "px-4 pt-6 pb-8",
        className
      )}
      style={style}
    >
      <span className="text-[10px] font-medium leading-normal text-guest-feedback-text">
        Powered by
      </span>
      <img src={logo} alt="Tummly" className="h-[19px] w-auto" />
    </footer>
  )
}

import type { ReactNode } from "react"

import { cn } from "@/lib/utils"

import { GuestFeedbackGlow } from "./GuestFeedbackGlow"
import { GuestFeedbackPoweredBy } from "./GuestFeedbackPoweredBy"

type GuestFeedbackShellGlow = "none" | "accent" | "neutral"

type GuestFeedbackShellProps = {
  children: ReactNode
  className?: string
  contentClassName?: string
  /** When false, screens own their own Powered by placement. */
  showPoweredBy?: boolean
  /**
   * Bottom bloom — Figma Rectangle 6 (accent thank-you / neutral unlock).
   * Painted on the shell root so it spans the full viewport width.
   */
  glow?: GuestFeedbackShellGlow
}

/**
 * Guest feedback page chrome — Figma Guest-Loop-MVP 6889:349.
 * Solid dark canvas only (no food pattern, no green bottom strip).
 */
export function GuestFeedbackShell({
  children,
  className,
  contentClassName,
  showPoweredBy = true,
  glow = "none",
}: GuestFeedbackShellProps) {
  return (
    <div
      className={cn(
        "relative flex min-h-dvh flex-col overflow-x-hidden bg-guest-feedback-bg text-guest-feedback-text",
        className
      )}
    >
      {glow !== "none" ? <GuestFeedbackGlow tone={glow} /> : null}

      <main
        className={cn(
          "relative z-1 mx-auto flex w-full min-h-0 flex-1 flex-col",
          "max-w-[min(100%,393px)] px-2.5 pt-2.5 pb-8",
          "sm:max-w-[min(100%,440px)] sm:px-3 sm:pt-3 sm:pb-10",
          "md:max-w-[min(100%,480px)] md:px-4 md:pt-5 md:pb-12",
          contentClassName
        )}
      >
        {children}
      </main>

      {showPoweredBy ? <GuestFeedbackPoweredBy /> : null}
    </div>
  )
}

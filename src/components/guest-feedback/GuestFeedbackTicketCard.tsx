import type { ReactNode } from "react"

import { cn } from "@/lib/utils"

type GuestFeedbackTicketCardProps = {
  children: ReactNode
  className?: string
}

/**
 * Notch-sided thank-you / unlock ticket — Figma Guest-Loop-MVP 6807:1186 / 6818:699.
 */
export function GuestFeedbackTicketCard({
  children,
  className,
}: GuestFeedbackTicketCardProps) {
  return (
    <div
      className={cn(
        "relative flex w-full flex-col items-center gap-[22px] overflow-visible rounded-[28px] bg-guest-feedback-surface px-5 py-[30px]",
        className
      )}
    >
      <span
        aria-hidden
        className="absolute top-1/2 left-[-11px] size-[18px] -translate-y-1/2 rounded-[20px] bg-guest-feedback-bg"
      />
      <span
        aria-hidden
        className="absolute top-1/2 right-[-11px] size-[18px] -translate-y-1/2 rounded-[20px] bg-guest-feedback-bg"
      />
      {children}
    </div>
  )
}

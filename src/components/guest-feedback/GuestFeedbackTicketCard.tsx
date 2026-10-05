import type { ReactNode } from "react"

import { GUEST_FEEDBACK_TICKET_CARD_CLASS } from "@/lib/guestFeedback/guestFeedbackLayoutPresentation"
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
    <div className={cn(GUEST_FEEDBACK_TICKET_CARD_CLASS, className)}>
      <span
        aria-hidden
        className="absolute top-1/2 left-[-11px] size-[18px] -translate-y-1/2 rounded-[20px] bg-guest-feedback-bg lg:left-[-13px] lg:size-5"
      />
      <span
        aria-hidden
        className="absolute top-1/2 right-[-11px] size-[18px] -translate-y-1/2 rounded-[20px] bg-guest-feedback-bg lg:right-[-13px] lg:size-5"
      />
      {children}
    </div>
  )
}

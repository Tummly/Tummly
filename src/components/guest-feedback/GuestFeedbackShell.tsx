import type { ReactNode } from "react"

import { cn } from "@/lib/utils"

import { GuestFeedbackAccent } from "./GuestFeedbackAccent"
import { GuestFeedbackBottomEdge } from "./GuestFeedbackBottomEdge"
import { GuestFeedbackPoweredBy } from "./GuestFeedbackPoweredBy"

type GuestFeedbackShellProps = {
  children: ReactNode
  className?: string
  contentClassName?: string
}

export function GuestFeedbackShell({
  children,
  className,
  contentClassName,
}: GuestFeedbackShellProps) {
  return (
    <div
      className={cn(
        "relative flex min-h-dvh flex-col overflow-x-hidden bg-guest-feedback-bg text-guest-feedback-text",
        className
      )}
    >
      <GuestFeedbackAccent />
      <GuestFeedbackBottomEdge />

      <main
        className={cn(
          "relative z-1 mx-auto flex w-full flex-1 flex-col px-[clamp(1.25rem,5vw,1.875rem)] pt-[clamp(4.5rem,14vw,5.125rem)]",
          "max-w-[min(100%,393px)] pb-6",
          "sm:max-w-[min(100%,480px)] sm:pb-10 sm:pt-[clamp(5rem,8vw,6rem)]",
          "md:max-w-[min(100%,560px)] md:pb-12",
          "lg:max-w-[min(100%,640px)]",
          contentClassName
        )}
      >
        {children}
      </main>

      <GuestFeedbackPoweredBy />
    </div>
  )
}

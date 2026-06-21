import logo from "@/assets/svg/logo.svg"

import { cn } from "@/lib/utils"

import { GUEST_FEEDBACK_BOTTOM_EDGE_HEIGHT } from "./GuestFeedbackBottomEdge"

type GuestFeedbackPoweredByProps = {
  className?: string
}

export function GuestFeedbackPoweredBy({
  className,
}: GuestFeedbackPoweredByProps) {
  return (
    <footer
      className={cn(
        "relative z-10 flex shrink-0 items-start justify-center gap-[5.54px] px-4 pt-6",
        className
      )}
      style={{
        paddingBottom: `calc(${GUEST_FEEDBACK_BOTTOM_EDGE_HEIGHT} + 1.25rem)`,
      }}
    >
      <span className="text-[10px] font-medium leading-normal text-guest-feedback-text">
        Powered by
      </span>
      <img
        src={logo}
        alt="Tummly"
        className="h-[19px] w-auto"
      />
    </footer>
  )
}

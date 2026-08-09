import { bottomStripPicture } from "@/assets/guest-feedback-images"

import { pictureToImageSet } from "@/lib/pictureBackground"
import { cn } from "@/lib/utils"

/** Matches the decorative strip height — keep in sync with shell footer spacing. */
export const GUEST_FEEDBACK_BOTTOM_EDGE_HEIGHT =
  "clamp(42px, 10vw, 56px)" as const

type GuestFeedbackBottomEdgeProps = {
  className?: string
  /**
   * `absolute` — Feedback form shell (pinned to viewport bottom).
   * `inline` — email Guest preview chrome (in document flow under Powered by).
   */
  placement?: "absolute" | "inline"
}

const bottomStripBackground = pictureToImageSet(bottomStripPicture)

const BOTTOM_EDGE_HEIGHT_CLASS =
  "h-[clamp(42px,10vw,56px)] sm:h-[clamp(50px,6vw,80px)] md:h-[clamp(56px,5vw,100px)] lg:h-[clamp(64px,4vw,120px)]"

export function GuestFeedbackBottomEdge({
  className,
  placement = "absolute",
}: GuestFeedbackBottomEdgeProps) {
  return (
    <div
      aria-hidden
      data-guest-feedback-bottom-edge={placement}
      className={cn(
        "pointer-events-none overflow-hidden",
        BOTTOM_EDGE_HEIGHT_CLASS,
        placement === "absolute"
          ? "absolute inset-x-0 bottom-0 z-0"
          : "relative w-full",
        className
      )}
    >
      <div
        className="absolute inset-x-0 rotate-180"
        style={{
          height: "200%",
          top: "-50%",
          backgroundImage: bottomStripBackground,
          backgroundRepeat: "repeat-x",
          backgroundSize: "110% 100%",
          backgroundPosition: "center center",
        }}
      />
    </div>
  )
}

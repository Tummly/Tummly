import { topDecorationPicture } from "@/assets/guest-feedback-images"

import { pictureToImageSet } from "@/lib/pictureBackground"
import { cn } from "@/lib/utils"

type GuestFeedbackAccentProps = {
  className?: string
}

const topDecorationBackground = pictureToImageSet(topDecorationPicture)

export function GuestFeedbackAccent({ className }: GuestFeedbackAccentProps) {
  return (
    <div
      aria-hidden
      className={cn(
        "pointer-events-none absolute inset-x-0 top-0 z-0 overflow-hidden",
        "h-[clamp(160px,32vw,232px)]",
        "sm:h-[clamp(200px,22vw,320px)]",
        "md:h-[clamp(240px,18vw,400px)]",
        "lg:h-[clamp(280px,15vw,480px)]",
        className
      )}
    >
      <div
        className="absolute inset-0"
        style={{
          backgroundImage: topDecorationBackground,
          backgroundRepeat: "repeat-x",
          backgroundSize: "auto 100%",
          backgroundPosition: "left top",
        }}
      />
      <div
        className="absolute inset-0"
        style={{
          backgroundImage:
            "linear-gradient(260deg, rgb(20, 20, 20) 2.32%, rgba(20, 20, 20, 0) 16.9%), linear-gradient(3deg, rgb(20, 20, 20) 13.4%, rgba(20, 20, 20, 0) 72.9%)",
        }}
      />
    </div>
  )
}

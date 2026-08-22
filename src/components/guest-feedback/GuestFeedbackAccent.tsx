import { topDecorationPicture } from "@/assets/guest-feedback-images"

import { pictureToImageSet } from "@/lib/pictureBackground"
import { cn } from "@/lib/utils"

type GuestFeedbackAccentProps = {
  className?: string
  /**
   * `phone` — early top→bottom fade for the Home hero phone preview only.
   * Default keeps the full Figma guest-form accent.
   */
  patternFade?: "none" | "phone"
}

const topDecorationBackground = pictureToImageSet(topDecorationPicture)

/** Top fully visible briefly, then fades out early toward the bottom. */
const PHONE_PATTERN_MASK =
  "linear-gradient(to bottom, #000 0%, #000 4%, transparent 45%)"

export function GuestFeedbackAccent({
  className,
  patternFade = "none",
}: GuestFeedbackAccentProps) {
  const usePhoneFade = patternFade === "phone"

  return (
    <div
      aria-hidden
      className={cn(
        "pointer-events-none absolute inset-x-0 top-0 z-0 overflow-hidden",
        "h-[clamp(200px,40vw,290px)]",
        "sm:h-[clamp(250px,28vw,400px)]",
        "md:h-[clamp(300px,22vw,500px)]",
        "lg:h-[clamp(350px,18vw,600px)]",
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
          ...(usePhoneFade
            ? {
                WebkitMaskImage: PHONE_PATTERN_MASK,
                maskImage: PHONE_PATTERN_MASK,
              }
            : null),
        }}
      />
      <div
        className="absolute inset-0"
        style={{
          backgroundImage: usePhoneFade
            ? "linear-gradient(260deg, var(--guest-feedback-bg) 2.32%, transparent 16.9%)"
            : "linear-gradient(260deg, rgb(20, 20, 20) 2.32%, rgba(20, 20, 20, 0) 16.9%), linear-gradient(3deg, rgb(20, 20, 20) 13.4%, rgba(20, 20, 20, 0) 72.9%)",
        }}
      />
    </div>
  )
}

import { cn } from "@/lib/utils"

/**
 * Dictation glow — Figma Guest-Loop-MVP 6890:402.
 * Soft emerald bloom from below the card; visible wash reaches ~half the input.
 */
export function GuestFeedbackDictationGlow({
  className,
}: {
  className?: string
}) {
  return (
    <div
      aria-hidden
      data-guest-feedback-dictation-glow=""
      className={cn(
        "pointer-events-none absolute inset-x-0 bottom-0 z-0 h-[60%] w-full",
        className
      )}
      style={{
        backgroundImage:
          "radial-gradient(ellipse 160% 115% at 50% 118%, rgba(20, 162, 71, 0.42) 0%, rgba(20, 162, 71, 0.28) 28%, rgba(19, 125, 57, 0.16) 52%, rgba(18, 89, 43, 0.08) 72%, rgba(15, 15, 15, 0) 100%)",
      }}
    />
  )
}

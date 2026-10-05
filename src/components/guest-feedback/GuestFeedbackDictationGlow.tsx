import { cn } from "@/lib/utils"

/**
 * Dictation glow — Figma Guest-Loop-MVP 6890:402 (static / reduced-motion).
 * Soft emerald bloom from the bottom edge; fades out before mid-card.
 * Layer fills the composer so there is no hard horizontal band.
 */
export const GUEST_FEEDBACK_DICTATION_GLOW_GRADIENT =
  "radial-gradient(ellipse 130% 90% at 50% 100%, rgba(20, 162, 71, 0.42) 0%, rgba(20, 162, 71, 0.24) 22%, rgba(19, 125, 57, 0.12) 42%, rgba(18, 89, 43, 0.05) 58%, rgba(15, 15, 15, 0) 72%)"

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
        "pointer-events-none absolute inset-0 z-0",
        className
      )}
      style={{
        backgroundImage: GUEST_FEEDBACK_DICTATION_GLOW_GRADIENT,
      }}
    />
  )
}

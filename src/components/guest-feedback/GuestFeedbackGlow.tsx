import { motion, useReducedMotion } from "framer-motion"

import {
  guestFeedbackMotionDurations,
  guestFeedbackMotionEaseInOut,
} from "@/lib/guestFeedback/guestFeedbackMotionTokens"
import { cn } from "@/lib/utils"

type GuestFeedbackGlowTone = "accent" | "neutral"

type GuestFeedbackGlowProps = {
  tone?: GuestFeedbackGlowTone
  className?: string
}

/** Thank-you bloom — Figma Rectangle 6 (6829:13688). Soft fade, no hard band. */
const ACCENT_GLOW =
  "radial-gradient(ellipse 120% 100% at 50% 100%, rgba(20, 162, 71, 0.34) 0%, rgba(19, 125, 57, 0.2) 28%, rgba(18, 89, 43, 0.1) 48%, rgba(16, 52, 29, 0.04) 65%, rgba(15, 15, 15, 0) 78%)"

const NEUTRAL_GLOW =
  "radial-gradient(ellipse 120% 100% at 50% 100%, rgba(154, 154, 154, 0.24) 0%, rgba(119, 119, 119, 0.14) 28%, rgba(85, 85, 85, 0.08) 48%, rgba(50, 50, 50, 0.03) 65%, rgba(15, 15, 15, 0) 78%)"

/**
 * Bottom radial glow — Figma Rectangle 6 (6829:13688).
 * Must sit on the shell root (full viewport width), not the narrow content column.
 * Opacity-only ambient pulse — no translate / scale (avoids a climbing band).
 */
export function GuestFeedbackGlow({
  tone = "accent",
  className,
}: GuestFeedbackGlowProps) {
  const shouldReduceMotion = useReducedMotion()
  const backgroundImage = tone === "accent" ? ACCENT_GLOW : NEUTRAL_GLOW

  if (shouldReduceMotion) {
    return (
      <div
        aria-hidden
        data-guest-feedback-glow={tone}
        className={cn(
          "pointer-events-none absolute inset-x-0 bottom-0 z-0 h-[min(45%,360px)] w-full",
          className
        )}
        style={{ backgroundImage }}
      />
    )
  }

  return (
    <motion.div
      aria-hidden
      data-guest-feedback-glow={tone}
      className={cn(
        "pointer-events-none absolute inset-x-0 bottom-0 z-0 h-[min(45%,360px)] w-full",
        className
      )}
      style={{ backgroundImage }}
      animate={{ opacity: [0.82, 1, 0.82] }}
      transition={{
        duration: guestFeedbackMotionDurations.offerAmbientLoop,
        ease: guestFeedbackMotionEaseInOut,
        repeat: Infinity,
      }}
    />
  )
}

import { motion, useReducedMotion } from "framer-motion"

import {
  GUEST_FEEDBACK_DICTATION_GLOW_GRADIENT,
  GuestFeedbackDictationGlow,
} from "@/components/guest-feedback/GuestFeedbackDictationGlow"
import {
  guestFeedbackMotionDurations,
  guestFeedbackMotionEaseInOut,
} from "@/lib/guestFeedback/guestFeedbackMotionTokens"
import { cn } from "@/lib/utils"

/**
 * Listening-state glow — soft emerald wash pinned to the bottom of the glass
 * composer. Only opacity breathes; the layer never translates or scales
 * (those create a hard band that climbs the card).
 */
export function GuestFeedbackDictationGlowMotion({
  className,
}: {
  className?: string
}) {
  const shouldReduceMotion = useReducedMotion()

  if (shouldReduceMotion) {
    return <GuestFeedbackDictationGlow className={className} />
  }

  return (
    <motion.div
      aria-hidden
      data-guest-feedback-dictation-glow=""
      className={cn("pointer-events-none absolute inset-0 z-0", className)}
      style={{
        backgroundImage: GUEST_FEEDBACK_DICTATION_GLOW_GRADIENT,
      }}
      animate={{ opacity: [0.72, 1, 0.72] }}
      transition={{
        duration: guestFeedbackMotionDurations.listeningGradientLoop,
        ease: guestFeedbackMotionEaseInOut,
        repeat: Infinity,
      }}
    />
  )
}

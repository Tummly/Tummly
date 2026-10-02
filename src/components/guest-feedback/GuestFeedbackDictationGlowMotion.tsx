import { motion, useReducedMotion } from "framer-motion"

import { GuestFeedbackDictationGlow } from "@/components/guest-feedback/GuestFeedbackDictationGlow"
import {
  guestFeedbackMotionDurations,
  guestFeedbackMotionEaseInOut,
} from "@/lib/guestFeedback/guestFeedbackMotionTokens"
import { cn } from "@/lib/utils"

const GRADIENT_BOTTOM =
  "radial-gradient(ellipse 160% 115% at 50% 125%, rgba(20, 162, 71, 0.42) 0%, rgba(20, 162, 71, 0.28) 28%, rgba(19, 125, 57, 0.16) 52%, rgba(18, 89, 43, 0.08) 72%, rgba(15, 15, 15, 0) 100%)"

const GRADIENT_DRIFT_UP =
  "radial-gradient(ellipse 170% 135% at 50% 108%, rgba(20, 162, 71, 0.4) 0%, rgba(20, 162, 71, 0.26) 30%, rgba(19, 125, 57, 0.14) 54%, rgba(18, 89, 43, 0.07) 74%, rgba(15, 15, 15, 0) 100%)"

/**
 * Listening-state glow + border motion per Guest Feedback motion spec.
 * Continuous loops run only while mic phase is recording (parent gates mount).
 * Gradient stays pinned to the bottom; wash drifts upward inside the layer.
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
    <>
      <motion.div
        aria-hidden
        data-guest-feedback-dictation-glow=""
        className={cn(
          "pointer-events-none absolute inset-x-0 bottom-0 z-0 h-[60%] w-full origin-bottom",
          className
        )}
        animate={{
          backgroundImage: [GRADIENT_BOTTOM, GRADIENT_DRIFT_UP, GRADIENT_BOTTOM],
        }}
        transition={{
          duration: guestFeedbackMotionDurations.listeningGradientLoop,
          repeat: Infinity,
          ease: guestFeedbackMotionEaseInOut,
        }}
        style={{
          backgroundImage: GRADIENT_BOTTOM,
        }}
      />
      <motion.div
        aria-hidden
        className="pointer-events-none absolute inset-0 z-0 rounded-[28px]"
        animate={{
          boxShadow: [
            "inset 0 0 0 1px rgba(20, 162, 71, 0.25)",
            "inset 0 0 0 1px rgba(20, 162, 71, 0.55)",
            "inset 0 0 0 1px rgba(20, 162, 71, 0.25)",
          ],
        }}
        transition={{
          duration: guestFeedbackMotionDurations.listeningBorderLoop,
          repeat: Infinity,
          ease: guestFeedbackMotionEaseInOut,
        }}
      />
      <motion.div
        aria-hidden
        className="pointer-events-none absolute -inset-1 z-0 rounded-[30px]"
        animate={{
          opacity: [0.35, 0.65, 0.35],
        }}
        transition={{
          duration: guestFeedbackMotionDurations.listeningGlowLoop,
          repeat: Infinity,
          ease: guestFeedbackMotionEaseInOut,
        }}
        style={{
          boxShadow: "0 0 28px rgba(20, 162, 71, 0.35)",
        }}
      />
    </>
  )
}

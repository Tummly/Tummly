import type { Transition } from "framer-motion"

/** Motion spec timings — Guest Feedback Form (Oct 2026 handoff). */

export const guestFeedbackMotionEaseOut = [0.22, 1, 0.36, 1] as const
export const guestFeedbackMotionEaseIn = [0.4, 0, 1, 1] as const
export const guestFeedbackMotionEaseInOut = [0.45, 0, 0.55, 1] as const

export const guestFeedbackMotionDurations = {
  press: 0.09,
  pressRelease: 0.16,
  fieldFocus: 0.2,
  smallUi: 0.18,
  component: 0.3,
  majorState: 0.4,
  thankYouHeadline: 0.3,
  offerCard: 0.38,
  offerCode: 0.22,
  copiedHoldMs: 1500,
  /** Dictation wash — soft in-place bloom pulse. */
  listeningGradientLoop: 3.2,
  /** Thank-you / unlock bottom bloom ambient pulse. */
  offerAmbientLoop: 6.5,
} as const

/** Entrances / appearing — Ease Out, no bounce. */
export const guestFeedbackEnterTransition: Transition = {
  duration: guestFeedbackMotionDurations.component,
  ease: guestFeedbackMotionEaseOut,
}

/** Major form state (submit → thank-you). */
export const guestFeedbackMajorStateTransition: Transition = {
  duration: guestFeedbackMotionDurations.majorState,
  ease: guestFeedbackMotionEaseInOut,
}

export const guestFeedbackPressTransition: Transition = {
  duration: guestFeedbackMotionDurations.press,
  ease: guestFeedbackMotionEaseOut,
}

export const guestFeedbackHeadlineTransition: Transition = {
  duration: guestFeedbackMotionDurations.thankYouHeadline,
  ease: guestFeedbackMotionEaseOut,
}

export const guestFeedbackOfferCardTransition: Transition = {
  duration: guestFeedbackMotionDurations.offerCard,
  ease: guestFeedbackMotionEaseOut,
}

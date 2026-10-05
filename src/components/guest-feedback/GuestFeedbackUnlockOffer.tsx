import {
  motion,
  useReducedMotion,
  type Variants,
} from "framer-motion"

import { GuestFeedbackPoweredBy } from "@/components/guest-feedback/GuestFeedbackPoweredBy"
import { GuestFeedbackTicketCard } from "@/components/guest-feedback/GuestFeedbackTicketCard"
import { Button } from "@/components/ui/button"
import {
  buildGuestFeedbackUnlockCopy,
  type GuestFeedbackUnlockChannel,
} from "@/lib/guestFeedback/guestFeedbackUnlockPresentation"
import {
  GUEST_FEEDBACK_PRIMARY_BUTTON_CLASS,
  GUEST_FEEDBACK_THANK_YOU_BODY_CLASS,
  GUEST_FEEDBACK_THANK_YOU_TITLE_CLASS,
} from "@/lib/guestFeedback/guestFeedbackLayoutPresentation"
import {
  guestFeedbackEnterTransition,
  guestFeedbackHeadlineTransition,
} from "@/lib/guestFeedback/guestFeedbackMotionTokens"
import { cn } from "@/lib/utils"

const containerVariants: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.08,
      delayChildren: 0.05,
    },
  },
}

const itemVariants: Variants = {
  hidden: { opacity: 0, y: 12 },
  visible: {
    opacity: 1,
    y: 0,
    transition: guestFeedbackEnterTransition,
  },
}

const headlineVariants: Variants = {
  hidden: { opacity: 0, y: 10 },
  visible: {
    opacity: 1,
    y: 0,
    transition: guestFeedbackHeadlineTransition,
  },
}

export type GuestFeedbackUnlockOfferProps = {
  restaurantName: string
  locationName: string
  brandLogoPublicUrl?: string | null
  offerTitle: string
  channel: GuestFeedbackUnlockChannel
  isUnlocking: boolean
  unlockError: string | null
  onUnlock: () => void
  onDecline: () => void
  className?: string
}

/** Need-consent unlock screen — Figma Guest-Loop-MVP 6818:699. */
export function GuestFeedbackUnlockOffer({
  restaurantName,
  locationName,
  offerTitle,
  channel,
  isUnlocking,
  unlockError,
  onUnlock,
  onDecline,
  className,
}: GuestFeedbackUnlockOfferProps) {
  const shouldReduceMotion = useReducedMotion()
  const displayLocation = locationName.trim() || "this location"
  const displayRestaurant = restaurantName.trim() || "this restaurant"
  const copy = buildGuestFeedbackUnlockCopy({
    restaurantName: displayRestaurant,
    locationName: displayLocation,
    offerTitle,
    channel,
  })

  return (
    <div
      className={cn(
        "relative mx-auto flex min-h-full w-full flex-1 flex-col",
        className
      )}
    >
      <motion.div
        variants={shouldReduceMotion ? undefined : containerVariants}
        initial={shouldReduceMotion ? false : "hidden"}
        animate="visible"
        className="relative z-1 flex min-h-full w-full flex-1 flex-col"
      >
        <div className="flex w-full flex-1 flex-col items-center justify-center gap-8 px-0 pt-6 pb-4 lg:gap-10 lg:pt-8">
          <div className="flex w-full flex-col items-center gap-3 text-center lg:gap-4">
            <motion.h1
              variants={shouldReduceMotion ? undefined : headlineVariants}
              className={GUEST_FEEDBACK_THANK_YOU_TITLE_CLASS}
            >
              {copy.thankYouHeading}
            </motion.h1>
            <motion.p
              variants={shouldReduceMotion ? undefined : itemVariants}
              className={GUEST_FEEDBACK_THANK_YOU_BODY_CLASS}
            >
              {copy.sharedBody}
            </motion.p>
          </div>

          <motion.div
            variants={shouldReduceMotion ? undefined : itemVariants}
            className="w-full"
          >
            <GuestFeedbackTicketCard className="gap-[33px] lg:gap-8">
              <div className="flex w-full flex-col items-center gap-3 text-center">
                <div className="flex flex-col items-center gap-2 text-guest-feedback-text">
                  <p className="m-0 text-xs font-medium leading-normal lg:text-sm">
                    Your thank-you offer
                  </p>
                  <p className="m-0 max-w-[255px] font-heading text-[clamp(1.5rem,2.6vw,1.875rem)] font-bold leading-normal lg:max-w-sm">
                    {copy.wantHeading}
                  </p>
                </div>
                <p className="m-0 text-sm font-medium leading-[18px] text-guest-feedback-text/40 lg:max-w-md lg:text-base lg:leading-6">
                  {copy.joinBody}
                </p>
              </div>
            </GuestFeedbackTicketCard>
          </motion.div>

          <motion.div
            variants={shouldReduceMotion ? undefined : itemVariants}
            className="flex w-full flex-col items-center gap-3"
          >
            {unlockError ? (
              <p
                role="alert"
                className="w-full text-center text-sm text-destructive"
              >
                {unlockError}
              </p>
            ) : null}

            <Button
              type="button"
              disabled={isUnlocking}
              onClick={onUnlock}
              className={cn(
                GUEST_FEEDBACK_PRIMARY_BUTTON_CLASS,
                "bg-guest-feedback-accent text-guest-feedback-text hover:bg-[#129641] disabled:opacity-70"
              )}
            >
              {isUnlocking ? "Unlocking…" : copy.unlockCta}
            </Button>
            <Button
              type="button"
              disabled={isUnlocking}
              onClick={onDecline}
              className={cn(
                GUEST_FEEDBACK_PRIMARY_BUTTON_CLASS,
                "bg-guest-feedback-secondary text-guest-feedback-secondary-fg hover:bg-guest-feedback-secondary"
              )}
            >
              {copy.declineCta}
            </Button>
          </motion.div>
        </div>

        <motion.div
          variants={shouldReduceMotion ? undefined : itemVariants}
          className="relative z-1 flex w-full justify-center pb-8 pt-2"
        >
          <GuestFeedbackPoweredBy placement="inline" />
        </motion.div>
      </motion.div>
    </div>
  )
}

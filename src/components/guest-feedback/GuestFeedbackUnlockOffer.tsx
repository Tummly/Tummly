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
        <div className="flex w-full flex-1 flex-col items-center justify-center gap-8 px-0 pt-6 pb-4">
          <div className="flex w-full max-w-[min(100%,400px)] flex-col items-center gap-3 text-center md:max-w-[min(100%,440px)]">
            <motion.h1
              variants={shouldReduceMotion ? undefined : headlineVariants}
              className="m-0 font-heading text-[28px] font-bold leading-normal text-guest-feedback-text"
            >
              {copy.thankYouHeading}
            </motion.h1>
            <motion.p
              variants={shouldReduceMotion ? undefined : itemVariants}
              className="m-0 max-w-[291px] text-sm font-medium leading-5 text-[#888]"
            >
              {copy.sharedBody}
            </motion.p>
          </div>

          <motion.div
            variants={shouldReduceMotion ? undefined : itemVariants}
            className="w-full max-w-[min(100%,400px)] md:max-w-[min(100%,440px)]"
          >
            <GuestFeedbackTicketCard className="gap-[33px] py-[30px]">
              <div className="flex w-full flex-col items-center gap-3 text-center">
                <div className="flex flex-col items-center gap-2 text-guest-feedback-text">
                  <p className="m-0 text-xs font-medium leading-normal">
                    Your thank-you offer
                  </p>
                  <p className="m-0 max-w-[255px] font-heading text-2xl font-bold leading-normal">
                    {copy.wantHeading}
                  </p>
                </div>
                <p className="m-0 text-sm font-medium leading-[18px] text-guest-feedback-text/40">
                  {copy.joinBody}
                </p>
              </div>
            </GuestFeedbackTicketCard>
          </motion.div>

          <motion.div
            variants={shouldReduceMotion ? undefined : itemVariants}
            className="flex w-full max-w-[min(100%,400px)] flex-col items-center gap-3 md:max-w-[min(100%,440px)]"
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
              className="h-auto min-h-12.5 w-full rounded-[54px] bg-guest-feedback-accent px-4 py-4 text-sm font-medium leading-normal text-guest-feedback-text shadow-none hover:bg-[#129641] disabled:opacity-70"
            >
              {isUnlocking ? "Unlocking…" : copy.unlockCta}
            </Button>
            <Button
              type="button"
              disabled={isUnlocking}
              onClick={onDecline}
              className="h-auto min-h-12.5 w-full rounded-[54px] bg-guest-feedback-secondary px-4 py-4 text-sm font-medium leading-normal text-guest-feedback-secondary-fg shadow-none hover:bg-guest-feedback-secondary"
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

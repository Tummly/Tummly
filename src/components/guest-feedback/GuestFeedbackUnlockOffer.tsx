import {
  motion,
  useReducedMotion,
  type Transition,
  type Variants,
} from "framer-motion"

import { BrandLogoMark } from "@/components/brand/BrandLogoMark"
import { Button } from "@/components/ui/button"
import {
  buildGuestFeedbackUnlockCopy,
  type GuestFeedbackUnlockChannel,
} from "@/lib/guestFeedback/guestFeedbackUnlockPresentation"
import { cn } from "@/lib/utils"

const cardSpring: Transition = {
  type: "spring",
  stiffness: 380,
  damping: 32,
  mass: 0.9,
}

const fadeTransition: Transition = {
  duration: 0.24,
  ease: [0.25, 0.1, 0.25, 1],
}

const containerVariants: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.08,
      delayChildren: 0.06,
    },
  },
}

const itemVariants: Variants = {
  hidden: { opacity: 0, y: 14 },
  visible: {
    opacity: 1,
    y: 0,
    transition: cardSpring,
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

/** Post-submit unlock screen — Figma Guest-Loop-MVP 6723:1094. */
export function GuestFeedbackUnlockOffer({
  restaurantName,
  locationName,
  brandLogoPublicUrl = null,
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
    <div className={cn("mx-auto flex w-full max-w-[min(100%,400px)] flex-col gap-6", className)}>
      <motion.div
        initial={shouldReduceMotion ? false : { opacity: 0, scale: 0.96, y: 12 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        transition={shouldReduceMotion ? { duration: 0 } : cardSpring}
        className="relative flex w-full flex-col items-center rounded-[10px] border border-guest-feedback-border bg-guest-feedback-surface px-5 pb-7.5 pt-0 text-center sm:px-8 sm:pb-10"
      >
        <span
          aria-hidden
          className="absolute -left-3 top-1/2 size-4.5 -translate-y-1/2 rounded-[20px] bg-guest-feedback-bg"
        />
        <span
          aria-hidden
          className="absolute -right-3 top-1/2 size-4.5 -translate-y-1/2 rounded-[20px] bg-guest-feedback-bg"
        />

        <motion.div
          initial={shouldReduceMotion ? false : { opacity: 0, scale: 0.7, y: 8 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          transition={
            shouldReduceMotion ? { duration: 0 } : { ...cardSpring, delay: 0.08 }
          }
          className="-mt-7 flex flex-col items-center gap-3"
        >
          <BrandLogoMark
            brandLogoPublicUrl={brandLogoPublicUrl}
            className="size-13"
            roundedClassName="rounded-md"
          />
          <span className="flex flex-col items-center gap-1">
            <span className="text-[22px] font-semibold leading-normal text-guest-feedback-text">
              {displayRestaurant}
            </span>
            <span className="text-xs leading-normal text-guest-feedback-muted">
              {displayLocation}
            </span>
          </span>
        </motion.div>

        <motion.div
          variants={shouldReduceMotion ? undefined : containerVariants}
          initial={shouldReduceMotion ? false : "hidden"}
          animate="visible"
          className="mt-15 flex w-full flex-col items-center gap-5"
        >
          <div className="flex flex-col items-center gap-3">
            <motion.h1
              variants={shouldReduceMotion ? undefined : itemVariants}
              className="text-[22px] font-medium leading-normal text-guest-feedback-text"
            >
              {copy.thankYouHeading}
            </motion.h1>
            <motion.p
              variants={shouldReduceMotion ? undefined : itemVariants}
              transition={shouldReduceMotion ? undefined : fadeTransition}
              className="max-w-70 text-xs leading-4.5 text-guest-feedback-muted"
            >
              {copy.sharedBody}
            </motion.p>
          </div>

          <motion.div
            variants={shouldReduceMotion ? undefined : itemVariants}
            className="w-full rounded-[8px] bg-guest-feedback-bg px-5 py-5 text-left"
          >
            <p className="m-0 text-base font-medium leading-normal text-guest-feedback-text">
              {copy.wantHeading}
            </p>
            <p className="m-0 mt-2 text-xs leading-4.5 text-guest-feedback-muted">
              {copy.joinBody}
            </p>
          </motion.div>
        </motion.div>
      </motion.div>

      <motion.div
        initial={shouldReduceMotion ? false : { opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={shouldReduceMotion ? { duration: 0 } : { ...cardSpring, delay: 0.12 }}
        className="flex flex-col items-center gap-3"
      >
        {unlockError ? (
          <p role="alert" className="w-full text-center text-sm text-destructive">
            {unlockError}
          </p>
        ) : null}

        <Button
          type="button"
          disabled={isUnlocking}
          onClick={onUnlock}
          className="h-auto min-h-12.5 w-full rounded-[54px] bg-guest-feedback-accent px-4.25 py-3.25 text-sm leading-normal text-white shadow-none hover:bg-[#129641] disabled:opacity-70"
        >
          {isUnlocking ? "Unlocking…" : copy.unlockCta}
        </Button>
        <Button
          type="button"
          disabled={isUnlocking}
          onClick={onDecline}
          className="h-auto min-h-12.5 w-full rounded-[54px] bg-[#2a2a2a] px-4.25 py-3.25 text-sm leading-normal text-guest-feedback-muted shadow-none hover:bg-[#2a2a2a]"
        >
          {copy.declineCta}
        </Button>
        <p className="m-0 text-xs leading-normal text-guest-feedback-muted">
          {copy.optOutNote}
        </p>
      </motion.div>
    </div>
  )
}

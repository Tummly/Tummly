import {
  motion,
  useReducedMotion,
  type Transition,
  type Variants,
} from "framer-motion"

import { BrandLogoMark } from "@/components/brand/BrandLogoMark"
import { GuestPreviewOfferCoupon } from "@/components/dashboard/operator/Feedback/GuestPreviewOfferCoupon"
import {
  buildGuestFeedbackSharedPrivatelyBody,
  displayGuestFeedbackLocation,
  displayGuestFeedbackRestaurant,
} from "@/lib/guestFeedback/guestFeedbackUnlockPresentation"
import type { GuestPreviewOfferCouponView } from "@/lib/operatorFeedback/guestPreviewPresentation"
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

type GuestFeedbackSuccessProps = {
  restaurantName: string
  locationName: string
  brandLogoPublicUrl?: string | null
  className?: string
  /** Issued or preview-sample offer coupon painted under thank-you copy. */
  offer?: GuestPreviewOfferCouponView | null
}

/** Thank-you ticket — Figma Guest-Loop-MVP 6778:28257. */
export function GuestFeedbackSuccess({
  restaurantName,
  locationName,
  brandLogoPublicUrl = null,
  className,
  offer = null,
}: GuestFeedbackSuccessProps) {
  const shouldReduceMotion = useReducedMotion()
  const displayRestaurant = displayGuestFeedbackRestaurant(restaurantName)
  const displayLocation = displayGuestFeedbackLocation(locationName)

  return (
    <motion.div
      initial={shouldReduceMotion ? false : { opacity: 0, scale: 0.96, y: 12 }}
      animate={{ opacity: 1, scale: 1, y: 0 }}
      transition={shouldReduceMotion ? { duration: 0 } : cardSpring}
      className={cn(
        "relative mx-auto flex w-full flex-col items-center rounded-[10px] border border-guest-feedback-border bg-guest-feedback-surface px-5 pb-[30px] pt-0 text-center",
        offer != null
          ? "max-w-[min(100%,400px)] sm:max-w-[min(100%,440px)] sm:px-8"
          : "max-w-[min(100%,333px)] sm:max-w-[min(100%,400px)] sm:px-8 md:max-w-[min(100%,440px)]",
        className
      )}
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
        className="-mt-7 flex flex-col items-center gap-[22px]"
      >
        <BrandLogoMark
          brandLogoPublicUrl={brandLogoPublicUrl}
          className="size-13"
          roundedClassName="rounded-[2px]"
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
        className="mt-15 flex w-full flex-col items-center gap-[33px]"
      >
        <div className="flex flex-col items-center gap-3">
          <motion.h1
            variants={shouldReduceMotion ? undefined : itemVariants}
            className="text-[22px] font-medium leading-normal text-guest-feedback-text"
          >
            Thank you.
          </motion.h1>
          <motion.p
            variants={shouldReduceMotion ? undefined : itemVariants}
            transition={shouldReduceMotion ? undefined : fadeTransition}
            className="max-w-70 text-xs leading-4.5 text-guest-feedback-muted"
          >
            {buildGuestFeedbackSharedPrivatelyBody(
              restaurantName,
              locationName
            )}
          </motion.p>
        </div>
        {offer != null ? (
          <motion.div
            variants={shouldReduceMotion ? undefined : itemVariants}
            className="w-full"
          >
            <GuestPreviewOfferCoupon coupon={offer} surface="thankYou" />
          </motion.div>
        ) : null}
      </motion.div>
    </motion.div>
  )
}

import {
  motion,
  useReducedMotion,
  type Variants,
} from "framer-motion"

import { GuestFeedbackPoweredBy } from "@/components/guest-feedback/GuestFeedbackPoweredBy"
import { GuestFeedbackTicketCard } from "@/components/guest-feedback/GuestFeedbackTicketCard"
import { GuestPreviewOfferCoupon } from "@/components/dashboard/operator/Feedback/GuestPreviewOfferCoupon"
import {
  buildGuestFeedbackSharedPrivatelyBody,
} from "@/lib/guestFeedback/guestFeedbackUnlockPresentation"
import {
  GUEST_FEEDBACK_THANK_YOU_BODY_CLASS,
  GUEST_FEEDBACK_THANK_YOU_TITLE_CLASS,
} from "@/lib/guestFeedback/guestFeedbackLayoutPresentation"
import {
  guestFeedbackHeadlineTransition,
  guestFeedbackOfferCardTransition,
} from "@/lib/guestFeedback/guestFeedbackMotionTokens"
import type { GuestPreviewOfferCouponView } from "@/lib/operatorFeedback/guestPreviewPresentation"
import { cn } from "@/lib/utils"

const containerVariants: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.05,
    },
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

const bodyVariants: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: {
    opacity: 1,
    y: 0,
    transition: guestFeedbackHeadlineTransition,
  },
}

const offerCardVariants: Variants = {
  hidden: { opacity: 0, y: 12, scale: 0.98 },
  visible: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: guestFeedbackOfferCardTransition,
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

/** Thank-you screen — Figma Guest-Loop-MVP 6807:1186. */
export function GuestFeedbackSuccess({
  restaurantName,
  locationName,
  brandLogoPublicUrl = null,
  className,
  offer = null,
}: GuestFeedbackSuccessProps) {
  const shouldReduceMotion = useReducedMotion()

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
              Thank you.
            </motion.h1>
            <motion.p
              variants={shouldReduceMotion ? undefined : bodyVariants}
              className={GUEST_FEEDBACK_THANK_YOU_BODY_CLASS}
            >
              {buildGuestFeedbackSharedPrivatelyBody(
                restaurantName,
                locationName
              )}
            </motion.p>
          </div>

          {offer != null ? (
            <motion.div
              variants={shouldReduceMotion ? undefined : offerCardVariants}
              className="w-full"
            >
              <GuestFeedbackTicketCard>
                <GuestPreviewOfferCoupon
                  coupon={offer}
                  surface="thankYou"
                  brandLogoPublicUrl={brandLogoPublicUrl}
                />
              </GuestFeedbackTicketCard>
            </motion.div>
          ) : null}
        </div>

        <motion.div
          variants={shouldReduceMotion ? undefined : bodyVariants}
          className="relative z-1 flex w-full justify-center pb-8 pt-2"
        >
          <GuestFeedbackPoweredBy placement="inline" />
        </motion.div>
      </motion.div>
    </div>
  )
}

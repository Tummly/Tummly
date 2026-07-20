import {
  motion,
  useReducedMotion,
  type Transition,
  type Variants,
} from "framer-motion"

import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
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
  locationName: string
  address: string
  className?: string
}

export function GuestFeedbackSuccess({
  locationName,
  address,
  className,
}: GuestFeedbackSuccessProps) {
  const shouldReduceMotion = useReducedMotion()
  const displayLocation = locationName.trim() || "this location"
  const displayAddress = address.trim()

  return (
    <motion.div
      initial={shouldReduceMotion ? false : { opacity: 0, scale: 0.96, y: 12 }}
      animate={{ opacity: 1, scale: 1, y: 0 }}
      transition={shouldReduceMotion ? { duration: 0 } : cardSpring}
      className={cn(
        "relative mx-auto flex w-full flex-col items-center rounded-[10px] border border-guest-feedback-border bg-guest-feedback-surface px-5 pb-7.5 text-center",
        "max-w-[min(100%,333px)]",
        "sm:max-w-[min(100%,400px)] sm:px-8 sm:pb-10",
        "md:max-w-[min(100%,440px)]",
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
        className="-mt-7 flex flex-col items-center gap-3"
      >
        <span
          className="size-13 shrink-0 overflow-hidden rounded-md"
          aria-hidden
        >
          <img
            src={brandLogoPlaceholder}
            alt=""
            className="size-full object-cover"
          />
        </span>
        <span className="flex flex-col items-center gap-1">
          <span className="text-[22px] font-semibold leading-normal text-guest-feedback-text">
            {displayLocation}
          </span>
          {displayAddress ? (
            <span className="text-xs leading-normal text-guest-feedback-muted">
              {displayAddress}
            </span>
          ) : null}
        </span>
      </motion.div>

      <motion.div
        variants={shouldReduceMotion ? undefined : containerVariants}
        initial={shouldReduceMotion ? false : "hidden"}
        animate="visible"
        className="mt-15 flex flex-col items-center gap-3"
      >
        <motion.h1
          variants={shouldReduceMotion ? undefined : itemVariants}
          className="text-[22px] font-medium leading-normal text-guest-feedback-text"
        >
          Thank you.
        </motion.h1>
        <motion.p
          variants={shouldReduceMotion ? undefined : itemVariants}
          transition={shouldReduceMotion ? undefined : fadeTransition}
          className="max-w-63.75 text-xs leading-4.5 text-guest-feedback-muted"
        >
          Your feedback has been shared with the team at {displayLocation}
          {displayAddress ? `, ${displayAddress}` : ""}.
        </motion.p>
      </motion.div>
    </motion.div>
  )
}

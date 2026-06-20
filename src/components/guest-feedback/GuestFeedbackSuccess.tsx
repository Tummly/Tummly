import {
  motion,
  useReducedMotion,
  type Transition,
  type Variants,
} from "framer-motion"
import successBadge from "@/assets/svg/guest-feedback-success-badge.svg"

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
  className?: string
}

export function GuestFeedbackSuccess({ className }: GuestFeedbackSuccessProps) {
  const shouldReduceMotion = useReducedMotion()

  return (
    <motion.div
      initial={shouldReduceMotion ? false : { opacity: 0, scale: 0.96, y: 12 }}
      animate={{ opacity: 1, scale: 1, y: 0 }}
      transition={shouldReduceMotion ? { duration: 0 } : cardSpring}
      className={cn(
        "relative mx-auto w-full rounded-[10px] border border-guest-feedback-border bg-guest-feedback-surface px-5 pb-[30px] pt-[50px] text-center",
        "max-w-[min(100%,333px)]",
        "sm:max-w-[min(100%,400px)] sm:px-8 sm:pb-10 sm:pt-[60px]",
        "md:max-w-[min(100%,440px)]",
        className
      )}
    >
      <span
        aria-hidden
        className="absolute left-[-12px] top-1/2 size-[18px] -translate-y-1/2 rounded-[20px] bg-guest-feedback-bg"
      />
      <span
        aria-hidden
        className="absolute right-[-12px] top-1/2 size-[18px] -translate-y-1/2 rounded-[20px] bg-guest-feedback-bg"
      />

      <motion.div
        initial={shouldReduceMotion ? false : { opacity: 0, scale: 0.7, y: 8 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        transition={shouldReduceMotion ? { duration: 0 } : { ...cardSpring, delay: 0.08 }}
        className="absolute left-1/2 top-[-21px] -translate-x-1/2"
      >
        <img
          src={successBadge}
          alt=""
          className="size-[46px]"
          aria-hidden
        />
      </motion.div>

      <motion.div
        variants={shouldReduceMotion ? undefined : containerVariants}
        initial={shouldReduceMotion ? false : "hidden"}
        animate="visible"
        className="flex flex-col items-center gap-3"
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
          className="max-w-[255px] text-xs leading-[18px] text-guest-feedback-muted"
        >
          Your feedback is private. It&apos;s shared only with the restaurant
          team and won&apos;t be posted publicly.
        </motion.p>
      </motion.div>
    </motion.div>
  )
}

import { zodResolver } from "@hookform/resolvers/zod"
import {
  motion,
  useReducedMotion,
  type Transition,
  type Variants,
} from "framer-motion"
import { useForm } from "react-hook-form"
import { Link } from "react-router-dom"

import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { FormFloatingTextarea } from "@/components/form/FormFloatingTextarea"
import {
  useGuestLoopStepCanSubmit,
  useGuestLoopStepValidationFeedback,
} from "@/components/guest-loop/useGuestLoopStepCanSubmit"
import { Button } from "@/components/ui/button"
import { Form } from "@/components/ui/form"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { cn } from "@/lib/utils"
import { defaultFormValidationOptions } from "@/lib/form"
import {
  guestFeedbackDefaultValues,
  guestFeedbackFields,
  guestFeedbackSchema,
  type GuestFeedbackFormValues,
} from "@/schemas/guestFeedback"

const formSpring: Transition = {
  type: "spring",
  stiffness: 420,
  damping: 34,
  mass: 0.85,
}

const containerVariants: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.07,
      delayChildren: 0.04,
    },
  },
}

const itemVariants: Variants = {
  hidden: { opacity: 0, y: 16 },
  visible: {
    opacity: 1,
    y: 0,
    transition: formSpring,
  },
}

const legalLinkClassName =
  "rounded-sm underline underline-offset-2 transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-guest-feedback-accent/40"

type GuestFeedbackFormProps = {
  restaurantName: string
  locationName: string
  isSubmitting: boolean
  submitError: string | null
  defaultValues?: GuestFeedbackFormValues
  onSubmit: (values: GuestFeedbackFormValues) => Promise<void>
  onRetry: () => void
}

export function GuestFeedbackForm({
  restaurantName,
  locationName,
  isSubmitting,
  submitError,
  defaultValues = guestFeedbackDefaultValues,
  onSubmit,
  onRetry,
}: GuestFeedbackFormProps) {
  const shouldReduceMotion = useReducedMotion()
  const form = useForm<GuestFeedbackFormValues>({
    ...defaultFormValidationOptions,
    resolver: zodResolver(guestFeedbackSchema),
    defaultValues,
  })

  const canSubmit = useGuestLoopStepCanSubmit(
    form,
    guestFeedbackFields,
    guestFeedbackSchema
  )

  useGuestLoopStepValidationFeedback(
    form,
    guestFeedbackFields,
    guestFeedbackSchema,
    canSubmit
  )

  const displayName = restaurantName.trim() || "this restaurant"
  const displayLocation = locationName.trim()

  const handleSubmit = form.handleSubmit(async (values) => {
    await onSubmit(values)
  })

  return (
    <Form {...form}>
      <motion.form
        variants={shouldReduceMotion ? undefined : containerVariants}
        initial={shouldReduceMotion ? false : "hidden"}
        animate="visible"
        onSubmit={(event) => void handleSubmit(event)}
        className="flex w-full flex-col gap-10"
      >
        <div className="flex flex-col gap-[38px]">
          <motion.div
            variants={shouldReduceMotion ? undefined : itemVariants}
            className="flex flex-col gap-[7px]"
          >
            <h1 className="text-[clamp(1.5rem,6vw,1.875rem)] font-medium leading-normal text-guest-feedback-text">
              Share private feedback with {displayName}
            </h1>
            <p className="text-xs font-medium leading-normal text-guest-feedback-text">
              {displayLocation
                ? `Tell the team at ${displayLocation} what you thought.`
                : "Tell the team what you thought."}
            </p>
          </motion.div>

          <div className="flex flex-col gap-7">
            <motion.div
              variants={shouldReduceMotion ? undefined : itemVariants}
              className="flex flex-col gap-3"
            >
              <FormFloatingInput
                control={form.control}
                name="guestName"
                label="Your name"
                variant="dark"
                liveValidate
                disabled={isSubmitting}
                autoComplete="name"
              />
              <FormFloatingInput
                control={form.control}
                name="guestContact"
                label="Email or phone number"
                variant="dark"
                liveValidate
                disabled={isSubmitting}
                autoComplete="email"
              />
              <FormFloatingTextarea
                control={form.control}
                name="comment"
                label="Leave your feedback"
                variant="dark"
                liveValidate
                disabled={isSubmitting}
              />
            </motion.div>

            <motion.p
              variants={shouldReduceMotion ? undefined : itemVariants}
              className="text-xs font-medium leading-normal text-guest-feedback-text"
            >
              By continuing, you agree to the{" "}
              <Link to={LEGAL_ROUTES.terms} className={legalLinkClassName}>
                Terms
              </Link>{" "}
              and{" "}
              <Link to={LEGAL_ROUTES.privacy} className={legalLinkClassName}>
                Privacy
              </Link>
              .
            </motion.p>
          </div>
        </div>

        <motion.div
          variants={shouldReduceMotion ? undefined : itemVariants}
          className="flex flex-col gap-[22px]"
        >
          {submitError ? (
            <div
              role="alert"
              className="rounded-[4px] border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive"
            >
              <p>{submitError}</p>
              <button
                type="button"
                onClick={onRetry}
                className="mt-1 font-medium underline underline-offset-2"
              >
                Try again
              </button>
            </div>
          ) : null}

          <motion.div
            whileTap={
              shouldReduceMotion || !canSubmit || isSubmitting
                ? undefined
                : { scale: 0.985 }
            }
            transition={formSpring}
          >
            <Button
              type="submit"
              disabled={!canSubmit || isSubmitting}
              className={cn(
                "h-auto min-h-[50px] w-full rounded-[54px] border border-[rgba(20,162,71,0)] px-[17px] py-[13px] text-sm font-medium leading-normal shadow-none",
                canSubmit && !isSubmitting
                  ? "bg-guest-feedback-accent text-white hover:bg-[#129641]"
                  : "bg-[#2a2a2a] text-guest-feedback-muted hover:bg-[#2a2a2a]"
              )}
            >
              {isSubmitting ? "Submitting..." : "Submit my feedback"}
            </Button>
          </motion.div>

          <p className="text-center text-xs font-medium leading-normal text-white">
            Your feedback is shared privately with the restaurant team. Your
            details won&apos;t be posted publicly.
          </p>
        </motion.div>
      </motion.form>
    </Form>
  )
}

import { zodResolver } from "@hookform/resolvers/zod"
import {
  motion,
  useReducedMotion,
  type Transition,
  type Variants,
} from "framer-motion"
import { useEffect, useMemo, useSyncExternalStore } from "react"
import { useForm } from "react-hook-form"
import { Link } from "react-router-dom"

import { transcribeGuestAudio } from "@/api/scanApi"
import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { GuestFeedbackMicChrome } from "@/components/guest-feedback/GuestFeedbackMicChrome"
import {
  useGuestLoopStepCanSubmit,
  useGuestLoopStepValidationFeedback,
} from "@/components/guest-loop/useGuestLoopStepCanSubmit"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { FieldGroup } from "@/components/ui/field"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { createBrowserGuestMicAdapters } from "@/lib/guestFeedback/createBrowserGuestMicAdapters"
import { createGuestMicSttModule } from "@/lib/guestFeedback/createGuestMicSttModule"
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

// Card drop shadow from Figma (Guest-Loop-MVP node 3216:25970).
const cardShadowClassName =
  "shadow-[-133px_98px_46px_0_rgba(0,0,0,0.01),-85px_63px_42px_0_rgba(0,0,0,0.05),-48px_35px_36px_0_rgba(0,0,0,0.15),-21px_16px_26px_0_rgba(0,0,0,0.26),-5px_4px_15px_0_rgba(0,0,0,0.30)]"

type GuestFeedbackFormProps = {
  token: string
  locationName: string
  address: string
  isSubmitting: boolean
  submitError: string | null
  defaultValues?: GuestFeedbackFormValues
  onSubmit: (values: GuestFeedbackFormValues) => Promise<void>
  onRetry: () => void
}

export function GuestFeedbackForm({
  token,
  locationName,
  address,
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

  const { setValue } = form

  const { micModule, micLevelSource } = useMemo(() => {
    const { adapters, audioLevelSource } = createBrowserGuestMicAdapters({
      transcribe: (audio) => transcribeGuestAudio(token, audio),
      replaceComment: (text) => {
        setValue("comment", text, {
          shouldDirty: true,
          shouldTouch: true,
          shouldValidate: true,
        })
      },
    })
    return {
      micModule: createGuestMicSttModule(adapters),
      micLevelSource: audioLevelSource,
    }
  }, [setValue, token])

  useEffect(() => {
    return () => {
      void micModule.cancel()
      micModule.reset()
    }
  }, [micModule])

  const mic = useSyncExternalStore(
    micModule.subscribe,
    micModule.getSnapshot,
    micModule.getSnapshot
  )

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

  const displayLocation = locationName.trim() || "this location"
  const displayAddress = address.trim()
  const submitBusy = mic.submitLocked || isSubmitting
  const commentNotice = mic.truncateNotice
  const commentError = mic.error?.message

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
        className="flex w-full flex-col gap-8"
      >
        <motion.header
          variants={shouldReduceMotion ? undefined : itemVariants}
          className="flex items-center gap-3"
        >
          <span
            className="size-12 shrink-0 overflow-hidden rounded-md"
            aria-hidden
          >
            <img
              src={brandLogoPlaceholder}
              alt=""
              className="size-full object-cover"
            />
          </span>
          <span className="flex min-w-0 flex-col gap-0.5">
            <span className="truncate text-base font-semibold leading-snug text-guest-feedback-text">
              {displayLocation}
            </span>
            {displayAddress ? (
              <span className="truncate text-xs leading-snug text-guest-feedback-muted">
                {displayAddress}
              </span>
            ) : null}
          </span>
        </motion.header>

        <motion.div
          variants={shouldReduceMotion ? undefined : itemVariants}
          className="flex flex-col gap-2"
        >
          <h1 className="text-[clamp(1.5rem,6vw,1.875rem)] font-medium leading-tight text-guest-feedback-text">
            Tell us about your experience
          </h1>
          <p className="text-sm leading-relaxed text-guest-feedback-muted">
            Your feedback is shared privately with the team at{" "}
            {displayLocation}
            {displayAddress ? `, ${displayAddress}` : ""}. They may follow up
            using the contact details you provide.
          </p>
        </motion.div>

        <FieldGroup className="gap-4">
          <motion.div variants={shouldReduceMotion ? undefined : itemVariants}>
            <Card
              className={cn(
                "gap-0 rounded-[8px] bg-guest-feedback-bg py-0 text-guest-feedback-text ring-guest-feedback-border",
                cardShadowClassName
              )}
            >
              <CardHeader className="sr-only">
                <CardTitle>Feedback</CardTitle>
              </CardHeader>
              <CardContent className="p-0">
                <FormField
                  control={form.control}
                  name="comment"
                  render={({ field, fieldState }) => (
                    <FormItem className="gap-0">
                      <div className="relative">
                        <FormControl>
                          <Textarea
                            {...field}
                            placeholder="Add your own feedback…"
                            disabled={isSubmitting}
                            readOnly={mic.messageLocked}
                            aria-invalid={Boolean(
                              fieldState.error || commentError
                            )}
                            className="min-h-40 resize-none rounded-[8px] border-0 bg-transparent px-5 pb-16 pt-5 text-base text-guest-feedback-text shadow-none placeholder:text-guest-feedback-placeholder focus-visible:border-transparent focus-visible:ring-1 focus-visible:ring-guest-feedback-accent/50 disabled:bg-transparent aria-invalid:ring-0 dark:aria-invalid:ring-0"
                          />
                        </FormControl>
                        <div
                          className={cn(
                            "absolute bottom-3 right-3",
                            mic.chrome === "tick_cancel" && "left-3"
                          )}
                        >
                          <GuestFeedbackMicChrome
                            chrome={mic.chrome}
                            micAvailable={mic.micAvailable}
                            levelSource={micLevelSource}
                            disabled={isSubmitting}
                            onStart={() => {
                              void micModule.start()
                            }}
                            onConfirm={() => {
                              void micModule.confirm()
                            }}
                            onCancel={() => {
                              void micModule.cancel()
                            }}
                          />
                        </div>
                      </div>
                      {commentError ? (
                        <p
                          role="alert"
                          className="px-5 pb-3 text-sm text-destructive"
                        >
                          {commentError}
                        </p>
                      ) : (
                        <FormMessage className="px-5 pb-3" />
                      )}
                      {commentNotice ? (
                        <p className="px-5 pb-3 text-sm text-guest-feedback-muted">
                          {commentNotice}
                        </p>
                      ) : null}
                    </FormItem>
                  )}
                />
              </CardContent>
            </Card>
          </motion.div>

          <motion.div variants={shouldReduceMotion ? undefined : itemVariants}>
            <Card
              className={cn(
                "rounded-[8px] bg-guest-feedback-bg text-guest-feedback-text ring-guest-feedback-border [--card-spacing:--spacing(5)]",
                cardShadowClassName
              )}
            >
              <CardHeader>
                <CardTitle className="text-lg text-guest-feedback-text">
                  Your details
                </CardTitle>
                <CardDescription className="text-xs leading-relaxed text-guest-feedback-muted">
                  Add your name and one contact method so the team can follow up.
                </CardDescription>
              </CardHeader>
              <CardContent>
                <FieldGroup className="gap-3">
                  <FormField
                    control={form.control}
                    name="guestName"
                    render={({ field, fieldState }) => (
                      <FormItem className="gap-1.5">
                        <FormControl>
                          <Input
                            {...field}
                            placeholder="Your name"
                            disabled={isSubmitting}
                            autoComplete="name"
                            aria-invalid={Boolean(fieldState.error)}
                            className="h-12 border-guest-feedback-border bg-transparent px-4 text-base text-guest-feedback-text placeholder:text-guest-feedback-placeholder focus-visible:border-guest-feedback-accent/60 focus-visible:ring-guest-feedback-accent/20 disabled:bg-transparent"
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="guestContact"
                    render={({ field, fieldState }) => (
                      <FormItem className="gap-1.5">
                        <FormControl>
                          <Input
                            {...field}
                            placeholder="Email or phone number"
                            disabled={isSubmitting}
                            autoComplete="email"
                            inputMode="email"
                            aria-invalid={Boolean(fieldState.error)}
                            className="h-12 border-guest-feedback-border bg-transparent px-4 text-base text-guest-feedback-text placeholder:text-guest-feedback-placeholder focus-visible:border-guest-feedback-accent/60 focus-visible:ring-guest-feedback-accent/20 disabled:bg-transparent"
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormCheckboxLabel
                    control={form.control}
                    name="acceptsOffers"
                    id="accepts-offers"
                    disabled={isSubmitting}
                    className="pt-1"
                    labelClassName="cursor-pointer text-xs font-normal leading-relaxed text-guest-feedback-muted"
                  >
                    {displayLocation} may contact you about your feedback and
                    may also send you offers using the contact details you
                    provide. Untick here if you would prefer not to receive
                    offers.
                  </FormCheckboxLabel>
                </FieldGroup>
              </CardContent>
            </Card>
          </motion.div>
        </FieldGroup>

        <motion.nav
          variants={shouldReduceMotion ? undefined : itemVariants}
          aria-label="Legal"
          className="flex items-center justify-center gap-2 text-xs text-guest-feedback-muted"
        >
          <Link to={LEGAL_ROUTES.terms} className={legalLinkClassName}>
            Terms &amp; Conditions
          </Link>
          <span aria-hidden>·</span>
          <Link to={LEGAL_ROUTES.privacy} className={legalLinkClassName}>
            Privacy Notice
          </Link>
        </motion.nav>

        <motion.div
          variants={shouldReduceMotion ? undefined : itemVariants}
          className="flex flex-col gap-3"
        >
          {submitError ? (
            <div
              role="alert"
              className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive"
            >
              <p>{submitError}</p>
              <Button
                type="button"
                variant="link-destructive"
                size="link-sm"
                onClick={onRetry}
                className="mt-1"
              >
                Try again
              </Button>
            </div>
          ) : null}

          <motion.div
            whileTap={
              shouldReduceMotion || !canSubmit || submitBusy
                ? undefined
                : { scale: 0.985 }
            }
            transition={formSpring}
          >
            <Button
              type="submit"
              disabled={!canSubmit || submitBusy}
              className={cn(
                "h-auto min-h-12.5 w-full rounded-[54px] px-4.25 py-3.25 text-sm leading-normal shadow-none",
                canSubmit && !submitBusy
                  ? "bg-guest-feedback-accent text-white hover:bg-[#129641]"
                  : "bg-[#2a2a2a] text-guest-feedback-muted hover:bg-[#2a2a2a]"
              )}
            >
              {isSubmitting ? "Submitting..." : "Submit feedback"}
            </Button>
          </motion.div>
        </motion.div>
      </motion.form>
    </Form>
  )
}

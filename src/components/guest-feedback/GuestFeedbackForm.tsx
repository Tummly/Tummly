import { zodResolver } from "@hookform/resolvers/zod"
import { motion, useReducedMotion, type Variants } from "framer-motion"
import { useEffect, useMemo, useSyncExternalStore } from "react"
import { useForm, useWatch } from "react-hook-form"
import { Link } from "react-router-dom"

import { transcribeGuestAudio } from "@/api/scanApi"
import { BrandLogoMark } from "@/components/brand/BrandLogoMark"
import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { GuestFeedbackDictationGlowMotion } from "@/components/guest-feedback/GuestFeedbackDictationGlowMotion"
import { GuestFeedbackMicChrome } from "@/components/guest-feedback/GuestFeedbackMicChrome"
import { GuestFeedbackPoweredBy } from "@/components/guest-feedback/GuestFeedbackPoweredBy"
import {
  useGuestLoopStepCanSubmit,
  useGuestLoopStepValidationFeedback,
} from "@/components/guest-loop/useGuestLoopStepCanSubmit"
import { Button } from "@/components/ui/button"
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
import { guestFeedbackCommentPresentation } from "@/lib/guestFeedback/guestFeedbackCommentPresentation"
import {
  buildGuestFormConsentCheckboxLabel,
  buildGuestFormIntroCopy,
  resolveGuestFormMarketingChannel,
  type GuestFormConsentConfig,
} from "@/lib/guestFeedback/guestFormConsentPresentation"
import { getRecaptchaSiteKey } from "@/lib/guestFeedback/executeGuestFeedbackRecaptcha"
import {
  guestFeedbackEnterTransition,
} from "@/lib/guestFeedback/guestFeedbackMotionTokens"
import { cn } from "@/lib/utils"
import { defaultFormValidationOptions } from "@/lib/form"
import {
  GUEST_FEEDBACK_BODY_CLASS,
  GUEST_FEEDBACK_COMPOSER_CLASS,
  GUEST_FEEDBACK_DETAILS_PANEL_CLASS,
  GUEST_FEEDBACK_FIELD_INPUT_CLASS,
  GUEST_FEEDBACK_FORM_HEADER_CLASS,
  GUEST_FEEDBACK_FORM_STACK_CLASS,
  GUEST_FEEDBACK_FORM_TITLE_CLASS,
  GUEST_FEEDBACK_PRIMARY_BUTTON_CLASS,
  GUEST_FEEDBACK_RESTAURANT_NAME_CLASS,
} from "@/lib/guestFeedback/guestFeedbackLayoutPresentation"
import {
  guestFeedbackDefaultValues,
  guestFeedbackFields,
  guestFeedbackSchema,
  type GuestFeedbackFormValues,
} from "@/schemas/guestFeedback"

const containerVariants: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.06,
      delayChildren: 0.04,
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

const legalLinkClassName =
  "rounded-sm transition-colors hover:text-guest-feedback-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-guest-feedback-accent/40"

type GuestFeedbackFormProps = {
  token: string
  /** Venue display name — used in intro copy as [Location]. */
  locationName: string
  /** Restaurant name for header + intro + marketing checkbox ([Restaurant]). */
  restaurantName?: string
  address: string
  brandLogoPublicUrl?: string | null
  guestFormConsent?: GuestFormConsentConfig | null
  isSubmitting: boolean
  submitError: string | null
  defaultValues?: GuestFeedbackFormValues
  onSubmit: (values: GuestFeedbackFormValues) => Promise<void>
  onRetry: () => void
}

export function GuestFeedbackForm({
  token,
  locationName,
  restaurantName,
  address,
  brandLogoPublicUrl = null,
  guestFormConsent = null,
  isSubmitting,
  submitError,
  defaultValues = guestFeedbackDefaultValues,
  onSubmit,
  onRetry,
}: GuestFeedbackFormProps) {
  const shouldReduceMotion = useReducedMotion()
  const showRecaptchaAttribution = getRecaptchaSiteKey() != null
  const form = useForm<GuestFeedbackFormValues>({
    ...defaultFormValidationOptions,
    resolver: zodResolver(guestFeedbackSchema),
    defaultValues,
  })

  const { setValue, control } = form
  const guestContact = useWatch({ control, name: "guestContact" }) ?? ""
  const marketingChannel = resolveGuestFormMarketingChannel(
    guestContact,
    guestFormConsent
  )

  useEffect(() => {
    // GF-03: marketing starts unchecked; clear when the channel hides or switches.
    setValue("acceptsOffers", false, {
      shouldDirty: false,
      shouldTouch: false,
      shouldValidate: false,
    })
  }, [marketingChannel, setValue])

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
  const displayRestaurant =
    (restaurantName ?? locationName).trim() || "this restaurant"
  const displayAddress = address.trim()
  const submitBusy = mic.submitLocked || isSubmitting
  const commentNotice = mic.truncateNotice
  const commentError = mic.error?.message
  const showConsentCheckbox = marketingChannel != null
  const consentCheckboxLabel =
    marketingChannel == null
      ? null
      : buildGuestFormConsentCheckboxLabel(displayRestaurant, marketingChannel)
  const introCopy = buildGuestFormIntroCopy(
    displayRestaurant,
    displayLocation
  )
  const isComposerBusy =
    mic.phase === "recording" || mic.phase === "transcribing"
  const showListeningGlow = mic.phase === "recording"

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
        className={GUEST_FEEDBACK_FORM_STACK_CLASS}
      >
        <div className="flex flex-col gap-3.5 lg:gap-4">
          <motion.header
            variants={shouldReduceMotion ? undefined : itemVariants}
            className={GUEST_FEEDBACK_FORM_HEADER_CLASS}
          >
            <div className="flex items-center gap-3 lg:gap-3.5">
              <BrandLogoMark
                brandLogoPublicUrl={brandLogoPublicUrl}
                className="size-[42px] lg:size-12"
                roundedClassName="rounded"
              />
              <span className="flex min-w-0 flex-col gap-1 font-heading">
                <span className={GUEST_FEEDBACK_RESTAURANT_NAME_CLASS}>
                  {displayRestaurant}
                </span>
                {displayAddress ? (
                  <span className="truncate text-xs font-semibold leading-normal text-[#9e9e9e] lg:text-sm">
                    {displayAddress}
                  </span>
                ) : null}
              </span>
            </div>

            <div className="flex flex-col gap-3">
              <h1 className={GUEST_FEEDBACK_FORM_TITLE_CLASS}>
                Tell us about your experience
              </h1>
              <p className={GUEST_FEEDBACK_BODY_CLASS}>{introCopy}</p>
            </div>
          </motion.header>

          <div className="flex flex-col gap-3.5">
            <motion.div
              variants={shouldReduceMotion ? undefined : itemVariants}
              className="min-h-0"
            >
              <FormField
                control={form.control}
                name="comment"
                render={({ field, fieldState }) => {
                  const commentUi = guestFeedbackCommentPresentation(mic.phase)

                  return (
                    <FormItem className="gap-0">
                      <div
                        className={cn(
                          GUEST_FEEDBACK_COMPOSER_CLASS,
                          isComposerBusy &&
                            "border-2 border-guest-feedback-border-soft bg-guest-feedback-glass-strong"
                        )}
                      >
                        {showListeningGlow ? (
                          <GuestFeedbackDictationGlowMotion />
                        ) : null}

                        <FormControl>
                          <Textarea
                            {...field}
                            placeholder={
                              commentUi.isRecording
                                ? undefined
                                : commentUi.placeholder
                            }
                            disabled={isSubmitting}
                            readOnly={mic.messageLocked}
                            aria-invalid={Boolean(
                              fieldState.error || commentError
                            )}
                            className={cn(
                              "relative z-1 min-h-[120px] flex-1 resize-none rounded-none border-0 bg-transparent px-5 pt-5 pb-3 text-sm font-normal leading-normal text-guest-feedback-text shadow-none placeholder:text-guest-feedback-placeholder focus-visible:border-transparent focus-visible:ring-0 disabled:bg-transparent aria-invalid:ring-0 dark:aria-invalid:ring-0 lg:min-h-[140px] lg:px-6 lg:pt-6 lg:text-base",
                              commentUi.isRecording &&
                                "text-transparent caret-transparent selection:bg-transparent"
                            )}
                          />
                        </FormControl>
                        {commentUi.isRecording ? (
                          <p
                            aria-hidden
                            className="pointer-events-none absolute top-5 right-5 left-5 z-1 text-sm leading-normal text-guest-feedback-placeholder lg:top-6 lg:right-6 lg:left-6 lg:text-base"
                          >
                            {commentUi.recordingHint}
                          </p>
                        ) : null}
                        <div className="sr-only" aria-live="polite">
                          {commentUi.isRecording
                            ? commentUi.recordingHint
                            : ""}
                        </div>

                        <div className="relative z-1 px-5 pb-5 lg:px-6 lg:pb-6">
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
                          className="px-2 pt-2 text-sm text-destructive"
                        >
                          {commentError}
                        </p>
                      ) : (
                        <FormMessage className="px-2 pt-2" />
                      )}
                      {commentNotice ? (
                        <p className="px-2 pt-2 text-sm text-guest-feedback-muted">
                          {commentNotice}
                        </p>
                      ) : null}
                    </FormItem>
                  )
                }}
              />
            </motion.div>

            <motion.div variants={shouldReduceMotion ? undefined : itemVariants}>
              <section className={GUEST_FEEDBACK_DETAILS_PANEL_CLASS}>
                <div className="flex flex-col gap-3.5">
                  <div className="flex flex-col gap-2 text-sm lg:text-base">
                    <h2 className="m-0 font-heading text-sm font-bold leading-normal text-guest-feedback-text lg:text-base">
                      Your details
                    </h2>
                    <p className="m-0 max-w-[16rem] text-sm leading-[19px] text-guest-feedback-muted-soft lg:max-w-md lg:text-base lg:leading-6">
                      Add your details so the team can respond to your feedback.
                    </p>
                  </div>

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
                              className={GUEST_FEEDBACK_FIELD_INPUT_CLASS}
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
                              placeholder="Email or UK mobile number"
                              disabled={isSubmitting}
                              autoComplete="email"
                              inputMode="email"
                              aria-invalid={Boolean(fieldState.error)}
                              className={GUEST_FEEDBACK_FIELD_INPUT_CLASS}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </FieldGroup>
                </div>

                {showConsentCheckbox && consentCheckboxLabel ? (
                  <FormCheckboxLabel
                    control={form.control}
                    name="acceptsOffers"
                    id="accepts-offers"
                    variant="ghost"
                    disabled={isSubmitting}
                    labelClassName="cursor-pointer text-xs font-normal leading-4 text-guest-feedback-muted-soft"
                  >
                    {consentCheckboxLabel}
                  </FormCheckboxLabel>
                ) : null}
              </section>
            </motion.div>
          </div>
        </div>

        <motion.div
          variants={shouldReduceMotion ? undefined : itemVariants}
          className="flex flex-col items-center gap-[29px] lg:gap-9"
        >
          <GuestFeedbackPoweredBy placement="inline" />

          <div className="flex w-full flex-col gap-3">
            {submitError ? (
              <div
                role="alert"
                className="rounded-[18px] border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive"
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

            <Button
              type="submit"
              disabled={!canSubmit || submitBusy}
              className={cn(
                GUEST_FEEDBACK_PRIMARY_BUTTON_CLASS,
                canSubmit && !submitBusy
                  ? "bg-guest-feedback-submit text-guest-feedback-submit-fg hover:bg-white"
                  : "bg-guest-feedback-secondary text-guest-feedback-secondary-fg hover:bg-guest-feedback-secondary"
              )}
            >
              {isSubmitting ? "Submitting..." : "Submit feedback"}
            </Button>
          </div>

          <nav
            aria-label="Legal"
            className="flex flex-col items-center gap-2 text-xs text-guest-feedback-muted-soft"
          >
            <div className="flex items-center justify-center gap-1.5">
              <Link to={LEGAL_ROUTES.terms} className={legalLinkClassName}>
                Terms &amp; Conditions
              </Link>
              <span aria-hidden>·</span>
              <Link to={LEGAL_ROUTES.privacy} className={legalLinkClassName}>
                Privacy Notice
              </Link>
            </div>
            {showRecaptchaAttribution ? (
              <p className="max-w-sm text-center text-[11px] leading-relaxed text-guest-feedback-muted/80">
                This site is protected by reCAPTCHA and the Google{" "}
                <a
                  href="https://policies.google.com/privacy"
                  target="_blank"
                  rel="noopener noreferrer"
                  className={legalLinkClassName}
                >
                  Privacy Policy
                </a>{" "}
                and{" "}
                <a
                  href="https://policies.google.com/terms"
                  target="_blank"
                  rel="noopener noreferrer"
                  className={legalLinkClassName}
                >
                  Terms of Service
                </a>{" "}
                apply.
              </p>
            ) : null}
          </nav>
        </motion.div>
      </motion.form>
    </Form>
  )
}

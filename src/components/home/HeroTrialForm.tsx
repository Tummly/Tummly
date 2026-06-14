import { useLayoutEffect, useRef, useState } from "react"
import type { FormEvent } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { isAxiosError } from "axios"
import { useForm } from "react-hook-form"
import { Link, useLocation } from "react-router-dom"

import heroFormAccent from "@/assets/svg/hero-form-accent.svg"
import {
  resendOtpRequest,
  submitTrialRequest,
  verifyOtpRequest,
} from "@/api/trialApi"
import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { FormFloatingSelect } from "@/components/form/FormFloatingSelect"
import HeroTrialOtpStep from "@/components/home/HeroTrialOtpStep"
import HeroTrialSuccessStep from "@/components/home/HeroTrialSuccessStep"
import {
  BUSINESS_CATEGORY_OPTIONS,
  LOCATION_COUNT_OPTIONS,
  MAIN_GOAL_OPTIONS,
  ROLE_OPTIONS,
} from "@/components/home/hero-trial-options"
import {
  mapResendApiMessage,
  mapVerifyApiMessage,
  MAX_VERIFY_ATTEMPTS,
  OTP_MESSAGES,
  RESEND_COOLDOWN_SECONDS,
  type OtpFeedback,
} from "@/components/home/hero-trial-otp"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { Form } from "@/components/ui/form"
import { useCountdown } from "@/hooks/use-countdown"
import { defaultFormValidationOptions } from "@/lib/form"
import {
  toTrialRequestPayload,
  trialRequestDefaultValues,
  trialRequestSchema,
  type TrialRequestFormValues,
} from "@/schemas/trialRequest"

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

function HeroTrialForm() {
  const location = useLocation()
  const form = useForm<TrialRequestFormValues>({
    resolver: zodResolver(trialRequestSchema),
    defaultValues: trialRequestDefaultValues,
    ...defaultFormValidationOptions,
  })

  const [step, setStep] = useState<"form" | "otp" | "success">("form")
  const [otpCode, setOtpCode] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [otpFeedback, setOtpFeedback] = useState<OtpFeedback | null>(null)
  const [verifyAttempts, setVerifyAttempts] = useState(0)
  const previousOtpEmailRef = useRef<string | null>(null)
  const stepContentRef = useRef<HTMLDivElement>(null)
  const [stepMinHeight, setStepMinHeight] = useState<number | null>(null)
  const {
    secondsRemaining: resendSecondsRemaining,
    isComplete: canResend,
    restart: restartResendTimer,
  } = useCountdown(RESEND_COOLDOWN_SECONDS, step === "otp")

  const email = form.watch("email")
  const rootError = form.formState.errors.root?.message

  const onSubmitTrialRequest = async (values: TrialRequestFormValues) => {
    setOtpFeedback(null)
    form.clearErrors("root")

    const payload = toTrialRequestPayload(values)
    const emailChanged =
      previousOtpEmailRef.current !== null &&
      previousOtpEmailRef.current !== payload.email

    setSubmitting(true)

    try {
      await submitTrialRequest(payload)

      setOtpCode("")
      setVerifyAttempts(0)
      previousOtpEmailRef.current = payload.email
      restartResendTimer(RESEND_COOLDOWN_SECONDS)

      if (emailChanged) {
        setOtpFeedback({
          kind: "info",
          code: "email_changed",
          message: OTP_MESSAGES.email_changed,
        })
      } else {
        setOtpFeedback(null)
      }

      setStep("otp")
    } catch (error) {
      const message = getApiErrorMessage(
        error,
        "We couldn't send your request. Please try again."
      )
      const normalized = message.toLowerCase()

      if (
        normalized.includes("already registered") ||
        normalized.includes("already verified")
      ) {
        setOtpFeedback(mapVerifyApiMessage(message))
        setStep("otp")
        return
      }

      form.setError("root", { message })
    } finally {
      setSubmitting(false)
    }
  }

  const handleVerifyOtp = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setOtpFeedback(null)

    if (otpCode.trim().length !== 6) {
      setOtpFeedback({
        kind: "error",
        code: "invalid",
        message: OTP_MESSAGES.incomplete,
      })
      return
    }

    if (verifyAttempts >= MAX_VERIFY_ATTEMPTS) {
      setOtpFeedback({
        kind: "error",
        code: "too_many_attempts",
        message: OTP_MESSAGES.too_many_attempts,
      })
      return
    }

    setSubmitting(true)

    try {
      await verifyOtpRequest({
        email: email.trim().toLowerCase(),
        otpCode: otpCode.trim(),
      })
      setStep("success")
      setOtpFeedback(null)
    } catch (error) {
      const message = getApiErrorMessage(
        error,
        OTP_MESSAGES.invalid
      )
      const feedback = mapVerifyApiMessage(message)
      const nextAttempts = verifyAttempts + 1

      setVerifyAttempts(nextAttempts)

      if (nextAttempts >= MAX_VERIFY_ATTEMPTS) {
        setOtpFeedback({
          kind: "error",
          code: "too_many_attempts",
          message: OTP_MESSAGES.too_many_attempts,
        })
      } else {
        setOtpFeedback(feedback)
      }
    } finally {
      setSubmitting(false)
    }
  }

  const handleResendOtp = async () => {
    if (!canResend || submitting) return

    setOtpFeedback(null)
    setSubmitting(true)

    try {
      await resendOtpRequest(email.trim().toLowerCase())
      setOtpCode("")
      setVerifyAttempts(0)
      restartResendTimer(RESEND_COOLDOWN_SECONDS)
      setOtpFeedback({
        kind: "info",
        code: "code_resent",
        message: OTP_MESSAGES.code_resent,
      })
    } catch (error) {
      setOtpFeedback(
        mapResendApiMessage(
          getApiErrorMessage(
            error,
            "We couldn't resend the code. Try again shortly."
          )
        )
      )
    } finally {
      setSubmitting(false)
    }
  }

  const handleChangeEmail = () => {
    setStep("form")
    setOtpCode("")
    setOtpFeedback(null)
    setVerifyAttempts(0)
    form.clearErrors("root")
  }

  const handleOtpChange = (value: string) => {
    setOtpCode(value)
    if (otpFeedback?.kind === "error") {
      setOtpFeedback(null)
    }
  }

  const handleReturnToTummly = () => {
    if (location.pathname === "/") {
      form.reset(trialRequestDefaultValues)
      setStep("form")
      setOtpCode("")
      setOtpFeedback(null)
      setVerifyAttempts(0)
      previousOtpEmailRef.current = null
      return
    }

    window.scrollTo({ top: 0, behavior: "smooth" })
  }

  const handleSubmitAgain = () => {
    setStep("form")
    setOtpCode("")
    setOtpFeedback(null)
    setVerifyAttempts(0)
    form.clearErrors("root")
  }

  useLayoutEffect(() => {
    if (step !== "form") return

    const node = stepContentRef.current
    if (!node) return

    const updateHeight = () => {
      const height = node.offsetHeight
      if (height > 0) {
        setStepMinHeight((current) =>
          current == null ? height : Math.max(current, height)
        )
      }
    }

    updateHeight()

    const observer = new ResizeObserver(updateHeight)
    observer.observe(node)

    return () => observer.disconnect()
  }, [step])

  return (
    <div className="relative w-full max-w-[615px] shrink-0 overflow-hidden px-5 pb-8 pt-12 shadow-[0_18px_50px_rgba(0,0,0,0.18)] sm:px-8 sm:pb-9 sm:pt-14 lg:px-[38px] lg:pb-[38px] lg:pt-[68px] lg:shadow-none">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 z-0 bg-white"
      />

      <div
        aria-hidden
        className="pointer-events-none absolute left-0 top-[-5px] z-[1] h-[210px] w-[367px] overflow-hidden "
      >
        <div className="absolute left-[3.67px] top-[-5px]">
          <div className="absolute left-0 top-0 flex h-[209.635px] w-[363.027px] items-center justify-center">
            <div className="-scale-y-100 flex-none rotate-180">
              <div className="relative h-[209.635px] w-[363.027px]">
                <img
                  src={heroFormAccent}
                  alt=""
                  className="absolute inset-0 block size-full max-w-none"
                />
              </div>
            </div>
          </div>
        </div>
        <div className="absolute left-0 top-[-5px] flex h-[210px] w-[367px] items-center justify-center">
          <div className="-scale-y-100 flex-none rotate-180">
            <div
              className="h-[210px] w-[367px]"
              style={{
                backgroundImage:
                  "linear-gradient(10.784231689007541deg, rgb(255, 255, 255) 27.237%, rgba(255, 255, 255, 0.2) 71.441%), linear-gradient(87.63101003628996deg, rgb(255, 255, 255) 1.4701%, rgba(255, 255, 255, 0.2) 48.114%)",
              }}
            />
          </div>
        </div>
      </div>

      <div
        ref={stepContentRef}
        className="relative z-[2] flex w-full flex-col"
        style={
          step !== "form" && stepMinHeight != null
            ? { minHeight: stepMinHeight }
            : undefined
        }
      >
      {step === "form" ? (
        <Form {...form}>
          <form
            onSubmit={form.handleSubmit(onSubmitTrialRequest)}
            noValidate
            className="relative z-[2] flex w-full flex-col"
          >
            <header className="flex flex-col gap-3 text-[#232323] lg:gap-3 mb-7 sm:mb-8 lg:mb-[34px]">
              <h2 className="m-0 text-[clamp(1.375rem,3vw,1.75rem)] font-bold leading-[normal] tracking-[-0.56px]">
                Request your guided trial
              </h2>
              <p className="m-0 text-sm font-medium leading-[21px] tracking-[-0.32px] sm:text-base">
                Tell us about your restaurant. We&apos;ll verify your email, review
                your setup needs and send the right next step for your location or
                group.
              </p>
            </header>

            <div className="flex flex-col">
              <FormFloatingInput
                control={form.control}
                name="businessName"
                label="Restaurant / business name"
                disableFocusRing
                required
                errorClassName="mb-2"
              />

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <FormFloatingSelect
                  control={form.control}
                  name="businessCategory"
                  label="Business category"
                  options={BUSINESS_CATEGORY_OPTIONS}
                  disableFocusRing
                  required
                  errorClassName="mb-2"
                />
                <FormFloatingSelect
                  control={form.control}
                  name="locations"
                  label="Number of locations"
                  options={LOCATION_COUNT_OPTIONS}
                  disableFocusRing
                  required
                  errorClassName="mb-2"
                />
              </div>

              <FormFloatingInput
                control={form.control}
                name="businessLink"
                label="Business link"
                optional
                disableFocusRing
                errorClassName="mb-2"
              />

              <FormFloatingInput
                control={form.control}
                name="fullName"
                label="Your full name"
                disableFocusRing
                required
                errorClassName="mb-2"
              />

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <FormFloatingInput
                  control={form.control}
                  name="email"
                  type="email"
                  label="Email address"
                  autoComplete="email"
                  errorClassName="mb-2"
                  disableFocusRing
                  required
                />
                <FormFloatingInput
                  control={form.control}
                  name="mobile"
                  type="tel"
                  label="Mobile number"
                  autoComplete="tel"
                  errorClassName="mb-2"
                  disableFocusRing
                  required
                />
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <FormFloatingSelect
                  control={form.control}
                  name="role"
                  label="Your role"
                  options={ROLE_OPTIONS}
                  errorClassName="mb-2"
                  disableFocusRing
                  required
                />
                <FormFloatingSelect
                  control={form.control}
                  name="goal"
                  label="Main goal"
                  options={MAIN_GOAL_OPTIONS}
                  errorClassName="mb-2"
                  disableFocusRing
                  required
                />
              </div>
            </div>

            <FormCheckboxLabel
              control={form.control}
              name="termsAccepted"
              id="termsAccepted"
            >
              I confirm I&apos;m requesting Tummly for a restaurant or hospitality
              business and agree to be contacted about this request.
            </FormCheckboxLabel>

            <p className="m-0 text-sm font-medium leading-5 text-[#141414]">
              By continuing, you agree to the{" "}
              <Button
                variant="link"
                size="link-sm"
                asChild
                className="font-medium text-[#141414] underline underline-offset-2"
              >
                <a href="#">Terms</a>
              </Button>{" "}
              and{" "}
              <Button
                variant="link"
                size="link-sm"
                asChild
                className="font-medium text-[#141414] underline underline-offset-2"
              >
                <a href="#">Privacy Notice</a>
              </Button>
              .
            </p>

            <FieldErrorSlot error={rootError} reserveClassName="min-h-5" />

            <div className="mt-auto flex flex-col items-center gap-5 pt-7 sm:pt-8 lg:gap-[22px] lg:pt-[34px]">
              <Button
                type="submit"
                disabled={submitting}
                className="h-auto min-h-0 w-full rounded-[54px] border border-[rgba(20,162,71,0)] bg-[#14a247] px-[17px] py-[13px] text-base leading-5 text-white hover:bg-[#129641]"
              >
                {submitting ? "Sending..." : "Request guided trial"}
              </Button>

              <p className="m-0 flex flex-wrap items-center justify-center gap-2.5 text-sm font-medium tracking-[0.4px] text-[#232323]">
                <span>Already have an account?</span>
                <Button
                  variant="link"
                  size="link-sm"
                  asChild
                  className="text-[#14a74a] underline underline-offset-2"
                >
                  <Link to="/login">Sign in</Link>
                </Button>
              </p>

              <p className="m-0 max-w-[313px] text-center text-sm font-medium leading-5 text-[#232323]">
                For restaurants and hospitality operators only. No payment is taken
                on this form.
              </p>
            </div>
          </form>
        </Form>
      ) : null}

      {step === "otp" ? (
        <HeroTrialOtpStep
          email={email.trim().toLowerCase()}
          otpCode={otpCode}
          submitting={submitting}
          feedback={otpFeedback}
          resendSecondsRemaining={resendSecondsRemaining}
          canResend={canResend}
          onOtpChange={handleOtpChange}
          onVerify={handleVerifyOtp}
          onResend={handleResendOtp}
          onChangeEmail={handleChangeEmail}
        />
      ) : null}

      {step === "success" ? (
        <HeroTrialSuccessStep
          onReturnToTummly={handleReturnToTummly}
          onSubmitAgain={handleSubmitAgain}
        />
      ) : null}
      </div>
    </div>
  )
}

export default HeroTrialForm

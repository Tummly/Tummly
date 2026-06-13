import { useLayoutEffect, useRef, useState } from "react"
import type { ChangeEvent, FormEvent } from "react"
import { Link, useLocation } from "react-router-dom"
import { isAxiosError } from "axios"

import heroFormAccent from "@/assets/svg/hero-form-accent.svg"
import {
  resendOtpRequest,
  submitTrialRequest,
  verifyOtpRequest,
} from "@/api/trialApi"
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
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import { FieldErrorSlot } from "@/components/ui/field"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { useCountdown } from "@/hooks/use-countdown"
import type { TrialRequestPayload } from "@/types/trial"

type FormErrors = Partial<Record<keyof TrialRequestPayload | "submit", string>>

type TrialFormData = TrialRequestPayload

const initialFormData: TrialFormData = {
  businessName: "",
  businessCategory: "",
  locations: "",
  businessLink: "",
  fullName: "",
  email: "",
  mobile: "",
  role: "",
  goal: "",
  termsAccepted: false,
}

function validateForm(data: TrialFormData): FormErrors {
  const errors: FormErrors = {}

  if (!data.businessName.trim()) {
    errors.businessName = "Restaurant / business name is required."
  }

  if (!data.businessCategory) {
    errors.businessCategory = "Select a business category."
  }

  if (!data.locations) {
    errors.locations = "Select the number of locations."
  }

  if (
    data.businessLink?.trim() &&
    !/^https?:\/\/.+/i.test(data.businessLink.trim())
  ) {
    errors.businessLink = "Enter a valid URL, including https://"
  }

  if (!data.fullName.trim()) {
    errors.fullName = "Your full name is required."
  }

  if (!data.email.trim()) {
    errors.email = "Email address is required."
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email.trim())) {
    errors.email = "Enter a valid email address."
  }

  if (!data.mobile.trim()) {
    errors.mobile = "Mobile number is required."
  } else if (!/^[0-9+\-\s]{10,15}$/.test(data.mobile.trim())) {
    errors.mobile = "Enter a valid mobile number."
  }

  if (!data.role) {
    errors.role = "Select your role."
  }

  if (!data.goal) {
    errors.goal = "Select your main goal."
  }

  if (!data.termsAccepted) {
    errors.termsAccepted = "Please confirm this request is for a hospitality business."
  }

  return errors
}

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
  const [formData, setFormData] = useState<TrialFormData>(initialFormData)
  const [errors, setErrors] = useState<FormErrors>({})
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

  const updateField = <K extends keyof TrialFormData>(
    key: K,
    value: TrialFormData[K]
  ) => {
    setFormData((current) => ({ ...current, [key]: value }))
    setErrors((current) => {
      if (!current[key]) return current
      const next = { ...current }
      delete next[key]
      return next
    })
  }

  const handleInputChange = (event: ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = event.target
    updateField(name as keyof TrialFormData, type === "checkbox" ? checked : value)
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setOtpFeedback(null)

    const nextErrors = validateForm(formData)
    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors)
      return
    }

    const trimmedEmail = formData.email.trim().toLowerCase()
    const emailChanged =
      previousOtpEmailRef.current !== null &&
      previousOtpEmailRef.current !== trimmedEmail

    setSubmitting(true)

    try {
      await submitTrialRequest({
        ...formData,
        businessName: formData.businessName.trim(),
        businessLink: formData.businessLink?.trim() || undefined,
        fullName: formData.fullName.trim(),
        email: trimmedEmail,
        mobile: formData.mobile.trim(),
      })

      setOtpCode("")
      setVerifyAttempts(0)
      setErrors({})
      previousOtpEmailRef.current = trimmedEmail
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

      setErrors({ submit: message })
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
        email: formData.email.trim().toLowerCase(),
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
      await resendOtpRequest(formData.email.trim().toLowerCase())
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
    setErrors({})
  }

  const handleOtpChange = (value: string) => {
    setOtpCode(value)
    if (otpFeedback?.kind === "error") {
      setOtpFeedback(null)
    }
  }

  const handleReturnToTummly = () => {
    if (location.pathname === "/") {
      setFormData(initialFormData)
      setStep("form")
      setOtpCode("")
      setOtpFeedback(null)
      setVerifyAttempts(0)
      setErrors({})
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
    setErrors({})
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
  }, [step, errors])

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
        <form
          onSubmit={handleSubmit}
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
            <FloatingLabelInput
              name="businessName"
              label="Restaurant / business name"
              value={formData.businessName}
              onChange={handleInputChange}
              error={errors.businessName}
              disableFocusRing
              required
              errorClassName="mb-2"
            />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FloatingLabelSelect
                label="Business category"
                name="businessCategory"
                value={formData.businessCategory}
                onValueChange={(value) => updateField("businessCategory", value)}
                options={BUSINESS_CATEGORY_OPTIONS}
                error={errors.businessCategory}
                disableFocusRing
                required
                errorClassName="mb-2"
              />
              <FloatingLabelSelect
                label="Number of locations"
                name="locations"
                value={formData.locations}
                onValueChange={(value) => updateField("locations", value)}
                options={LOCATION_COUNT_OPTIONS}
                error={errors.locations}
                disableFocusRing
                required
                errorClassName="mb-2"

              />
            </div>

            <FloatingLabelInput
              name="businessLink"
              label="Business link"
              optional
              value={formData.businessLink}
              onChange={handleInputChange}
              error={errors.businessLink}
              disableFocusRing
              errorClassName="mb-2"
            />

            <FloatingLabelInput
              name="fullName"
              label="Your full name"
              value={formData.fullName}
              onChange={handleInputChange}
              error={errors.fullName}
              disableFocusRing
              required
              errorClassName="mb-2"
            />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FloatingLabelInput
                name="email"
                type="email"
                label="Email address"
                autoComplete="email"
                value={formData.email}
                onChange={handleInputChange}
                error={errors.email}
                errorClassName="mb-2"
                disableFocusRing
                required
              />
              <FloatingLabelInput
                name="mobile"
                type="tel"
                label="Mobile number"
                autoComplete="tel"
                value={formData.mobile}
                onChange={handleInputChange}
                error={errors.mobile}
                errorClassName="mb-2"
                disableFocusRing
                required
              />
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FloatingLabelSelect
                label="Your role"
                name="role"
                value={formData.role}
                onValueChange={(value) => updateField("role", value)}
                options={ROLE_OPTIONS}
                error={errors.role}
                errorClassName="mb-2"
                disableFocusRing
                required
              />
              <FloatingLabelSelect
                label="Main goal"
                name="goal"
                value={formData.goal}
                onValueChange={(value) => updateField("goal", value)}
                options={MAIN_GOAL_OPTIONS}
                error={errors.goal}
                errorClassName="mb-2"
                disableFocusRing
                required
              />
            </div>
          </div>

          <CheckboxLabel
            id="termsAccepted"
            checked={formData.termsAccepted}
            onCheckedChange={(checked) => updateField("termsAccepted", checked)}
            error={errors.termsAccepted}
          >
            I confirm I&apos;m requesting Tummly for a restaurant or hospitality
            business and agree to be contacted about this request.
          </CheckboxLabel>

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

          <FieldErrorSlot error={errors.submit} />

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
      ) : null}

      {step === "otp" ? (
        <HeroTrialOtpStep
          email={formData.email.trim().toLowerCase()}
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

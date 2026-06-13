import { useState } from "react"
import type { ChangeEvent, FormEvent } from "react"
import { Link } from "react-router-dom"
import { isAxiosError } from "axios"

import heroFormAccent from "@/assets/svg/hero-form-accent.svg"
import {
  resendOtpRequest,
  submitTrialRequest,
  verifyOtpRequest,
} from "@/api/trialApi"
import {
  BUSINESS_CATEGORY_OPTIONS,
  LOCATION_COUNT_OPTIONS,
  MAIN_GOAL_OPTIONS,
  ROLE_OPTIONS,
} from "@/components/home/hero-trial-options"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { cn } from "@/lib/utils"
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

function maskEmail(email: string) {
  const [local, domain] = email.split("@")
  if (!local || !domain) return email

  const visible = local.slice(0, 1)
  return `${visible}${"•".repeat(Math.max(local.length - 1, 3))}@${domain}`
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

  if (
    data.mobile.trim() &&
    !/^[0-9+\-\s]{10,15}$/.test(data.mobile.trim())
  ) {
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
  const [formData, setFormData] = useState<TrialFormData>(initialFormData)
  const [errors, setErrors] = useState<FormErrors>({})
  const [step, setStep] = useState<"form" | "otp" | "success">("form")
  const [otpCode, setOtpCode] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [statusMessage, setStatusMessage] = useState<string | null>(null)

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
    setStatusMessage(null)

    const nextErrors = validateForm(formData)
    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors)
      return
    }

    setSubmitting(true)

    try {
      await submitTrialRequest({
        ...formData,
        businessName: formData.businessName.trim(),
        businessLink: formData.businessLink?.trim() || undefined,
        fullName: formData.fullName.trim(),
        email: formData.email.trim(),
        mobile: formData.mobile.trim(),
      })
      setStep("otp")
    } catch (error) {
      setErrors({
        submit: getApiErrorMessage(
          error,
          "We couldn't send your request. Please try again."
        ),
      })
    } finally {
      setSubmitting(false)
    }
  }

  const handleVerifyOtp = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setStatusMessage(null)

    if (otpCode.trim().length !== 6) {
      setErrors({ submit: "Enter the 6-digit code from your email." })
      return
    }

    setSubmitting(true)

    try {
      await verifyOtpRequest({
        email: formData.email.trim(),
        otpCode: otpCode.trim(),
      })
      setStep("success")
    } catch (error) {
      setErrors({
        submit: getApiErrorMessage(
          error,
          "That code didn't work. Check it and try again."
        ),
      })
    } finally {
      setSubmitting(false)
    }
  }

  const handleResendOtp = async () => {
    setStatusMessage(null)
    setSubmitting(true)

    try {
      await resendOtpRequest(formData.email.trim())
      setStatusMessage("A new code has been sent to your email.")
    } catch (error) {
      setErrors({
        submit: getApiErrorMessage(
          error,
          "We couldn't resend the code. Try again shortly."
        ),
      })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="relative w-full max-w-[615px] shrink-0 overflow-hidden px-5 pb-8 pt-12 shadow-[0_18px_50px_rgba(0,0,0,0.18)] sm:px-8 sm:pb-9 sm:pt-14 lg:px-[38px] lg:pb-[38px] lg:pt-[68px] lg:shadow-none">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 z-0 bg-white"
      />

      <div
        aria-hidden
        className="pointer-events-none absolute left-0 top-[-5px] z-[1] h-[210px] w-[367px] max-w-full"
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

      {step === "form" ? (
        <form
          onSubmit={handleSubmit}
          noValidate
          className="relative z-[2] flex w-full flex-col gap-7 sm:gap-8 lg:gap-[34px]"
        >
          <header className="flex flex-col gap-3 text-[#232323] lg:gap-3">
            <h2 className="m-0 text-[clamp(1.375rem,3vw,1.75rem)] font-bold leading-[normal] tracking-[-0.56px]">
              Request your guided trial
            </h2>
            <p className="m-0 text-sm font-medium leading-[21px] tracking-[-0.32px] sm:text-base">
              Tell us about your restaurant. We&apos;ll verify your email, review
              your setup needs and send the right next step for your location or
              group.
            </p>
          </header>

          <div className="flex flex-col gap-4">
            <FloatingLabelInput
              name="businessName"
              label="Restaurant / business name"
              value={formData.businessName}
              onChange={handleInputChange}
              error={errors.businessName}
              disableFocusRing
              required
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
            />

            <FloatingLabelInput
              name="fullName"
              label="Your full name"
              value={formData.fullName}
              onChange={handleInputChange}
              error={errors.fullName}
              disableFocusRing
              required
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
                disableFocusRing
                required
              />
              <FloatingLabelInput
                name="mobile"
                type="tel"
                label="Mobile number"
                optional
                autoComplete="tel"
                value={formData.mobile}
                onChange={handleInputChange}
                error={errors.mobile}
                disableFocusRing
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

          {errors.submit ? (
            <p className="text-sm text-destructive">{errors.submit}</p>
          ) : null}

          <div className="flex flex-col items-center gap-5 lg:gap-[22px]">
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
        <form
          onSubmit={handleVerifyOtp}
          noValidate
          className="relative z-[2] flex w-full flex-col gap-7 sm:gap-8 lg:gap-[34px]"
        >
          <header className="flex flex-col gap-3 text-[#232323] lg:gap-3">
            <h2 className="m-0 text-[clamp(1.375rem,3vw,1.75rem)] font-bold leading-[normal] tracking-[-0.56px]">
              Verify your email
            </h2>
            <p className="m-0 text-sm font-medium leading-[21px] tracking-[-0.32px] sm:text-base">
              We sent a 6-digit code to {maskEmail(formData.email.trim())}. Enter
              it below to finish your request.
            </p>
          </header>

          <FloatingLabelInput
            name="otpCode"
            label="Enter the 6-digit code"
            inputMode="numeric"
            autoComplete="one-time-code"
            value={otpCode}
            onChange={(event) => {
              setOtpCode(event.target.value.replace(/\D/g, "").slice(0, 6))
              setErrors({})
            }}
            error={errors.submit}
            disableFocusRing
            required
          />

          {statusMessage ? (
            <p className="text-sm font-medium text-[#14a247]">{statusMessage}</p>
          ) : null}

          <div className="flex flex-col items-center gap-5 lg:gap-[22px]">
            <Button
              type="submit"
              disabled={submitting}
              className="h-auto min-h-0 w-full rounded-[54px] border border-[rgba(20,162,71,0)] bg-[#14a247] px-[17px] py-[13px] text-base leading-5 text-white hover:bg-[#129641]"
            >
              {submitting ? "Verifying..." : "Verify email"}
            </Button>

            <Button
              type="button"
              variant="link"
              disabled={submitting}
              onClick={handleResendOtp}
              className={cn("text-sm font-medium text-[#14a74a]")}
            >
              Didn&apos;t get a code? Resend
            </Button>

            <Button
              type="button"
              variant="link"
              disabled={submitting}
              onClick={() => {
                setStep("form")
                setOtpCode("")
                setErrors({})
              }}
              className="text-sm font-medium text-[#232323]"
            >
              Back to form
            </Button>
          </div>
        </form>
      ) : null}

      {step === "success" ? (
        <div className="relative z-[2] flex w-full flex-col gap-5 text-[#232323] lg:gap-[22px]">
          <header className="flex flex-col gap-3">
            <h2 className="m-0 text-[clamp(1.375rem,3vw,1.75rem)] font-bold leading-[normal] tracking-[-0.56px]">
              Request received
            </h2>
            <p className="m-0 text-sm font-medium leading-[21px] tracking-[-0.32px] sm:text-base">
              Thanks, {formData.fullName.trim()}. We&apos;ve verified your email
              and will review your setup needs before sending the next step for{" "}
              {formData.businessName.trim()}.
            </p>
          </header>

          <p className="m-0 max-w-[313px] text-sm font-medium leading-5">
            For restaurants and hospitality operators only. No payment is taken on
            this form.
          </p>
        </div>
      ) : null}
    </div>
  )
}

export default HeroTrialForm

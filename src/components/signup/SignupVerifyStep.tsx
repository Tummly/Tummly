import { useId } from "react"
import type { FormEvent } from "react"
import { REGEXP_ONLY_DIGITS } from "input-otp"

import {
  OTP_LENGTH,
  type OtpFeedback,
} from "@/components/home/hero-trial-otp"
import { Button } from "@/components/ui/button"
import { Field, FieldError, FieldLabel } from "@/components/ui/field"
import {
  InputOTP,
  InputOTPGroup,
  InputOTPSlot,
} from "@/components/ui/input-otp"
import { cn } from "@/lib/utils"

const signupPrimaryButtonClassName =
  "h-[45px] min-h-[45px] w-full rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-[22px] text-white hover:bg-[#129641]"

type SignupVerifyStepProps = {
  email: string
  otpCode: string
  submitting: boolean
  feedback: OtpFeedback | null
  resendSecondsRemaining: number
  canResend: boolean
  onOtpChange: (value: string) => void
  onVerify: (event: FormEvent<HTMLFormElement>) => void
  onResend: () => void
  onUseDifferentEmail: () => void
}

function OtpFeedbackMessage({ feedback }: { feedback: OtpFeedback }) {
  return (
    <p
      role={feedback.kind === "error" ? "alert" : "status"}
      className={cn(
        "m-0 text-sm font-medium leading-5",
        feedback.kind === "error" ? "text-destructive" : "text-[#14a247]"
      )}
    >
      {feedback.message}
    </p>
  )
}

export function SignupVerifyStep({
  email,
  otpCode,
  submitting,
  feedback,
  resendSecondsRemaining,
  canResend,
  onOtpChange,
  onVerify,
  onResend,
  onUseDifferentEmail,
}: SignupVerifyStepProps) {
  const fieldId = useId()
  const hasError = feedback?.kind === "error"
  const resendDisabled = submitting || !canResend

  return (
    <div className="flex w-full max-w-[434px] flex-col items-center gap-[30px]">
      <div className="flex w-full flex-col items-center gap-10">
        <div className="flex flex-col gap-3 text-center text-[#141414]">
          <h1 className="m-0 font-serif text-[clamp(1.75rem,4vw,2.25rem)] font-medium leading-normal">
            Verify your email
          </h1>
          <p className="m-0 text-base leading-[22px]">
            We&apos;ve sent a verification code to {email}. Enter the code to
            continue setting up your Tummly account.
          </p>
        </div>

        <form
          onSubmit={onVerify}
          noValidate
          className="flex w-full flex-col gap-6"
        >
          <Field data-invalid={hasError ? true : undefined}>
            <FieldLabel
              htmlFor={fieldId}
              className="text-sm font-semibold leading-5 text-[#232323]"
            >
              Enter the 6 digit code
            </FieldLabel>

            <InputOTP
              id={fieldId}
              autoFocus
              maxLength={OTP_LENGTH}
              pattern={REGEXP_ONLY_DIGITS}
              inputMode="numeric"
              autoComplete="one-time-code"
              value={otpCode}
              disabled={submitting}
              onChange={(value) => onOtpChange(value)}
              containerClassName="w-full justify-center"
            >
              <InputOTPGroup>
                {Array.from({ length: OTP_LENGTH }, (_, index) => (
                  <InputOTPSlot
                    key={index}
                    index={index}
                    aria-invalid={hasError || undefined}
                  />
                ))}
              </InputOTPGroup>
            </InputOTP>

            {hasError ? (
              <FieldError className="text-sm font-medium leading-5">
                {feedback.message}
              </FieldError>
            ) : null}
          </Field>

          {feedback?.kind === "info" ? (
            <OtpFeedbackMessage feedback={feedback} />
          ) : null}

          <Button
            type="submit"
            disabled={submitting || otpCode.length !== OTP_LENGTH}
            className={signupPrimaryButtonClassName}
          >
            {submitting ? "Please wait..." : "Verify"}
          </Button>
        </form>

        <div className="h-px w-full bg-[#d2d2d2]" />

        <div className="flex flex-col items-center gap-4">
          <Button
            type="button"
            variant="link"
            size="link-sm"
            disabled={resendDisabled}
            onClick={onResend}
            className="font-medium text-[#141414] underline underline-offset-2"
          >
            {canResend
              ? "Resend verification email"
              : `Resend in ${resendSecondsRemaining}s`}
          </Button>

          <Button
            type="button"
            variant="link"
            size="link-sm"
            disabled={submitting}
            onClick={onUseDifferentEmail}
            className="font-medium text-[#141414]"
          >
            Use a different email
          </Button>
        </div>
      </div>
    </div>
  )
}

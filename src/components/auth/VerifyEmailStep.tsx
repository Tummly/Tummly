import { useId } from "react"
import type { FormEvent } from "react"
import { REGEXP_ONLY_DIGITS } from "input-otp"

import { AuthFormHeader } from "@/components/auth/AuthFormHeader"
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

type VerifyEmailStepProps = {
  email: string
  otpCode: string
  submitting: boolean
  feedback: OtpFeedback | null
  resendSecondsRemaining: number
  canResend: boolean
  onOtpChange: (value: string) => void
  onVerify: (event: FormEvent<HTMLFormElement>) => void
  onResend: () => void
  onUseDifferentAccount: () => void
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

const authPrimaryButtonClassName =
  "h-[45px] min-h-[45px] w-full rounded-[4px] bg-[#14a74a] px-[18px] py-3 text-sm font-medium leading-[22px] text-white hover:bg-[#129641]"

export function VerifyEmailStep({
  email,
  otpCode,
  submitting,
  feedback,
  resendSecondsRemaining,
  canResend,
  onOtpChange,
  onVerify,
  onResend,
  onUseDifferentAccount,
}: VerifyEmailStepProps) {
  const fieldId = useId()
  const hasError = feedback?.kind === "error"
  const resendDisabled = submitting || !canResend

  return (
    <div className="flex w-full flex-col gap-10">
      <AuthFormHeader
        title="Verify your email"
        description={`Check your inbox for the verification email we sent to ${email}.`}
      />

      <div className="flex flex-col gap-9">
        <Button
          type="button"
          disabled={resendDisabled}
          onClick={onResend}
          className={authPrimaryButtonClassName}
        >
          {submitting
            ? "Please wait..."
            : canResend
              ? "Resend verification email"
              : `Resend in ${resendSecondsRemaining}s`}
        </Button>

        <div className="h-px w-full bg-[#d2d2d2]" />

        <Button
          type="button"
          variant="link"
          size="link-sm"
          disabled={submitting}
          onClick={onUseDifferentAccount}
          className="self-start font-medium text-primary underline underline-offset-2"
        >
          Use a different account
        </Button>
      </div>

      <form
        onSubmit={onVerify}
        noValidate
        className="flex w-full flex-col gap-9"
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
            containerClassName="w-full"
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
          className={authPrimaryButtonClassName}
        >
          {submitting ? "Please wait..." : "Verify"}
        </Button>
      </form>
    </div>
  )
}

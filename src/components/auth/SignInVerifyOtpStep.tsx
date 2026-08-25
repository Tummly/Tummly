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

const cardShadow =
  "shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

type SignInVerifyOtpStepProps = {
  destination: string
  otpCode: string
  submitting: boolean
  feedback: OtpFeedback | null
  resendSecondsRemaining: number
  canResend: boolean
  onOtpChange: (value: string) => void
  onVerify: (event: FormEvent<HTMLFormElement>) => void
  onResend: () => void
  onChooseSignInMethod?: () => void
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

export function SignInVerifyOtpStep({
  destination,
  otpCode,
  submitting,
  feedback,
  resendSecondsRemaining,
  canResend,
  onOtpChange,
  onVerify,
  onResend,
  onChooseSignInMethod,
}: SignInVerifyOtpStepProps) {
  const fieldId = useId()
  const hasError = feedback?.kind === "error"

  return (
    <form
      onSubmit={onVerify}
      noValidate
      className={`flex w-full max-w-[490px] shrink-0 flex-col gap-6 rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)] sm:gap-7 lg:gap-16 ${cardShadow}`}
    >
      <header className="flex flex-col gap-4 text-[#232323]">
        <h1 className="m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px]">
          Verify it&apos;s you
        </h1>
        <p className="m-0 text-sm leading-normal">
          We sent a 6 digit code to {destination}.
          <br />
          Enter it below to continue.
        </p>
      </header>

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
        className="h-auto min-h-0 w-full rounded-[4px] bg-[#14a74a] px-[17px] py-[15px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
      >
        {submitting ? "Please wait..." : "Verify"}
      </Button>

      <div className="flex flex-col gap-3.5">
        <p className="m-0 flex flex-wrap items-center gap-2.5 text-sm font-medium tracking-[0.4px] text-[#232323]">
          <span>Didn&apos;t get a code?</span>
          {canResend ? (
            <Button
              type="button"
              variant="link"
              size="link-sm"
              disabled={submitting}
              onClick={onResend}
              className="font-medium text-primary underline underline-offset-2"
            >
              Resend code
            </Button>
          ) : (
            <span className="text-[#232323]">
              Resend code in {resendSecondsRemaining} seconds
            </span>
          )}
        </p>

        {onChooseSignInMethod != null ? (
          <Button
            type="button"
            variant="link"
            size="link-sm"
            disabled={submitting}
            onClick={onChooseSignInMethod}
            className="self-start font-medium text-primary underline underline-offset-2"
          >
            Use a different sign-in method
          </Button>
        ) : null}
      </div>
    </form>
  )
}

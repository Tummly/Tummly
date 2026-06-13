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

type HeroTrialOtpStepProps = {
  email: string
  otpCode: string
  submitting: boolean
  feedback: OtpFeedback | null
  resendSecondsRemaining: number
  canResend: boolean
  onOtpChange: (value: string) => void
  onVerify: (event: FormEvent<HTMLFormElement>) => void
  onResend: () => void
  onChangeEmail: () => void
}

function OtpFeedbackMessage({ feedback }: { feedback: OtpFeedback | null }) {
  if (!feedback) return null

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

function HeroTrialOtpStep({
  email,
  otpCode,
  submitting,
  feedback,
  resendSecondsRemaining,
  canResend,
  onOtpChange,
  onVerify,
  onResend,
  onChangeEmail,
}: HeroTrialOtpStepProps) {
  const fieldId = useId()
  const hasError = feedback?.kind === "error"
  const isAlreadyVerified = feedback?.code === "already_verified"

  return (
    <form
      onSubmit={onVerify}
      noValidate
      className="relative z-[2] flex min-h-full w-full flex-col"
    >
      <div className="flex flex-col gap-7 sm:gap-8 lg:gap-[34px]">
      <header className="flex flex-col gap-3 text-[#232323] lg:gap-3">
        <h2 className="m-0 text-[clamp(1.375rem,3vw,1.75rem)] font-bold leading-[normal] tracking-[-0.56px]">
          Check your email
        </h2>
        <p className="m-0 text-sm font-medium leading-[21px] tracking-[-0.32px] sm:text-base">
          We&apos;ve sent a 6 digit verification code to
          <br />
          <a
            href={`mailto:${email}`}
            className="font-medium text-[#232323] underline decoration-solid underline-offset-2"
          >
            {email}
          </a>
          <br />
          <br />
          Enter the code below to verify your email and continue your guided
          trial request.
        </p>
      </header>

      {isAlreadyVerified ? (
        <OtpFeedbackMessage feedback={feedback} />
      ) : (
        <Field data-invalid={hasError ? true : undefined}>
          <FieldLabel
            htmlFor={fieldId}
            className="text-sm font-semibold leading-5 text-[#232323]"
          >
            Verification code
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
      )}

      {feedback?.kind === "info" && !isAlreadyVerified ? (
        <OtpFeedbackMessage feedback={feedback} />
      ) : null}
      </div>

      <div className="mt-auto flex flex-col items-stretch gap-5 pt-7 sm:pt-8 lg:gap-[22px] lg:pt-[34px]">
        {!isAlreadyVerified ? (
          <Button
            type="submit"
            disabled={submitting || otpCode.length !== OTP_LENGTH}
            className="h-auto min-h-0 w-full rounded-[54px] border border-[rgba(20,162,71,0)] bg-[#14a247] px-5 py-[9px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
          >
            {submitting ? "Verifying..." : "Verify email"}
          </Button>
        ) : null}

        <div className="flex flex-col gap-3.5">
          {!isAlreadyVerified ? (
            <div className="flex flex-wrap items-center gap-6">
              {canResend ? (
                <Button
                  type="button"
                  variant="link"
                  disabled={submitting}
                  onClick={onResend}
                  className="h-auto min-h-0 p-0 text-sm font-medium text-[#141414] underline underline-offset-2"
                >
                  Resend code
                </Button>
              ) : (
                <p className="m-0 text-sm font-medium leading-normal text-[#141414]">
                  Resend code in {resendSecondsRemaining} seconds
                </p>
              )}

              <Button
                type="button"
                variant="link"
                disabled={submitting}
                onClick={onChangeEmail}
                className="h-auto min-h-0 p-0 text-sm font-medium text-[#141414] underline underline-offset-2"
              >
                Change email address
              </Button>
            </div>
          ) : (
            <Button
              type="button"
              variant="link"
              disabled={submitting}
              onClick={onChangeEmail}
              className="h-auto min-h-0 p-0 text-sm font-medium text-[#141414] underline underline-offset-2"
            >
              Change email address
            </Button>
          )}

          {!isAlreadyVerified ? (
            <p className="m-0 text-sm font-medium leading-5 text-[#232323]">
              Didn&apos;t receive it? Check your spam folder or resend the code.
            </p>
          ) : null}
        </div>
      </div>
    </form>
  )
}

export default HeroTrialOtpStep

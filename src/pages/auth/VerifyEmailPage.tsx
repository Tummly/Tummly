import { useEffect, useState } from "react"
import type { FormEvent } from "react"
import { isAxiosError } from "axios"
import { useLocation, useNavigate, useSearchParams } from "react-router-dom"

import { resendOtpRequest, verifyOtpRequest } from "@/api/trialApi"
import { AuthShell } from "@/components/auth/AuthShell"
import { VerifyEmailStep } from "@/components/auth/VerifyEmailStep"
import HeroTrialSuccessStep from "@/components/home/HeroTrialSuccessStep"
import {
  isAlreadyVerifiedFeedback,
  mapResendApiMessage,
  mapVerifyApiMessage,
  MAX_VERIFY_ATTEMPTS,
  OTP_MESSAGES,
  RESEND_COOLDOWN_SECONDS,
  type OtpFeedback,
} from "@/components/home/hero-trial-otp"
import { useCountdown } from "@/hooks/use-countdown"
import { readVerifyEmail } from "@/lib/verifyEmailFlow"

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const location = useLocation()
  const navigate = useNavigate()
  const email = readVerifyEmail(
    searchParams,
    (location.state as { email?: string } | null)?.email
  )

  const [step, setStep] = useState<"otp" | "success">("otp")
  const [confirmationEmailSent, setConfirmationEmailSent] = useState(true)
  const [otpCode, setOtpCode] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [otpFeedback, setOtpFeedback] = useState<OtpFeedback | null>(null)
  const [verifyAttempts, setVerifyAttempts] = useState(0)
  const {
    secondsRemaining: resendSecondsRemaining,
    isComplete: canResend,
    restart: restartResendTimer,
  } = useCountdown(RESEND_COOLDOWN_SECONDS, step === "otp" && Boolean(email))

  useEffect(() => {
    if (!email) {
      navigate("/", { replace: true })
    }
  }, [email, navigate])

  const handleVerifyOtp = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!email) return

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
      const result = await verifyOtpRequest({
        email: email.trim().toLowerCase(),
        otpCode: otpCode.trim(),
      })
      setConfirmationEmailSent(result.confirmationEmailSent !== false)
      setStep("success")
      setOtpFeedback(null)
    } catch (error) {
      const message = getApiErrorMessage(error, OTP_MESSAGES.invalid)
      const feedback = mapVerifyApiMessage(message)

      if (isAlreadyVerifiedFeedback(feedback)) {
        setConfirmationEmailSent(true)
        setStep("success")
        setOtpFeedback(null)
        return
      }

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
    if (!email || !canResend || submitting) return

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

  const handleOtpChange = (value: string) => {
    setOtpCode(value)
    if (otpFeedback?.kind === "error") {
      setOtpFeedback(null)
    }
  }

  const goHome = () => {
    navigate("/")
  }

  if (!email) {
    return null
  }

  return (
    <AuthShell>
      {step === "otp" ? (
        <VerifyEmailStep
          email={email}
          otpCode={otpCode}
          submitting={submitting}
          feedback={otpFeedback}
          resendSecondsRemaining={resendSecondsRemaining}
          canResend={canResend}
          onOtpChange={handleOtpChange}
          onVerify={handleVerifyOtp}
          onResend={() => {
            void handleResendOtp()
          }}
          onUseDifferentAccount={goHome}
        />
      ) : (
        <HeroTrialSuccessStep
          confirmationEmailSent={confirmationEmailSent}
          onReturnToTummly={goHome}
          onSubmitAgain={goHome}
        />
      )}
    </AuthShell>
  )
}

export default VerifyEmailPage

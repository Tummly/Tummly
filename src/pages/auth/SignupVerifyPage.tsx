import { useEffect, useState } from "react"
import type { FormEvent } from "react"
import { isAxiosError } from "axios"
import { useNavigate, useSearchParams } from "react-router-dom"

import { resendSignupOtp, verifySignupOtp } from "@/api/signupApi"
import {
  mapResendApiMessage,
  mapVerifyApiMessage,
  MAX_VERIFY_ATTEMPTS,
  OTP_MESSAGES,
  RESEND_COOLDOWN_SECONDS,
  type OtpFeedback,
} from "@/components/home/hero-trial-otp"
import { SignupModalShell } from "@/components/signup/SignupModalShell"
import { SignupVerifyStep } from "@/components/signup/SignupVerifyStep"
import { useCountdown } from "@/hooks/use-countdown"
import {
  clearSignupSessionToken,
  readSignupSessionToken,
} from "@/lib/signupSession"

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

function readVerifyEmail(searchParams: URLSearchParams) {
  const email = searchParams.get("email")?.trim() ?? ""
  return email
}

function SignupVerifyPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const email = readVerifyEmail(searchParams)
  const sessionToken = readSignupSessionToken()

  const [otpCode, setOtpCode] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [otpFeedback, setOtpFeedback] = useState<OtpFeedback | null>(null)
  const [verifyAttempts, setVerifyAttempts] = useState(0)
  const {
    secondsRemaining: resendSecondsRemaining,
    isComplete: canResend,
    restart: restartResendTimer,
  } = useCountdown(RESEND_COOLDOWN_SECONDS, Boolean(email && sessionToken))

  useEffect(() => {
    if (!email || !sessionToken) {
      navigate("/signup", { replace: true })
    }
  }, [email, sessionToken, navigate])

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
      await verifySignupOtp({
        email: email.trim().toLowerCase(),
        otpCode: otpCode.trim(),
      })
      navigate("/signup/onboarding")
    } catch (error) {
      const message = getApiErrorMessage(error, OTP_MESSAGES.invalid)
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
    if (!email || !canResend || submitting) return

    setOtpFeedback(null)
    setSubmitting(true)

    try {
      await resendSignupOtp(email.trim().toLowerCase())
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

  const handleUseDifferentEmail = () => {
    clearSignupSessionToken()
    navigate("/signup")
  }

  if (!email || !sessionToken) {
    return null
  }

  return (
    <SignupModalShell>
      <SignupVerifyStep
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
        onUseDifferentEmail={handleUseDifferentEmail}
      />
    </SignupModalShell>
  )
}

export default SignupVerifyPage

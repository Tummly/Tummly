import { useEffect, useState } from "react"
import type { FormEvent } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"

import { resendSignupOtp, verifySignupOtp } from "@/api/signupApi"
import {
  OTP_MESSAGES,
  RESEND_COOLDOWN_SECONDS,
} from "@/components/home/hero-trial-otp"
import { SignupModalShell } from "@/components/signup/SignupModalShell"
import { SignupVerifyStep } from "@/components/signup/SignupVerifyStep"
import { useCountdown } from "@/hooks/use-countdown"
import {
  mapSignupOtpError,
  type SignupOtpFeedback,
} from "@/lib/signupOtpFeedback"
import {
  clearSignupSessionToken,
  saveSignupSessionToken,
} from "@/lib/signupSession"

function readVerifyEmail(searchParams: URLSearchParams) {
  const email = searchParams.get("email")?.trim() ?? ""
  return email
}

/** Only `email` is needed. The token comes back from a correct code. */
function SignupVerifyPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const email = readVerifyEmail(searchParams)

  const [otpCode, setOtpCode] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [otpFeedback, setOtpFeedback] = useState<SignupOtpFeedback | null>(
    null
  )
  const {
    secondsRemaining: resendSecondsRemaining,
    isComplete: canResend,
    restart: restartResendTimer,
  } = useCountdown(RESEND_COOLDOWN_SECONDS, Boolean(email))

  useEffect(() => {
    if (!email) {
      navigate("/signup", { replace: true })
    }
  }, [email, navigate])

  const handleVerifyOtp = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!email) return

    setOtpFeedback(null)

    if (otpCode.trim().length !== 6) {
      setOtpFeedback({
        kind: "error",
        message: OTP_MESSAGES.incomplete,
        showRecoveryLinks: false,
        retryAtLabel: null,
      })
      return
    }

    setSubmitting(true)

    try {
      const session = await verifySignupOtp({
        email: email.trim().toLowerCase(),
        otpCode: otpCode.trim(),
      })
      saveSignupSessionToken(session.sessionToken)
      navigate("/signup/onboarding")
    } catch (error) {
      setOtpFeedback(mapSignupOtpError(error))
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
      restartResendTimer(RESEND_COOLDOWN_SECONDS)
      setOtpFeedback({
        kind: "info",
        message: OTP_MESSAGES.code_resent,
        showRecoveryLinks: false,
        retryAtLabel: null,
      })
    } catch (error) {
      setOtpFeedback(mapSignupOtpError(error))
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

  if (!email) {
    return null
  }

  return (
    <SignupModalShell>
      <SignupVerifyStep
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

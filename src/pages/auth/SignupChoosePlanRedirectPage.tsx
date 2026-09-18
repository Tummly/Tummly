import { useEffect, useState } from "react"
import { isAxiosError } from "axios"
import { useNavigate } from "react-router-dom"

import { getSignupSession } from "@/api/signupApi"
import { SetupAccountStatus } from "@/components/auth/SetupAccountShell"
import { readSignupSessionToken } from "@/lib/signupSession"

const PROVISIONING_STATUSES = new Set([
  "Provisioning",
  "AwaitingPayment",
  "OnboardingComplete",
])

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

/**
 * Legacy `/signup/choose-plan` path — redirects by session status.
 * Choose plan UI is removed from the product path (always Pilot after onboarding).
 */
function SignupChoosePlanRedirectPage() {
  const navigate = useNavigate()
  const sessionToken = readSignupSessionToken()
  const [gateError, setGateError] = useState<string | null>(null)

  useEffect(() => {
    if (!sessionToken) {
      navigate("/signup", { replace: true })
      return
    }

    let cancelled = false

    void (async () => {
      try {
        const session = await getSignupSession(sessionToken)

        if (cancelled) return

        if (session.status === "Verified") {
          navigate("/signup/onboarding", { replace: true })
          return
        }

        if (PROVISIONING_STATUSES.has(session.status)) {
          navigate("/signup/provisioning", { replace: true })
          return
        }

        if (session.status === "Complete") {
          navigate("/login?setup=complete", { replace: true })
          return
        }

        navigate("/signup", { replace: true })
      } catch (error) {
        if (cancelled) return
        setGateError(
          getApiErrorMessage(
            error,
            "We couldn't load your signup session. Please start again."
          )
        )
      }
    })()

    return () => {
      cancelled = true
    }
  }, [sessionToken, navigate])

  if (!sessionToken) {
    return null
  }

  if (gateError) {
    return (
      <SetupAccountStatus
        tone="error"
        title="Unable to continue signup"
        message={gateError}
      />
    )
  }

  return <SetupAccountStatus title="Loading your signup" />
}

export default SignupChoosePlanRedirectPage

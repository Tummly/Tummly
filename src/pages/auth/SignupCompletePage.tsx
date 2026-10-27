import { useCallback, useEffect, useRef, useState } from "react"
import { isAxiosError } from "axios"
import { useNavigate } from "react-router-dom"

import { getBillingCreditsPage } from "@/api/billingCreditsApi"
import {
  getSignupProvisioningStatus,
  signupSessionLogin,
  SignupSessionLoginError,
} from "@/api/signupApi"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import { Button } from "@/components/ui/button"
import {
  clearSignupSessionToken,
  isSessionLoginExpiredError,
  readSignupSessionToken,
  redirectIfSignupSessionExpired,
  SIGNUP_SETUP_LOGIN_PATH,
} from "@/lib/signupSession"
import { buildPilotSetupConfirmation } from "@/lib/signupPilotConfirmation"
import { persistSignupLoginSession } from "@/pages/utils/authHelpers"

const POLL_INTERVAL_MS = 2000
const SESSION_LOGIN_MAX_MS = 120_000
const SESSION_LOGIN_BACKOFF_START_MS = 1500
const SESSION_LOGIN_BACKOFF_MAX_MS = 8000

const LOADING_COPY = "Finishing your setup…"
const LOCATIONS_PATH = "/signup/locations"

/** Pilot start and end dates for the setup confirmation. Null for any other plan. */
async function loadPilotConfirmation(): Promise<string | null> {
  try {
    const page = await getBillingCreditsPage()
    return buildPilotSetupConfirmation(page.planSubscription)
  } catch {
    // The dates are extra. Never block setup on them.
    return null
  }
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

function sleep(ms: number) {
  return new Promise<void>((resolve) => {
    window.setTimeout(resolve, ms)
  })
}

function SignupCompletePage() {
  const navigate = useNavigate()
  // Read once. The token is cleared after sign-in and the page must stay mounted
  // to show the Pilot confirmation.
  const [sessionToken] = useState(() => readSignupSessionToken())

  const [pilotConfirmation, setPilotConfirmation] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const [paymentIncomplete, setPaymentIncomplete] = useState(false)

  const runIdRef = useRef(0)

  const goToLogin = useCallback(() => {
    clearSignupSessionToken()
    navigate(SIGNUP_SETUP_LOGIN_PATH, { replace: true })
  }, [navigate])

  useEffect(() => {
    if (!sessionToken) {
      navigate(SIGNUP_SETUP_LOGIN_PATH, { replace: true })
      return
    }

    const runId = ++runIdRef.current
    let cancelled = false
    const isCurrent = () => !cancelled && runId === runIdRef.current

    const pollUntilReady = async (): Promise<"ok" | "login" | "redirected"> => {
      const startedAt = Date.now()
      while (isCurrent()) {
        const status = await getSignupProvisioningStatus(sessionToken)

        if (status.status === "Abandoned") {
          return "login"
        }

        if (status.status === "PaymentFailed") {
          // Checkout never opened: the provisioning page offers Retry.
          navigate("/signup/provisioning", { replace: true })
          return "redirected"
        }

        if (status.ready || status.status === "Complete") {
          return "ok"
        }

        if (
          status.status === "Provisioning"
          || status.status === "AwaitingPayment"
          || status.status === "OnboardingComplete"
        ) {
          // Revolut left without paying: stop polling and offer checkout again.
          if (Date.now() - startedAt >= SESSION_LOGIN_MAX_MS) {
            setPaymentIncomplete(true)
            throw new Error(
              "We have not received your payment yet. Complete payment to continue."
            )
          }
          await sleep(POLL_INTERVAL_MS)
          continue
        }

        if (
          status.status === "Verified"
          || status.status === "EmailPending"
        ) {
          return "login"
        }

        throw new Error(
          `Setup stalled (${status.status || "unknown"}). Please try again.`
        )
      }

      return "login"
    }

    const exchangeSession = async (): Promise<"ok" | "login" | "retry"> => {
      const startedAt = Date.now()
      let delayMs = SESSION_LOGIN_BACKOFF_START_MS

      while (isCurrent()) {
        try {
          const session = await signupSessionLogin(sessionToken)
          if (!isCurrent()) {
            return "login"
          }
          persistSignupLoginSession(session)
          clearSignupSessionToken()
          const confirmation = await loadPilotConfirmation()
          if (!isCurrent()) {
            return "ok"
          }
          if (confirmation) {
            setPilotConfirmation(confirmation)
            return "ok"
          }
          navigate(LOCATIONS_PATH, { replace: true })
          return "ok"
        } catch (caught) {
          if (!isCurrent()) {
            return "login"
          }

          // Used or expired link: the account exists, so the user signs in
          // (never the "enter your email" step).
          if (
            (caught instanceof SignupSessionLoginError
              && caught.code === "session_login_used")
            || isSessionLoginExpiredError(caught)
          ) {
            return "login"
          }

          if (
            caught instanceof SignupSessionLoginError
            && caught.code === "payment_pending"
          ) {
            if (Date.now() - startedAt >= SESSION_LOGIN_MAX_MS) {
              return "retry"
            }
            await sleep(delayMs)
            delayMs = Math.min(delayMs * 1.5, SESSION_LOGIN_BACKOFF_MAX_MS)
            continue
          }

          throw caught
        }
      }

      return "login"
    }

    void (async () => {
      setError(null)

      try {
        const gate = await pollUntilReady()
        if (!isCurrent()) {
          return
        }
        if (gate === "redirected") {
          return
        }
        if (gate === "login") {
          goToLogin()
          return
        }

        const outcome = await exchangeSession()
        if (!isCurrent()) {
          return
        }
        if (outcome === "login") {
          goToLogin()
          return
        }
        if (outcome === "retry") {
          setError(
            "Payment is still confirming. You can retry in a moment, or sign in once it completes."
          )
        }
      } catch (caught) {
        if (!isCurrent()) {
          return
        }
        if (redirectIfSignupSessionExpired(caught, navigate)) {
          return
        }
        setError(
          getApiErrorMessage(
            caught,
            "We couldn't finish setting up your account. Please try again."
          )
        )
      }
    })()

    return () => {
      cancelled = true
      if (runIdRef.current === runId) {
        runIdRef.current += 1
      }
    }
  }, [attempt, goToLogin, navigate, sessionToken])

  const handleRetry = () => {
    if (isBusy || !sessionToken) {
      return
    }
    setIsBusy(true)
    setError(null)
    setPaymentIncomplete(false)
    runIdRef.current += 1
    setAttempt((current) => current + 1)
    setIsBusy(false)
  }

  if (!sessionToken) {
    return null
  }

  if (pilotConfirmation) {
    return (
      <GuestLoopShell
        contentAlign="center"
        contentMaxWidthClassName="max-w-none"
        fullBleed
        showBackButton={false}
      >
        <div className="mx-auto flex w-full max-w-[473px] flex-col gap-6">
          <p
            className="m-0 text-center font-sans text-lg font-normal leading-6 text-[#141414]"
            role="status"
          >
            {pilotConfirmation}
          </p>
          <Button
            type="button"
            onClick={() => navigate(LOCATIONS_PATH, { replace: true })}
            className="h-[45px] w-full rounded-[4px] bg-[#14a74a] text-sm font-medium text-white shadow-none hover:bg-[#14a74a]/90"
          >
            Continue
          </Button>
        </div>
      </GuestLoopShell>
    )
  }

  return (
    <GuestLoopShell
      contentAlign="center"
      contentMaxWidthClassName="max-w-none"
      fullBleed
      showBackButton={false}
    >
      {error ? (
        <div className="mx-auto flex w-full max-w-[473px] flex-col gap-6">
          <p className="m-0 text-center font-sans text-lg font-normal leading-6 text-[#141414]">
            We couldn&apos;t finish signing you in.
          </p>
          <div
            className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-center text-sm text-red-700"
            role="alert"
          >
            {error}
          </div>
          <Button
            type="button"
            disabled={isBusy}
            onClick={handleRetry}
            className="h-[45px] w-full rounded-[4px] bg-[#14a74a] text-sm font-medium text-white shadow-none hover:bg-[#14a74a]/90"
          >
            {isBusy ? "Retrying…" : "Retry"}
          </Button>
          {paymentIncomplete ? (
            <Button
              type="button"
              variant="outline"
              disabled={isBusy}
              onClick={() => navigate("/signup/provisioning", { replace: true })}
              className="h-[45px] w-full rounded-[4px] text-sm font-medium shadow-none"
            >
              Complete payment
            </Button>
          ) : null}
          <Button
            type="button"
            variant="outline"
            disabled={isBusy}
            onClick={goToLogin}
            className="h-[45px] w-full rounded-[4px] text-sm font-medium shadow-none"
          >
            Go to sign in
          </Button>
        </div>
      ) : (
        <p
          className="m-0 text-center font-sans text-lg font-normal leading-6 text-[#141414]"
          role="status"
          aria-live="polite"
        >
          {LOADING_COPY}
        </p>
      )}
    </GuestLoopShell>
  )
}

export default SignupCompletePage

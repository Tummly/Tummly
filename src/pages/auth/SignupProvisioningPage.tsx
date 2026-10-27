import { useCallback, useEffect, useRef, useState } from "react"
import { isAxiosError } from "axios"
import { useNavigate } from "react-router-dom"

import {
  getSignupProvisioningStatus,
  retrySignupProvision,
  signupSessionLogin,
} from "@/api/signupApi"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import { Button } from "@/components/ui/button"
import {
  clearSignupSessionToken,
  readSignupSessionToken,
  redirectIfSignupSessionExpired,
} from "@/lib/signupSession"
import { persistSignupLoginSession } from "@/pages/utils/authHelpers"

const POLL_INTERVAL_MS = 2000

/** Figma provisioning loading copy (`4974:23056`). */
const PROVISIONING_LOADING_COPY = "Setting up your Tummly workspace…"

async function kickPilotProvision(sessionToken: string) {
  await retrySignupProvision(sessionToken)
}

/** Account exists (no paid plan yet) when checkout fails to open. */
const PAYMENT_FAILED_HEADING =
  "Your account is created, but we couldn't take payment."

const PAYMENT_FAILED_CONTINUE_FREE = "Continue on Free for now"

const PAYMENT_FAILED_MESSAGE =
  "Your paid plan is not active yet because the payment page didn't open. Select Retry to open it again."

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

function SignupProvisioningPage() {
  const navigate = useNavigate()
  const sessionToken = readSignupSessionToken()

  const [provisioningError, setProvisioningError] = useState<string | null>(
    null
  )
  const [isBusy, setIsBusy] = useState(false)
  const [provisioningAttempt, setProvisioningAttempt] = useState(0)

  const provisioningRunId = useRef(0)
  const isPaymentFailed = provisioningError === PAYMENT_FAILED_MESSAGE

  const goToComplete = useCallback(() => {
    navigate("/signup/complete", { replace: true })
  }, [navigate])

  const goToOnboarding = useCallback(() => {
    navigate("/signup/onboarding", { replace: true })
  }, [navigate])

  useEffect(() => {
    if (!sessionToken) {
      navigate("/signup", { replace: true })
      return
    }

    const runId = ++provisioningRunId.current
    let cancelled = false

    const isCurrent = () => !cancelled && runId === provisioningRunId.current

    const pollUntilProvisionable = async (): Promise<"ok" | "redirected"> => {
      while (isCurrent()) {
        const status = await getSignupProvisioningStatus(sessionToken)

        if (status.status === "Abandoned") {
          throw new Error(
            "This signup session is no longer active. Please start again."
          )
        }

        if (status.status === "OnboardingComplete") {
          // Legacy / stalled sessions: re-kick Pilot provision.
          await kickPilotProvision(sessionToken)
          await sleep(POLL_INTERVAL_MS)
          continue
        }

        if (status.status === "Verified") {
          navigate("/signup/onboarding", { replace: true })
          return "redirected"
        }

        if (status.status === "EmailPending") {
          navigate("/signup", { replace: true })
          return "redirected"
        }

        if (status.status === "PaymentFailed") {
          throw new Error(PAYMENT_FAILED_MESSAGE)
        }

        if (status.status === "AwaitingPayment") {
          if (
            typeof status.paymentRedirectUrl === "string"
            && status.paymentRedirectUrl.length > 0
          ) {
            window.location.assign(status.paymentRedirectUrl)
            return "redirected"
          }
          // Pay session abandoned or TTL — continue to Free ready path.
          if (status.ready) {
            return "ok"
          }
          navigate("/signup/onboarding", { replace: true })
          return "redirected"
        }

        if (
          status.status === "Provisioning"
          || status.status === "Complete"
          || status.ready
        ) {
          return "ok"
        }

        throw new Error(
          `Unexpected signup status (${status.status || "empty"}). Please try again.`
        )
      }

      return "redirected"
    }

    const waitUntilReady = async () => {
      while (isCurrent()) {
        const status = await getSignupProvisioningStatus(sessionToken)

        if (status.status === "PaymentFailed") {
          throw new Error(PAYMENT_FAILED_MESSAGE)
        }

        if (status.ready || status.status === "Complete") {
          if (
            typeof status.paymentRedirectUrl === "string"
            && status.paymentRedirectUrl.length > 0
          ) {
            window.location.assign(status.paymentRedirectUrl)
            return
          }
          return
        }

        if (status.status === "Provisioning") {
          await sleep(POLL_INTERVAL_MS)
          continue
        }

        if (status.status === "OnboardingComplete") {
          await kickPilotProvision(sessionToken)
          await sleep(POLL_INTERVAL_MS)
          continue
        }

        throw new Error(
          `Setup stalled (${status.status || "unknown"}). Please try again.`
        )
      }

      throw new Error("Provisioning cancelled.")
    }

    void (async () => {
      setProvisioningError(null)

      try {
        const gate = await pollUntilProvisionable()
        if (!isCurrent() || gate === "redirected") return

        await waitUntilReady()
        if (!isCurrent()) return

        goToComplete()
      } catch (error) {
        if (!isCurrent()) return
        if (redirectIfSignupSessionExpired(error, navigate)) {
          return
        }
        setProvisioningError(
          getApiErrorMessage(
            error,
            "We couldn't finish setting up your account. Please try again."
          )
        )
      }
    })()

    return () => {
      cancelled = true
      if (provisioningRunId.current === runId) {
        provisioningRunId.current += 1
      }
    }
  }, [goToComplete, navigate, provisioningAttempt, sessionToken])

  const handleRetry = () => {
    if (isBusy || !sessionToken) return

    void (async () => {
      setIsBusy(true)
      setProvisioningError(null)

      try {
        await kickPilotProvision(sessionToken)
      } catch (error) {
        if (redirectIfSignupSessionExpired(error, navigate)) {
          return
        }
        setProvisioningError(
          getApiErrorMessage(
            error,
            "We couldn't restart setup. Please try again."
          )
        )
        setIsBusy(false)
        return
      }

      provisioningRunId.current += 1
      setProvisioningAttempt((current) => current + 1)
      setIsBusy(false)
    })()
  }

  const handleContinueOnFree = () => {
    if (isBusy || !sessionToken) return

    void (async () => {
      setIsBusy(true)
      try {
        const session = await signupSessionLogin(sessionToken, {
          continueOnFree: true,
        })
        persistSignupLoginSession(session)
        clearSignupSessionToken()
        // Free cap is 1 Location: the Locations step forwards to the WebApp.
        navigate("/signup/locations", { replace: true })
      } catch (error) {
        if (redirectIfSignupSessionExpired(error, navigate)) {
          return
        }
        setProvisioningError(
          getApiErrorMessage(
            error,
            "We couldn't sign you in. Please try again."
          )
        )
        setIsBusy(false)
      }
    })()
  }

  if (!sessionToken) {
    return null
  }

  return (
    <GuestLoopShell
      contentAlign="center"
      contentMaxWidthClassName="max-w-none"
      showBackButton={Boolean(provisioningError)}
      backButtonPlacement="top"
      backButtonDisabled={false}
      onBack={provisioningError ? goToOnboarding : undefined}
    >
      {provisioningError ? (
        <div className="mx-auto flex w-full max-w-[473px] flex-col gap-6">
          <p className="m-0 text-center font-sans text-lg font-normal leading-6 text-[#141414]">
            {isPaymentFailed
              ? PAYMENT_FAILED_HEADING
              : "We couldn't finish setting up your account."}
          </p>
          <div
            className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-center text-sm text-red-700"
            role="alert"
          >
            {provisioningError}
          </div>
          <Button
            type="button"
            disabled={isBusy}
            onClick={handleRetry}
            className="h-[45px] w-full rounded-[4px] bg-[#14a74a] text-sm font-medium text-white shadow-none hover:bg-[#14a74a]/90"
          >
            {isBusy ? "Retrying…" : "Retry"}
          </Button>
          {isPaymentFailed ? (
            <Button
              type="button"
              variant="outline"
              disabled={isBusy}
              onClick={handleContinueOnFree}
              className="h-[45px] w-full rounded-[4px] border-[#4e4e4e] bg-transparent text-sm font-medium text-[#141414] shadow-none"
            >
              {PAYMENT_FAILED_CONTINUE_FREE}
            </Button>
          ) : null}
        </div>
      ) : (
        <p
          className="m-0 text-center font-sans text-lg font-normal leading-6 text-[#141414]"
          role="status"
          aria-live="polite"
        >
          {PROVISIONING_LOADING_COPY}
        </p>
      )}
    </GuestLoopShell>
  )
}

export default SignupProvisioningPage

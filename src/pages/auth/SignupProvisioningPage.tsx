import { useCallback, useEffect, useRef, useState } from "react"
import { isAxiosError } from "axios"
import { useNavigate } from "react-router-dom"

import {
  getSignupProvisioningStatus,
  retrySignupProvision,
} from "@/api/signupApi"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import { Button } from "@/components/ui/button"
import {
  clearSignupSessionToken,
  readSignupSessionToken,
} from "@/lib/signupSession"

const POLL_INTERVAL_MS = 2000

/** Figma provisioning loading copy (`4974:23056`). */
const PROVISIONING_LOADING_COPY = "Setting up your Tummly workspace…"

async function kickPilotProvision(sessionToken: string) {
  await retrySignupProvision(sessionToken)
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

function SignupProvisioningPage() {
  const navigate = useNavigate()
  const sessionToken = readSignupSessionToken()

  const [provisioningError, setProvisioningError] = useState<string | null>(
    null
  )
  const [isBusy, setIsBusy] = useState(false)
  const [provisioningAttempt, setProvisioningAttempt] = useState(0)

  const provisioningRunId = useRef(0)
  /** Avoid /signup bounce when goToLogin clears the session before navigate lands. */
  const leavingForLoginRef = useRef(false)

  const goToLogin = useCallback(() => {
    leavingForLoginRef.current = true
    clearSignupSessionToken()
    navigate("/login?setup=complete", { replace: true })
  }, [navigate])

  const goToOnboarding = useCallback(() => {
    navigate("/signup/onboarding", { replace: true })
  }, [navigate])

  useEffect(() => {
    if (!sessionToken) {
      if (leavingForLoginRef.current) {
        navigate("/login?setup=complete", { replace: true })
        return
      }
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

        goToLogin()
      } catch (error) {
        if (!isCurrent()) return
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
  }, [goToLogin, navigate, provisioningAttempt, sessionToken])

  const handleRetry = () => {
    if (isBusy || !sessionToken) return

    void (async () => {
      setIsBusy(true)
      setProvisioningError(null)

      try {
        await kickPilotProvision(sessionToken)
      } catch (error) {
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

  if (!sessionToken) {
    return null
  }

  return (
    <GuestLoopShell
      contentAlign="center"
      contentMaxWidthClassName="max-w-none"
      showBackButton={Boolean(provisioningError)}
      backButtonDisabled={false}
      onBack={provisioningError ? goToOnboarding : undefined}
    >
      {provisioningError ? (
        <div className="mx-auto flex w-full max-w-[473px] flex-col gap-6">
          <p className="m-0 text-center font-sans text-lg font-normal leading-6 text-[#141414]">
            We couldn&apos;t finish setting up your account.
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

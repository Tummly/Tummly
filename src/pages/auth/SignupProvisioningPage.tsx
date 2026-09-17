import { useCallback, useEffect, useRef, useState } from "react"
import { isAxiosError } from "axios"
import { useNavigate, useSearchParams } from "react-router-dom"

import {
  chooseSignupPlan,
  getSignupProvisioningStatus,
} from "@/api/signupApi"
import { GuestLoopReadyStep } from "@/components/guest-loop/GuestLoopReadyStep"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import {
  runProvisioningPhases,
  type ProvisioningPhaseStatus,
} from "@/lib/runProvisioningPhases"
import {
  clearSignupPaidIntent,
  clearSignupSessionToken,
  readSignupPaidIntent,
  readSignupSessionToken,
} from "@/lib/signupSession"

const POLL_INTERVAL_MS = 2000
/** Stop infinite AwaitingPayment polls after Revolut cancel / abandon. */
const AWAITING_PAYMENT_TIMEOUT_MS = 60_000
/** Shorter wait when returning from checkout (`?pay=return`). */
const AWAITING_PAYMENT_RETURN_TIMEOUT_MS = 45_000

const CONFIRMING_PAYMENT_COPY =
  "We're confirming your payment. This usually takes a few seconds — hang tight."

const PREPARING_COPY =
  "We're preparing the core setup for this location. You can sign in once setup finishes."

const PAYMENT_NOT_COMPLETE_MESSAGE =
  "Payment not complete. Choose a plan again, or try payment once more."

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
  const [searchParams] = useSearchParams()
  const sessionToken = readSignupSessionToken()
  const isPayReturn = searchParams.get("pay") === "return"
  const awaitingTimeoutMs = isPayReturn
    ? AWAITING_PAYMENT_RETURN_TIMEOUT_MS
    : AWAITING_PAYMENT_TIMEOUT_MS

  const [phase1Status, setPhase1Status] =
    useState<ProvisioningPhaseStatus>("idle")
  const [phase2Status, setPhase2Status] =
    useState<ProvisioningPhaseStatus>("idle")
  const [phase3Status, setPhase3Status] =
    useState<ProvisioningPhaseStatus>("idle")
  const [isWorkspaceReady, setIsWorkspaceReady] = useState(false)
  const [provisioningError, setProvisioningError] = useState<string | null>(
    null
  )
  const [headerDescription, setHeaderDescription] = useState(PREPARING_COPY)
  const [provisioningAttempt, setProvisioningAttempt] = useState(0)
  const [isPaidPath, setIsPaidPath] = useState(false)
  const [isConfirmingPayment, setIsConfirmingPayment] = useState(false)

  const provisioningRunId = useRef(0)
  const isPaidPathRef = useRef(false)
  const awaitingPaymentSinceRef = useRef<number | null>(null)

  const isProvisioningActive =
    phase1Status === "loading" ||
    phase2Status === "loading" ||
    phase3Status === "loading"

  const goToLogin = useCallback(() => {
    clearSignupSessionToken()
    navigate("/login?setup=complete", { replace: true })
  }, [navigate])

  const goToChoosePlan = useCallback(
    (reason: "cancelled" | "incomplete" = "cancelled") => {
      clearSignupPaidIntent()
      navigate(`/signup/choose-plan?pay=${reason}`, { replace: true })
    },
    [navigate]
  )

  useEffect(() => {
    if (!sessionToken) {
      navigate("/signup", { replace: true })
      return
    }

    const runId = ++provisioningRunId.current
    let cancelled = false

    const isCurrent = () =>
      !cancelled && runId === provisioningRunId.current

    const markPaidPath = () => {
      isPaidPathRef.current = true
      if (!isCurrent()) return
      setIsPaidPath(true)
    }

    // Persist across checkout redirect: first poll may already be Provisioning.
    if (readSignupPaidIntent() || isPayReturn) {
      markPaidPath()
    }

    const markAwaitingPayment = () => {
      markPaidPath()
      if (awaitingPaymentSinceRef.current == null) {
        awaitingPaymentSinceRef.current = Date.now()
      }
      if (!isCurrent()) return
      setIsConfirmingPayment(true)
      setHeaderDescription(CONFIRMING_PAYMENT_COPY)
      setPhase1Status("loading")
      setPhase2Status("idle")
      setPhase3Status("idle")
    }

    const awaitingPaymentTimedOut = () => {
      const since = awaitingPaymentSinceRef.current
      if (since == null) return false
      return Date.now() - since >= awaitingTimeoutMs
    }

    const pollUntilProvisionable = async (): Promise<"ok" | "redirected"> => {
      while (isCurrent()) {
        const status = await getSignupProvisioningStatus(sessionToken)

        if (status.status === "Abandoned") {
          throw new Error(
            "This signup session is no longer active. Please start again."
          )
        }

        if (status.status === "OnboardingComplete") {
          navigate("/signup/choose-plan", { replace: true })
          return "redirected"
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
          markAwaitingPayment()
          if (awaitingPaymentTimedOut()) {
            throw new Error(PAYMENT_NOT_COMPLETE_MESSAGE)
          }
          await sleep(POLL_INTERVAL_MS)
          continue
        }

        awaitingPaymentSinceRef.current = null
        if (isCurrent()) {
          setIsConfirmingPayment(false)
        }

        if (
          status.status === "Provisioning" ||
          status.status === "Complete" ||
          status.ready
        ) {
          if (isCurrent()) {
            setHeaderDescription(PREPARING_COPY)
          }
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
          return
        }

        if (status.status === "Provisioning") {
          awaitingPaymentSinceRef.current = null
          if (isCurrent()) {
            setIsConfirmingPayment(false)
          }
          await sleep(POLL_INTERVAL_MS)
          continue
        }

        if (status.status === "AwaitingPayment") {
          markAwaitingPayment()
          if (awaitingPaymentTimedOut()) {
            throw new Error(PAYMENT_NOT_COMPLETE_MESSAGE)
          }
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
      setIsWorkspaceReady(false)
      setPhase1Status("idle")
      setPhase2Status("idle")
      setPhase3Status("idle")
      setHeaderDescription(PREPARING_COPY)
      setIsConfirmingPayment(false)
      awaitingPaymentSinceRef.current = null

      try {
        const gate = await pollUntilProvisionable()
        if (!isCurrent() || gate === "redirected") return

        const result = await runProvisioningPhases(
          () => waitUntilReady(),
          (snapshot) => {
            if (!isCurrent()) return
            setPhase1Status(snapshot.phase1)
            setPhase2Status(snapshot.phase2)
            setPhase3Status(snapshot.phase3)
          }
        )

        if (!isCurrent()) return

        if (result.success) {
          setIsWorkspaceReady(true)
          goToLogin()
          return
        }

        setProvisioningError(result.message)
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
  }, [
    awaitingTimeoutMs,
    goToLogin,
    isPayReturn,
    navigate,
    provisioningAttempt,
    sessionToken,
  ])

  const handleRetry = () => {
    if (isProvisioningActive || !sessionToken) return

    const paid = isPaidPathRef.current || isPaidPath
    const paymentIncomplete =
      provisioningError === PAYMENT_NOT_COMPLETE_MESSAGE ||
      (provisioningError?.includes("Payment not complete") ?? false)

    if (paid && paymentIncomplete) {
      goToChoosePlan("incomplete")
      return
    }

    void (async () => {
      setProvisioningError(null)
      setIsWorkspaceReady(false)
      setPhase1Status("idle")
      setPhase2Status("idle")
      setPhase3Status("idle")

      if (!paid) {
        try {
          await chooseSignupPlan(sessionToken, {
            planId: "Pilot",
            cadence: "monthly",
          })
        } catch (error) {
          setProvisioningError(
            getApiErrorMessage(
              error,
              "We couldn't restart Essential setup. Please try again."
            )
          )
          return
        }
      }

      provisioningRunId.current += 1
      setProvisioningAttempt((current) => current + 1)
    })()
  }

  if (!sessionToken) {
    return null
  }

  const showChoosePlanEscape =
    isConfirmingPayment ||
    provisioningError === PAYMENT_NOT_COMPLETE_MESSAGE ||
    (provisioningError?.includes("Payment not complete") ?? false)

  return (
    <GuestLoopShell
      contentAlign="start"
      showBackButton={Boolean(provisioningError) || showChoosePlanEscape}
      backButtonDisabled={false}
      onBack={
        showChoosePlanEscape
          ? () => {
              goToChoosePlan(
                provisioningError ? "incomplete" : "cancelled"
              )
            }
          : provisioningError
            ? () => {
                navigate("/signup/choose-plan", { replace: true })
              }
            : undefined
      }
    >
      <GuestLoopReadyStep
        activeStep={3}
        phaseStatuses={{
          phase1: phase1Status,
          phase2: phase2Status,
          phase3: phase3Status,
        }}
        isWorkspaceReady={isWorkspaceReady}
        provisioningError={provisioningError}
        isProvisioningActive={isProvisioningActive}
        description={headerDescription}
        primaryActionLabel="Continue to sign in"
        onOpenWorkspace={goToLogin}
        onRetry={handleRetry}
        secondaryActionLabel={
          showChoosePlanEscape
            ? isConfirmingPayment && !provisioningError
              ? "Cancel / choose a different plan"
              : "Back to choose plan"
            : undefined
        }
        onSecondaryAction={
          showChoosePlanEscape
            ? () => {
                goToChoosePlan(
                  provisioningError ? "incomplete" : "cancelled"
                )
              }
            : undefined
        }
        secondaryActionEnabled={!isWorkspaceReady}
      />
    </GuestLoopShell>
  )
}

export default SignupProvisioningPage

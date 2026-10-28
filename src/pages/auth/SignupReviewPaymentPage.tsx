import { useCallback, useEffect, useState } from "react"
import { isAxiosError } from "axios"
import { useNavigate } from "react-router-dom"

import { SetupAccountStatus } from "@/components/auth/SetupAccountShell"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import { SignupCustomisePlanStep } from "@/components/signup/SignupCustomisePlanStep"
import { SignupReviewPlanStep } from "@/components/signup/SignupReviewPlanStep"
import {
  getSignupProvisioningStatus,
  updateSignupPlan,
} from "@/api/signupApi"
import { quoteReviewMessage } from "@/lib/marketingCustomPlan/quoteReview"
import {
  signupPlanIntentFromSavedPlan,
  signupPlanUpdatePayload,
  signupReviewSubmitOutcome,
} from "@/lib/signupPaymentReview"
import {
  clearSignupPlanIntent,
  readSignupPlanIntent,
  saveSignupPlanIntent,
} from "@/lib/signupPlanIntent"
import {
  readSignupSessionToken,
  redirectIfSignupSessionExpired,
} from "@/lib/signupSession"

const DENSE_STEP_ZOOM = "w-full [zoom:0.8] min-[2200px]:[zoom:1]"

type Phase = "loading" | "customise" | "review" | "error"

function getApiErrorCode(error: unknown): string | null {
  const code = (error as { code?: unknown } | null)?.code
  return typeof code === "string" ? code : null
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

/**
 * After a paid checkout did not open because the saved quote is stale or names a
 * retired option: the guest sees a fresh server quote for the saved choices (or
 * chooses again), ticks the updated total, and only then is checkout opened.
 */
function SignupReviewPaymentPage() {
  const navigate = useNavigate()
  const sessionToken = readSignupSessionToken()

  const [phase, setPhase] = useState<Phase>("loading")
  const [loadError, setLoadError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  // The server refused the reviewed hash: review the newest total again.
  const [quoteChanged, setQuoteChanged] = useState(true)

  const goToProvisioning = useCallback(() => {
    navigate("/signup/provisioning", { replace: true })
  }, [navigate])

  useEffect(() => {
    if (!sessionToken) {
      navigate("/signup", { replace: true })
      return
    }

    let cancelled = false
    void (async () => {
      try {
        const status = await getSignupProvisioningStatus(sessionToken)
        if (cancelled) return
        const intent =
          status.status === "PaymentFailed"
            ? signupPlanIntentFromSavedPlan(status.savedPlan)
            : null
        if (intent == null) {
          goToProvisioning()
          return
        }
        saveSignupPlanIntent(intent)
        setPhase(intent.capacityOptionRetired === true ? "customise" : "review")
      } catch (error) {
        if (cancelled) return
        if (redirectIfSignupSessionExpired(error, navigate)) return
        setLoadError(
          getApiErrorMessage(
            error,
            "We couldn't load your plan. Please try again."
          )
        )
        setPhase("error")
      }
    })()

    return () => {
      cancelled = true
    }
  }, [goToProvisioning, navigate, sessionToken])

  const handleConfirm = async () => {
    if (!sessionToken || isSaving) return
    const payload = signupPlanUpdatePayload(readSignupPlanIntent())
    if (payload == null) return

    setIsSaving(true)
    setSubmitError(null)
    setQuoteChanged(false)
    try {
      await updateSignupPlan(sessionToken, payload)
      clearSignupPlanIntent()
      goToProvisioning()
    } catch (error) {
      if (redirectIfSignupSessionExpired(error, navigate)) return
      const code = getApiErrorCode(error)
      switch (signupReviewSubmitOutcome(code)) {
        case "review_again":
          setQuoteChanged(true)
          break
        case "choose_again": {
          const current = readSignupPlanIntent()
          if (current != null) {
            saveSignupPlanIntent({
              plan: current.plan,
              cadence: current.cadence,
              capacityOptionRetired: true,
            })
          }
          setSubmitError(quoteReviewMessage(code))
          setPhase("customise")
          break
        }
        default:
          setSubmitError(
            getApiErrorMessage(
              error,
              "We couldn't save your plan. Please try again."
            )
          )
      }
    } finally {
      setIsSaving(false)
    }
  }

  if (!sessionToken) {
    return null
  }

  if (phase === "loading") {
    return <SetupAccountStatus title="Loading your plan" />
  }

  if (phase === "error") {
    return (
      <SetupAccountStatus
        tone="error"
        title="Unable to continue signup"
        message={loadError ?? undefined}
      />
    )
  }

  return (
    <GuestLoopShell
      contentAlign="start"
      showBackButton={false}
      contentMaxWidthClassName={
        phase === "customise" ? "max-w-[1548px]" : "max-w-[640px]"
      }
      wide
    >
      {submitError != null ? (
        <p role="alert" className="mb-4 m-0 text-sm text-red-600">
          {submitError}
        </p>
      ) : null}
      <div className={DENSE_STEP_ZOOM}>
        {phase === "customise" ? (
          <SignupCustomisePlanStep
            onBack={goToProvisioning}
            onContinueToReview={() => {
              setQuoteChanged(true)
              setPhase("review")
            }}
          />
        ) : (
          <SignupReviewPlanStep
            onBack={goToProvisioning}
            onConfirm={handleConfirm}
            isSubmitting={isSaving}
            quoteChanged={quoteChanged}
          />
        )}
      </div>
    </GuestLoopShell>
  )
}

export default SignupReviewPaymentPage

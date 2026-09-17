import { useEffect, useState } from "react"
import { isAxiosError } from "axios"
import { useNavigate, useSearchParams } from "react-router-dom"

import { chooseSignupPlan, getSignupSession } from "@/api/signupApi"
import { SetupAccountStatus } from "@/components/auth/SetupAccountShell"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import { SignupChoosePlanCards } from "@/components/signup/SignupChoosePlanCards"
import { SignupPlanFeaturesDialog } from "@/components/signup/SignupPlanFeaturesDialog"
import type {
  BillingCadence,
  ManagePlanId,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  mapSignupCardToPlanId,
  type SignupPlanCard,
} from "@/lib/signupPlanMap"
import {
  clearSignupPaidIntent,
  markSignupPaidIntent,
  readSignupSessionToken,
} from "@/lib/signupSession"

const CHOOSE_PLAN_STATUSES = new Set([
  "OnboardingComplete",
  "AwaitingPayment",
  "Provisioning",
])

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    if (error.response?.status === 501) {
      return (
        error.response?.data?.message ??
        "Paid plan checkout is not available yet. Choose Essential (Pilot) for now, or try again later."
      )
    }
    return error.response?.data?.message ?? fallback
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

function SignupChoosePlanPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const sessionToken = readSignupSessionToken()

  const [gateState, setGateState] = useState<"loading" | "ready" | "error">(
    "loading"
  )
  const [gateError, setGateError] = useState<string | null>(null)
  const [cadence, setCadence] = useState<BillingCadence>("monthly")
  const [featuresOpen, setFeaturesOpen] = useState(false)
  const [busyCard, setBusyCard] = useState<SignupPlanCard | null>(null)
  const [busyPlanId, setBusyPlanId] = useState<ManagePlanId | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  useEffect(() => {
    const pay = searchParams.get("pay")
    if (pay !== "cancelled" && pay !== "incomplete") return

    setActionError(
      pay === "incomplete"
        ? "Payment not complete. Choose a plan again, or try payment once more."
        : "Payment was cancelled. Choose a plan to continue."
    )
    clearSignupPaidIntent()

    const next = new URLSearchParams(searchParams)
    next.delete("pay")
    setSearchParams(next, { replace: true })
  }, [searchParams, setSearchParams])

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

        if (session.status === "Complete") {
          navigate("/login?setup=complete", { replace: true })
          return
        }

        if (!CHOOSE_PLAN_STATUSES.has(session.status)) {
          if (session.status === "Verified") {
            navigate("/signup/onboarding", { replace: true })
            return
          }
          navigate("/signup", { replace: true })
          return
        }

        setGateState("ready")
      } catch (error) {
        if (cancelled) return
        setGateError(
          getApiErrorMessage(
            error,
            "We couldn't load your signup session. Please start again."
          )
        )
        setGateState("error")
      }
    })()

    return () => {
      cancelled = true
    }
  }, [sessionToken, navigate])

  const runChoosePlan = async (
    planId: ManagePlanId,
    options?: { card?: SignupPlanCard }
  ) => {
    if (!sessionToken || busyCard != null || busyPlanId != null) return

    setActionError(null)
    if (options?.card) {
      setBusyCard(options.card)
    } else {
      setBusyPlanId(planId)
    }

    try {
      const result = await chooseSignupPlan(sessionToken, {
        planId,
        cadence,
      })

      if (result.mode === "provisioned") {
        clearSignupPaidIntent()
        navigate("/signup/provisioning", { replace: true })
        return
      }

      if (result.mode === "checkout") {
        if (!result.checkoutUrl) {
          setActionError(
            "Checkout started but no payment URL was returned. Please try again."
          )
          return
        }
        markSignupPaidIntent()
        window.location.assign(result.checkoutUrl)
        return
      }

      setActionError(
        `Unexpected choose-plan response (${result.mode || "empty"}). Please try again.`
      )
    } catch (error) {
      setActionError(
        getApiErrorMessage(
          error,
          "We couldn't start your plan. Please try again."
        )
      )
    } finally {
      setBusyCard(null)
      setBusyPlanId(null)
    }
  }

  if (!sessionToken) {
    return null
  }

  if (gateState === "loading") {
    return <SetupAccountStatus title="Loading your plan options" />
  }

  if (gateState === "error") {
    return (
      <SetupAccountStatus
        tone="error"
        title="Unable to continue signup"
        message={gateError ?? undefined}
      />
    )
  }

  const isBusy = busyCard != null || busyPlanId != null

  return (
    <GuestLoopShell
      contentAlign="start"
      contentMaxWidthClassName="max-w-[min(100%,960px)]"
      showBackButton
      backButtonDisabled={isBusy}
      onBack={() => {
        navigate("/signup/onboarding")
      }}
    >
      <div className="flex w-full flex-col gap-10">
        <header className="flex flex-col gap-3 text-[#232323]">
          <h1 className="m-0 font-serif text-[clamp(1.75rem,4vw,2.25rem)] font-medium leading-normal tracking-[-0.36px]">
            Choose how you&apos;d like to start
          </h1>
          <p className="m-0 max-w-[40rem] text-base leading-[22px] tracking-[-0.32px]">
            Choose Essential (Pilot) or Pro (Growth). Open the full features
            list to compare every plan.
          </p>
        </header>

        {actionError ? (
          <p
            role="alert"
            className="m-0 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
          >
            {actionError}
          </p>
        ) : null}

        <SignupChoosePlanCards
          cadence={cadence}
          onCadenceChange={setCadence}
          onSelectCard={(card) => {
            void runChoosePlan(mapSignupCardToPlanId(card), { card })
          }}
          onOpenFeatures={() => {
            setFeaturesOpen(true)
          }}
          busyCard={busyCard}
          disabled={isBusy}
        />
      </div>

      <SignupPlanFeaturesDialog
        open={featuresOpen}
        onOpenChange={setFeaturesOpen}
        cadence={cadence}
        onSelectPlan={(planId) => {
          void runChoosePlan(planId)
        }}
        busyPlanId={busyPlanId}
        disabled={isBusy}
      />
    </GuestLoopShell>
  )
}

export default SignupChoosePlanPage

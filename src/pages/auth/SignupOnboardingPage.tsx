import { useEffect, useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { isAxiosError } from "axios"
import { useForm } from "react-hook-form"
import { useNavigate } from "react-router-dom"

import { getSignupSession, saveSignupOnboarding } from "@/api/signupApi"
import { SetupAccountStatus } from "@/components/auth/SetupAccountShell"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import { validateWizardStep } from "@/components/guest-loop/useGuestLoopStepCanSubmit"
import { SignupAccountStep } from "@/components/signup/SignupAccountStep"
import { SignupChoosePlanStep } from "@/components/signup/SignupChoosePlanStep"
import { SignupCustomisePlanStep } from "@/components/signup/SignupCustomisePlanStep"
import { SignupLocationStep } from "@/components/signup/SignupLocationStep"
import { SignupRestaurantStep } from "@/components/signup/SignupRestaurantStep"
import { SignupReviewPlanStep } from "@/components/signup/SignupReviewPlanStep"
import { Form } from "@/components/ui/form"
import { defaultFormValidationOptions } from "@/lib/form"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  clearSignupOnboardingDraft,
  readSignupOnboardingDraft,
  saveSignupOnboardingDraft,
} from "@/lib/signupOnboardingDraft"
import { readSignupSessionToken } from "@/lib/signupSession"
import {
  activeCustomConfig,
  clearSignupPlanIntent,
  readSignupPlanIntent,
  saveSignupPlanIntent,
  signupStepAfterLocation,
} from "@/lib/signupPlanIntent"
import {
  signupAccountStepFields,
  signupAccountStepSchema,
  signupAccountStepSocialFields,
  signupAccountStepSocialSchema,
  signupLocationStepFields,
  signupLocationStepSchema,
  signupOnboardingDefaultValues,
  signupOnboardingSchema,
  signupOnboardingSocialSchema,
  signupRestaurantStepFields,
  signupRestaurantStepSchema,
  splitSignupFullName,
  toSignupOnboardingPayload,
  type SignupOnboardingFormValues,
} from "@/schemas/signupOnboarding"

/** Server error code from a rejected request (for example quote_changed). */
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

type OnboardingSessionPrefill = {
  sessionToken: string
  email: string
  firstName: string
  lastName: string
  isSocial: boolean
}

type SignupOnboardingWizardProps = {
  prefill: OnboardingSessionPrefill
}

type WizardStep = 1 | 2 | 3 | 4 | 5 | 6

/**
 * Calculator + Review Figma frames are drawn for 2K screens; below that width
 * scale the body to 80% so 1080p screens need less scrolling. The footer bar
 * is portalled outside this wrapper and keeps full size.
 */
const DENSE_STEP_ZOOM = "w-full [zoom:0.8] min-[2200px]:[zoom:1]"

function SignupOnboardingWizard({ prefill }: SignupOnboardingWizardProps) {
  const navigate = useNavigate()
  const { isSocial, sessionToken } = prefill
  // Reload keeps entered details and plan; password is never stored, so
  // email signups resume on step 1 to re-enter it.
  const [draft] = useState(() => readSignupOnboardingDraft(sessionToken))
  const [step, setStep] = useState<WizardStep>(() => {
    if (draft == null || !isSocial) return 1
    return Math.min(Math.max(1, Math.floor(draft.step)), 6) as WizardStep
  })
  const [isSaving, setIsSaving] = useState(false)
  // The saved quote no longer matches the server: stay on review and say so.
  const [quoteChanged, setQuoteChanged] = useState(false)

  const form = useForm<SignupOnboardingFormValues>({
    resolver: zodResolver(
      isSocial ? signupOnboardingSocialSchema : signupOnboardingSchema,
    ),
    defaultValues: {
      ...signupOnboardingDefaultValues,
      ...draft?.values,
      token: prefill.sessionToken,
      email: prefill.email,
      firstName: draft?.values.firstName ?? prefill.firstName,
      lastName: draft?.values.lastName ?? prefill.lastName,
    },
    ...defaultFormValidationOptions,
  })

  useEffect(() => {
    saveSignupOnboardingDraft(sessionToken, step, form.getValues())
    const subscription = form.watch((values) => {
      saveSignupOnboardingDraft(
        sessionToken,
        step,
        values as Partial<SignupOnboardingFormValues>,
      )
    })
    return () => subscription.unsubscribe()
  }, [form, sessionToken, step])

  const handleContinueAccount = () => {
    const valid = isSocial
      ? validateWizardStep(
          form,
          signupAccountStepSocialFields,
          signupAccountStepSocialSchema,
        )
      : validateWizardStep(
          form,
          signupAccountStepFields,
          signupAccountStepSchema,
        )
    if (!valid) return
    setStep(2)
  }

  const handleContinueRestaurant = () => {
    const valid = validateWizardStep(
      form,
      signupRestaurantStepFields,
      signupRestaurantStepSchema,
    )
    if (!valid) return
    setStep(3)
  }

  const handleContinueLocation = () => {
    const valid = validateWizardStep(
      form,
      signupLocationStepFields,
      signupLocationStepSchema,
    )
    if (!valid) return
    // A plan built on the Custom plan page needs no second customise pass.
    setStep(
      signupStepAfterLocation(readSignupPlanIntent()) === "review" ? 6 : 4,
    )
  }

  const saveOnboarding = async () => {
    setIsSaving(true)
    setQuoteChanged(false)
    form.clearErrors("root")

    try {
      await saveSignupOnboarding(
        sessionToken,
        toSignupOnboardingPayload(form.getValues(), { isSocial }),
      )
      clearSignupPlanIntent()
      clearSignupOnboardingDraft(sessionToken)
      navigate("/signup/provisioning", { replace: true })
    } catch (error) {
      if (getApiErrorCode(error) === "quote_changed") {
        setQuoteChanged(true)
        return
      }
      form.setError("root", {
        type: "server",
        message: getApiErrorMessage(
          error,
          "We couldn't save your details. Please try again.",
        ),
      })
    } finally {
      setIsSaving(false)
    }
  }

  const handlePilotContinue = () => {
    saveSignupPlanIntent({ plan: "Pilot" })
    void saveOnboarding()
  }

  const handleCheckoutPlain = (
    plan: "Starter" | "Growth" | "Group",
    cadence: BillingCadence,
  ) => {
    const existing = readSignupPlanIntent()
    // Keep restored custom config when the selected card matches its base plan.
    if (
      existing?.config != null &&
      existing.config.basePlan === plan.toLowerCase()
    ) {
      saveSignupPlanIntent({
        plan: existing.plan === "Custom" ? "Custom" : plan,
        cadence,
        config: { ...existing.config, cadence },
      })
    } else {
      // Keep the saved customisation for its own card; it is not applied here.
      saveSignupPlanIntent({
        plan,
        cadence,
        ...(existing?.config != null ? { config: existing.config } : {}),
      })
    }
    setStep(6)
  }

  const handleCustomise = (
    plan: "Starter" | "Growth" | "Group",
    cadence: BillingCadence,
  ) => {
    const existing = readSignupPlanIntent()
    saveSignupPlanIntent({
      plan,
      cadence,
      config:
        existing?.config != null &&
        existing.config.basePlan === plan.toLowerCase()
          ? { ...existing.config, cadence }
          : {
              version: 2,
              cadence,
              basePlan: plan.toLowerCase() as "starter" | "growth" | "group",
              locations: 1,
              packs: {},
            },
    })
    setStep(5)
  }

  const handleBack = () => {
    if (step === 2) {
      setStep(1)
      return
    }
    if (step === 3) {
      setStep(2)
      return
    }
    if (step === 4) {
      setStep(3)
      return
    }
    if (step === 5) {
      setStep(4)
      return
    }
    if (step === 6) {
      // Prefer customise when a custom config exists.
      setStep(activeCustomConfig(readSignupPlanIntent()) != null ? 5 : 4)
    }
  }

  const contentMaxWidth =
    step === 4
      ? "max-w-[1588px]"
      : step === 5
        ? "max-w-[1548px]"
        : step === 6
          ? "max-w-[640px]"
          : "max-w-[473px]"

  const rootError = form.formState.errors.root?.message

  return (
    <Form {...form}>
      <GuestLoopShell
        contentAlign={step >= 4 ? "start" : "center"}
        showBackButton={false}
        contentMaxWidthClassName={contentMaxWidth}
        wide={step >= 4}
      >
        {step === 1 ? (
          <SignupAccountStep
            form={form}
            onContinue={handleContinueAccount}
            isSocial={isSocial}
          />
        ) : null}
        {step === 2 ? (
          <SignupRestaurantStep
            form={form}
            onContinue={handleContinueRestaurant}
            onBack={handleBack}
          />
        ) : null}
        {step === 3 ? (
          <SignupLocationStep
            form={form}
            onContinue={handleContinueLocation}
            onBack={handleBack}
          />
        ) : null}
        {step === 4 ? (
          <>
            {rootError != null ? (
              <p className="mb-4 m-0 text-sm text-red-600">{rootError}</p>
            ) : null}
            <SignupChoosePlanStep
              onBack={handleBack}
              onCheckout={handleCheckoutPlain}
              onCustomise={handleCustomise}
              onPilotContinue={handlePilotContinue}
              isSubmitting={isSaving}
            />
          </>
        ) : null}
        {step === 5 ? (
          <div className={DENSE_STEP_ZOOM}>
            <SignupCustomisePlanStep
              onBack={handleBack}
              onContinueToReview={() => setStep(6)}
            />
          </div>
        ) : null}
        {step === 6 ? (
          <>
            {rootError != null ? (
              <p className="mb-4 m-0 text-sm text-red-600">{rootError}</p>
            ) : null}
            <div className={DENSE_STEP_ZOOM}>
              <SignupReviewPlanStep
                onBack={handleBack}
                onConfirm={saveOnboarding}
                isSubmitting={isSaving}
                quoteChanged={quoteChanged}
              />
            </div>
          </>
        ) : null}
      </GuestLoopShell>
    </Form>
  )
}

function SignupOnboardingPage() {
  const navigate = useNavigate()
  const sessionToken = readSignupSessionToken()

  const [gateState, setGateState] = useState<"loading" | "ready" | "error">(
    "loading",
  )
  const [gateError, setGateError] = useState<string | null>(null)
  const [prefill, setPrefill] = useState<OnboardingSessionPrefill | null>(null)

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

        if (
          session.status === "Provisioning" ||
          session.status === "AwaitingPayment" ||
          session.status === "OnboardingComplete"
        ) {
          navigate("/signup/provisioning", { replace: true })
          return
        }

        if (session.status !== "Verified") {
          navigate("/signup", { replace: true })
          return
        }

        const { firstName, lastName } = splitSignupFullName(session.fullName)

        setPrefill({
          sessionToken,
          email: session.email,
          firstName,
          lastName,
          isSocial: Boolean(session.authProvider?.trim()),
        })
        setGateState("ready")
      } catch (error) {
        if (cancelled) return
        setGateError(
          getApiErrorMessage(
            error,
            "We couldn't load your signup session. Please start again.",
          ),
        )
        setGateState("error")
      }
    })()

    return () => {
      cancelled = true
    }
  }, [sessionToken, navigate])

  if (!sessionToken) {
    return null
  }

  if (gateState === "loading") {
    return <SetupAccountStatus title="Loading your signup" />
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

  if (!prefill) {
    return <SetupAccountStatus title="Loading your signup" />
  }

  return <SignupOnboardingWizard prefill={prefill} />
}

export default SignupOnboardingPage

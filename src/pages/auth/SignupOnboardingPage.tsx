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
import { SignupLocationStep } from "@/components/signup/SignupLocationStep"
import { SignupRestaurantStep } from "@/components/signup/SignupRestaurantStep"
import { Form } from "@/components/ui/form"
import { defaultFormValidationOptions } from "@/lib/form"
import { readSignupSessionToken } from "@/lib/signupSession"
import {
  signupAccountStepFields,
  signupAccountStepSchema,
  signupLocationStepFields,
  signupLocationStepSchema,
  signupOnboardingDefaultValues,
  signupOnboardingSchema,
  signupRestaurantStepFields,
  signupRestaurantStepSchema,
  toSignupOnboardingPayload,
  type SignupOnboardingFormValues,
} from "@/schemas/signupOnboarding"

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

function SignupOnboardingPage() {
  const navigate = useNavigate()
  const sessionToken = readSignupSessionToken()

  const [gateState, setGateState] = useState<"loading" | "ready" | "error">(
    "loading"
  )
  const [gateError, setGateError] = useState<string | null>(null)
  const [step, setStep] = useState(1)
  const [isSaving, setIsSaving] = useState(false)

  const form = useForm<SignupOnboardingFormValues>({
    resolver: zodResolver(signupOnboardingSchema),
    defaultValues: signupOnboardingDefaultValues,
    ...defaultFormValidationOptions,
  })

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

        form.reset({
          ...signupOnboardingDefaultValues,
          token: sessionToken,
          email: session.email,
          firstName: "",
          lastName: "",
        })
        setStep(1)
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
    // One-shot session gate; form instance is stable for this page.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessionToken, navigate])

  const handleContinueAccount = () => {
    const valid = validateWizardStep(
      form,
      signupAccountStepFields,
      signupAccountStepSchema
    )
    if (!valid) return
    setStep(2)
  }

  const handleContinueRestaurant = () => {
    const valid = validateWizardStep(
      form,
      signupRestaurantStepFields,
      signupRestaurantStepSchema
    )
    if (!valid) return
    setStep(3)
  }

  const handleFinish = async () => {
    if (!sessionToken) {
      navigate("/signup", { replace: true })
      return
    }

    const valid = validateWizardStep(
      form,
      signupLocationStepFields,
      signupLocationStepSchema
    )
    if (!valid) return

    setIsSaving(true)
    form.clearErrors("root")

    try {
      await saveSignupOnboarding(
        sessionToken,
        toSignupOnboardingPayload(form.getValues())
      )
      navigate("/signup/provisioning", { replace: true })
    } catch (error) {
      form.setError("root", {
        type: "server",
        message: getApiErrorMessage(
          error,
          "We couldn't save your details. Please try again."
        ),
      })
    } finally {
      setIsSaving(false)
    }
  }

  const handleBack = () => {
    if (step === 2) {
      setStep(1)
      return
    }
    if (step === 3) {
      setStep(2)
    }
  }

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

  return (
    <Form {...form}>
      <GuestLoopShell
        contentAlign="center"
        showBackButton={false}
        contentMaxWidthClassName="max-w-[473px]"
      >
        {step === 1 ? (
          <SignupAccountStep form={form} onContinue={handleContinueAccount} />
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
            onContinue={handleFinish}
            onBack={handleBack}
            isSubmitting={isSaving}
          />
        ) : null}
      </GuestLoopShell>
    </Form>
  )
}

export default SignupOnboardingPage

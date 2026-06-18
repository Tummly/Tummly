import { useCallback, useEffect, useRef, useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import axios, { isAxiosError } from "axios"
import { useForm } from "react-hook-form"
import { useNavigate, useSearchParams } from "react-router-dom"

import { SetupAccountStatus } from "@/components/auth/SetupAccountShell"
import { GuestLoopPasswordStep } from "@/components/guest-loop/GuestLoopPasswordStep"
import { GuestLoopReadyStep } from "@/components/guest-loop/GuestLoopReadyStep"
import { GuestLoopRestaurantStep } from "@/components/guest-loop/GuestLoopRestaurantStep"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import { WizardLiveValidationProvider } from "@/components/form/WizardLiveValidationContext"
import { AUTH_API_BASE_URL } from "@/config/api"
import { useSetupTokenValidation } from "@/hooks/useSetupTokenValidation"
import { Form } from "@/components/ui/form"
import { addAttemptedFields, defaultFormValidationOptions } from "@/lib/form"
import {
  runProvisioningPhases,
  type ProvisioningPhaseStatus,
} from "@/lib/runProvisioningPhases"
import { isAccountAlreadyProvisionedMessage } from "@/lib/setupAccountErrors"
import {
  accountSetupSingleDefaultValues,
  accountSetupSingleSchema,
  accountSetupSingleStep1Fields,
  accountSetupSingleStep2Fields,
  toSingleLocationSetupPayload,
  type AccountSetupSingleFormValues,
} from "@/schemas/accountSetupSingle"

interface SetupAccountResponse {
  success?: boolean
  message?: string
  errors?: unknown
}

function RegisterSinglePage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const tokenFromParams = searchParams.get("token")?.trim() ?? ""
  const { token, tokenLoading, tokenError, prefill } =
    useSetupTokenValidation("Single")

  const form = useForm<AccountSetupSingleFormValues>({
    resolver: zodResolver(accountSetupSingleSchema),
    defaultValues: {
      ...accountSetupSingleDefaultValues,
      token: tokenFromParams,
    },
    ...defaultFormValidationOptions,
  })

  const [step, setStep] = useState(1)
  const [attemptedFields, setAttemptedFields] = useState<Set<string>>(new Set())
  const [phase1Status, setPhase1Status] = useState<ProvisioningPhaseStatus>("idle")
  const [phase2Status, setPhase2Status] = useState<ProvisioningPhaseStatus>("idle")
  const [phase3Status, setPhase3Status] = useState<ProvisioningPhaseStatus>("idle")
  const [isWorkspaceReady, setIsWorkspaceReady] = useState(false)
  const [provisioningError, setProvisioningError] = useState<string | null>(null)
  const [provisioningAttempt, setProvisioningAttempt] = useState(0)
  const provisioningRunId = useRef(0)
  const setupPromiseRef = useRef<Promise<void> | null>(null)

  const isProvisioningActive =
    phase1Status === "loading" ||
    phase2Status === "loading" ||
    phase3Status === "loading"

  useEffect(() => {
    if (!prefill) {
      return
    }

    form.reset({
      ...accountSetupSingleDefaultValues,
      token,
      email: prefill.email,
      fullName: prefill.fullName,
      restaurantName: prefill.businessName,
      phone: prefill.mobile,
      businessCategory:
        prefill.businessCategory ||
        accountSetupSingleDefaultValues.businessCategory,
    })
  }, [form, prefill, token])

  const handleContinueStep1 = async () => {
    const fieldsToValidate = Array.from(accountSetupSingleStep1Fields) as Array<
      keyof AccountSetupSingleFormValues
    >
    const valid = await form.trigger(fieldsToValidate)
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupSingleStep1Fields)
      )
      return
    }
    setStep(2)
  }

  const handleConfirmRestaurantSubmit = async () => {
    const fieldsToValidate = Array.from(accountSetupSingleStep2Fields) as Array<
      keyof AccountSetupSingleFormValues
    >
    const valid = await form.trigger(fieldsToValidate)
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupSingleStep2Fields)
      )
      return
    }

    form.clearErrors("root")
    setProvisioningError(null)
    setIsWorkspaceReady(false)
    setPhase1Status("idle")
    setPhase2Status("idle")
    setPhase3Status("idle")
    setupPromiseRef.current = null
    setStep(3)
  }

  const runSetupAccount = useCallback(async () => {
    if (setupPromiseRef.current) {
      return setupPromiseRef.current
    }

    const executeSetup = async () => {
      const step1Valid = await form.trigger(
        Array.from(accountSetupSingleStep1Fields) as Array<
          keyof AccountSetupSingleFormValues
        >
      )
      const step2Valid = await form.trigger(
        Array.from(accountSetupSingleStep2Fields) as Array<
          keyof AccountSetupSingleFormValues
        >
      )

      if (!step1Valid || !step2Valid) {
        throw new Error("Please complete all required fields.")
      }

      const values = form.getValues()

      try {
        const response = await axios.post<SetupAccountResponse>(
          `${AUTH_API_BASE_URL}/setup-account`,
          toSingleLocationSetupPayload(values)
        )

        if (response.data.success) {
          return
        }

        if (isAccountAlreadyProvisionedMessage(response.data.message)) {
          return
        }

        throw new Error(response.data.message || "Account setup failed.")
      } catch (error: unknown) {
        if (isAxiosError<SetupAccountResponse>(error)) {
          const message = error.response?.data?.message

          if (isAccountAlreadyProvisionedMessage(message)) {
            return
          }

          throw new Error(
            message || "Something went wrong during onboarding processing."
          )
        }

        if (error instanceof Error) {
          throw error
        }

        throw new Error("Something went wrong during onboarding processing.")
      }
    }

    const promise = executeSetup().finally(() => {
      if (setupPromiseRef.current === promise) {
        setupPromiseRef.current = null
      }
    })

    setupPromiseRef.current = promise
    return promise
  }, [form])

  useEffect(() => {
    if (step !== 3) {
      return
    }

    const runId = ++provisioningRunId.current
    let cancelled = false

    void (async () => {
      setProvisioningError(null)
      setIsWorkspaceReady(false)

      const result = await runProvisioningPhases(
        () => runSetupAccount(),
        (snapshot) => {
          if (cancelled || runId !== provisioningRunId.current) {
            return
          }

          setPhase1Status(snapshot.phase1)
          setPhase2Status(snapshot.phase2)
          setPhase3Status(snapshot.phase3)
        }
      )

      if (cancelled || runId !== provisioningRunId.current) {
        return
      }

      if (result.success) {
        setIsWorkspaceReady(true)
        return
      }

      setProvisioningError(result.message)
    })()

    return () => {
      cancelled = true
      if (provisioningRunId.current === runId) {
        provisioningRunId.current += 1
      }
    }
  }, [provisioningAttempt, runSetupAccount, step])

  const handleRetryProvisioning = () => {
    if (isProvisioningActive) {
      return
    }

    provisioningRunId.current += 1
    setupPromiseRef.current = null
    setProvisioningError(null)
    setIsWorkspaceReady(false)
    setPhase1Status("idle")
    setPhase2Status("idle")
    setPhase3Status("idle")
    setProvisioningAttempt((current) => current + 1)
  }

  const handleBackToRestaurant = () => {
    provisioningRunId.current += 1
    setupPromiseRef.current = null
    setProvisioningError(null)
    setIsWorkspaceReady(false)
    setPhase1Status("idle")
    setPhase2Status("idle")
    setPhase3Status("idle")
    setStep(2)
  }

  const handleContinueToLogin = () => {
    navigate("/login?setup=complete", { replace: true })
  }

  if (tokenLoading) {
    return <SetupAccountStatus title="Validating your setup link" />
  }

  if (tokenError) {
    return (
      <SetupAccountStatus
        tone="error"
        title="Invalid setup link"
        message={tokenError}
      />
    )
  }

  if (!prefill) {
    return (
      <SetupAccountStatus
        tone="error"
        title="Unable to load setup"
        message="We couldn't load your account setup details. Please open the link from your approval email again or contact support."
      />
    )
  }

  return (
    <Form {...form}>
      <WizardLiveValidationProvider attemptedFields={attemptedFields}>
        <GuestLoopShell
          showBackButton={step >= 2}
          backButtonDisabled={step === 3 && !provisioningError}
          onBack={step === 2 ? () => setStep(1) : handleBackToRestaurant}
        >
          {step === 1 ? (
            <GuestLoopPasswordStep
              form={form}
              activeStep={1}
              onContinue={handleContinueStep1}
            />
          ) : step === 2 ? (
            <GuestLoopRestaurantStep
              form={form}
              activeStep={2}
              onConfirm={handleConfirmRestaurantSubmit}
            />
          ) : (
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
              onOpenWorkspace={handleContinueToLogin}
              onRetry={handleRetryProvisioning}
            />
          )}
        </GuestLoopShell>
      </WizardLiveValidationProvider>
    </Form>
  )
}

export default RegisterSinglePage

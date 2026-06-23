import { useCallback, useEffect, useRef, useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import axiosInstance from "@/api/axiosInstance"
import { isAxiosError } from "axios"
import { useForm } from "react-hook-form"
import { useNavigate, useSearchParams } from "react-router-dom"

import { SetupAccountStatus } from "@/components/auth/SetupAccountShell"
import { GuestLoopGroupStep } from "@/components/guest-loop/GuestLoopGroupStep"
import { GuestLoopLocationsStep } from "@/components/guest-loop/GuestLoopLocationsStep"
import { GuestLoopPasswordStep } from "@/components/guest-loop/GuestLoopPasswordStep"
import { GuestLoopReadyStep } from "@/components/guest-loop/GuestLoopReadyStep"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import { GUEST_LOOP_MULTI_STEPS } from "@/components/guest-loop/guestLoopSteps"
import { WizardLiveValidationProvider } from "@/components/form/WizardLiveValidationContext"
import { useInvitePrefill } from "@/hooks/useInvitePrefill"
import { Form } from "@/components/ui/form"
import { addAttemptedFields, defaultFormValidationOptions } from "@/lib/form"
import {
  runProvisioningPhases,
  type ProvisioningPhaseStatus,
} from "@/lib/runProvisioningPhases"
import {
  accountSetupMultiDefaultValues,
  accountSetupMultiSchema,
  accountSetupMultiStep1Fields,
  accountSetupMultiStep1Schema,
  accountSetupMultiStep2Fields,
  getAccountSetupMultiStep3FieldNames,
  toMultiLocationSetupPayload,
  type AccountSetupMultiFormValues,
} from "@/schemas/accountSetupMulti"

interface SetupAccountResponse {
  success?: boolean
  message?: string
  errors?: unknown
}

const MULTI_PASSWORD_DESCRIPTION = (
  <>
    Your multi-location setup request has been approved.
    <br className="hidden sm:block" />
    <span className="sm:sr-only"> </span>
    Create a password to access your Tummly workspace.
  </>
)

function RegisterMultiPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const tokenFromParams = searchParams.get("token")?.trim() ?? ""
  const { token, tokenLoading, tokenError, prefill } =
    useInvitePrefill("Multi")

  const form = useForm<AccountSetupMultiFormValues>({
    resolver: zodResolver(accountSetupMultiSchema),
    defaultValues: {
      ...accountSetupMultiDefaultValues,
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
      ...accountSetupMultiDefaultValues,
      token,
      email: prefill.email,
      fullName: prefill.fullName,
      groupName: prefill.businessName,
      primaryPhone: prefill.mobile,
      businessCategory:
        prefill.businessCategory ||
        accountSetupMultiDefaultValues.businessCategory,
      numLocations:
        prefill.numLocations || accountSetupMultiDefaultValues.numLocations,
    })
  }, [form, prefill, token])

  const handleContinueStep1 = async () => {
    const fieldsToValidate = Array.from(accountSetupMultiStep1Fields) as Array<
      keyof AccountSetupMultiFormValues
    >
    const valid = await form.trigger(fieldsToValidate)
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupMultiStep1Fields)
      )
      return
    }
    setStep(2)
  }

  const handleConfirmGroupSubmit = async () => {
    const fieldsToValidate = Array.from(accountSetupMultiStep2Fields) as Array<
      keyof AccountSetupMultiFormValues
    >
    const valid = await form.trigger(fieldsToValidate)
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupMultiStep2Fields)
      )
      return
    }
    setStep(3)
  }

  const handleContinueLocations = async () => {
    const locationCount = form.getValues("locations").length
    const fieldsToValidate = getAccountSetupMultiStep3FieldNames(locationCount)
    const valid = await form.trigger(fieldsToValidate)
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, fieldsToValidate)
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
    setStep(4)
  }

  const runSetupAccount = useCallback(async () => {
    if (setupPromiseRef.current) {
      return setupPromiseRef.current
    }

    const executeSetup = async () => {
      const step1Valid = await form.trigger(
        Array.from(accountSetupMultiStep1Fields) as Array<
          keyof AccountSetupMultiFormValues
        >
      )
      const step2Valid = await form.trigger(
        Array.from(accountSetupMultiStep2Fields) as Array<
          keyof AccountSetupMultiFormValues
        >
      )
      const locationCount = form.getValues("locations").length
      const step3Valid = await form.trigger(
        getAccountSetupMultiStep3FieldNames(locationCount)
      )

      if (!step1Valid || !step2Valid || !step3Valid) {
        throw new Error("Please complete all required fields.")
      }

      const values = form.getValues()

      try {
        const response = await axiosInstance.post<SetupAccountResponse>(
          "/auth/setup-account",
          toMultiLocationSetupPayload(values),
          { skipAuthRedirect: true }
        )

        if (response.data.success) {
          return
        }

        throw new Error(response.data.message || "Account setup failed.")
      } catch (error: unknown) {
        if (isAxiosError<SetupAccountResponse>(error)) {
          if (error.response?.status === 409) {
            return
          }

          const message = error.response?.data?.message

          throw new Error(
            message || "Something went wrong during onboarding processing.",
            { cause: error }
          )
        }

        if (error instanceof Error) {
          throw error
        }

        throw new Error("Something went wrong during onboarding processing.", {
          cause: error,
        })
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
    if (step !== 4) {
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

  const resetProvisioningState = () => {
    provisioningRunId.current += 1
    setupPromiseRef.current = null
    setProvisioningError(null)
    setIsWorkspaceReady(false)
    setPhase1Status("idle")
    setPhase2Status("idle")
    setPhase3Status("idle")
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
      resetProvisioningState()
      setStep(3)
    }
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
          contentAlign={step === 4 ? "start" : "center"}
          showBackButton={step >= 2}
          backButtonDisabled={step === 4 && !provisioningError}
          onBack={handleBack}
        >
          {step === 1 ? (
            <GuestLoopPasswordStep
              form={form}
              activeStep={1}
              steps={GUEST_LOOP_MULTI_STEPS}
              step1Fields={accountSetupMultiStep1Fields}
              step1Schema={accountSetupMultiStep1Schema}
              description={MULTI_PASSWORD_DESCRIPTION}
              submitLabel="Create account"
              onContinue={handleContinueStep1}
            />
          ) : step === 2 ? (
            <GuestLoopGroupStep
              form={form}
              activeStep={2}
              onConfirm={handleConfirmGroupSubmit}
            />
          ) : step === 3 ? (
            <GuestLoopLocationsStep
              form={form}
              activeStep={3}
              onContinue={handleContinueLocations}
            />
          ) : (
            <GuestLoopReadyStep
              activeStep={4}
              steps={GUEST_LOOP_MULTI_STEPS}
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

export default RegisterMultiPage

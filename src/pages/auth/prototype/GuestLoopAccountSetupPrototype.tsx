/**
 * PROTOTYPE — Guest Loop Account Setup QA walkthrough for single + multi flows.
 *
 * Question this answers: can we step through every wizard screen with production
 * components and QA-like prefill without hitting the real setup API?
 *
 * Usage:
 *   /prototype/account-setup
 *   /prototype/account-setup?type=multi&step=3
 *   /prototype/account-setup?type=single&step=3&ready=error
 *
 * Delete when done reviewing.
 */
import { useCallback, useEffect, useRef, useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { Navigate, useSearchParams } from "react-router-dom"
import { ZodError } from "zod"

import { GuestLoopGroupStep } from "@/components/guest-loop/GuestLoopGroupStep"
import { GuestLoopLocationsStep } from "@/components/guest-loop/GuestLoopLocationsStep"
import { GuestLoopPasswordStep } from "@/components/guest-loop/GuestLoopPasswordStep"
import { GuestLoopReadyStep } from "@/components/guest-loop/GuestLoopReadyStep"
import { GuestLoopRestaurantStep } from "@/components/guest-loop/GuestLoopRestaurantStep"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import {
  GUEST_LOOP_MULTI_STEPS,
} from "@/components/guest-loop/guestLoopSteps"
import { WizardLiveValidationProvider } from "@/components/form/WizardLiveValidationContext"
import { AUTH_API_BASE_URL } from "@/config/api"
import { Form } from "@/components/ui/form"
import { addAttemptedFields, defaultFormValidationOptions } from "@/lib/form"
import {
  runProvisioningPhases,
  type ProvisioningPhaseStatus,
} from "@/lib/runProvisioningPhases"
import {
  accountSetupMultiSchema,
  accountSetupMultiStep1Fields,
  accountSetupMultiStep1Schema,
  accountSetupMultiStep2Fields,
  getAccountSetupMultiStep3FieldNames,
  toMultiLocationSetupPayload,
  type AccountSetupMultiFormValues,
} from "@/schemas/accountSetupMulti"
import {
  accountSetupSingleSchema,
  accountSetupSingleStep1Fields,
  accountSetupSingleStep2Fields,
  toSingleLocationSetupPayload,
  type AccountSetupSingleFormValues,
} from "@/schemas/accountSetupSingle"

import {
  GuestLoopAccountSetupPrototypeBar,
  type PrototypeAccountType,
  type PrototypeReadyMode,
} from "./GuestLoopAccountSetupPrototypeBar"
import {
  buildPrototypeMultiFormValues,
  buildPrototypePrefillFormValues,
  buildPrototypeSingleFormValues,
  MULTI_QA_PREFILL,
  SINGLE_QA_PREFILL,
} from "./guestLoopAccountSetupPrototypeData"
import { loadPrototypeFormDraft, savePrototypeFormDraft } from "./guestLoopAccountSetupPrototypeStorage"

const MULTI_PASSWORD_DESCRIPTION = (
  <>
    Your multi-location setup request has been approved.
    <br className="hidden sm:block" />
    <span className="sm:sr-only"> </span>
    Create a password to access your Tummly workspace.
  </>
)

const SINGLE_STEP_LABELS = ["Account", "Restaurant", "Ready"] as const
const MULTI_STEP_LABELS = ["Account", "Group", "Locations", "Ready"] as const

function parseAccountType(value: string | null): PrototypeAccountType {
  return value === "multi" ? "multi" : "single"
}

function parseReadyMode(value: string | null): PrototypeReadyMode {
  if (value === "complete" || value === "error") {
    return value
  }
  return "animate"
}

function clampStep(step: number, maxStep: number) {
  if (!Number.isFinite(step) || step < 1) {
    return 1
  }
  if (step > maxStep) {
    return maxStep
  }
  return step
}

function sleep(ms: number) {
  return new Promise<void>((resolve) => {
    window.setTimeout(resolve, ms)
  })
}

function formatPrototypeSetupError(error: unknown) {
  if (error instanceof ZodError) {
    const fields = [
      ...new Set(error.issues.map((issue) => issue.path.join(".") || "form")),
    ].join(", ")
    return `Setup payload is incomplete (${fields}). Go back to Step 1 and re-enter your password, or use "Seed all valid fields".`
  }

  if (error instanceof Error && error.message.trim()) {
    return error.message.trim()
  }

  return "Something went wrong during onboarding processing."
}

async function mockSetupAccountRequest(
  label: string,
  payload: unknown,
  shouldFail = false
) {
  const endpoint = `${AUTH_API_BASE_URL}/setup-account`
  console.group(`[PROTOTYPE] ${label}`)
  console.log("POST", endpoint)
  console.log("payload", payload)
  console.log("validate-setup-token (skipped — using QA prefill)")
  await sleep(600)
  if (shouldFail) {
    console.log("response", { success: false, message: "Prototype forced error" })
    console.groupEnd()
    throw new Error("Prototype forced provisioning error")
  }
  console.log("response", { success: true })
  console.groupEnd()
}

function buildInitialSingleFormValues(): AccountSetupSingleFormValues {
  return {
    ...buildPrototypePrefillFormValues("Single"),
    ...loadPrototypeFormDraft("single"),
  }
}

function buildInitialMultiFormValues(): AccountSetupMultiFormValues {
  return {
    ...buildPrototypePrefillFormValues("Multi"),
    ...loadPrototypeFormDraft("multi"),
  }
}

function GuestLoopAccountSetupPrototype() {
  const [searchParams, setSearchParams] = useSearchParams()
  const accountType = parseAccountType(searchParams.get("type"))
  const maxStep = accountType === "multi" ? 4 : 3
  const step = clampStep(Number(searchParams.get("step") ?? "1"), maxStep)
  const readyMode = parseReadyMode(searchParams.get("ready"))

  const isMulti = accountType === "multi"
  const stepLabels = isMulti ? MULTI_STEP_LABELS : SINGLE_STEP_LABELS

  const singleForm = useForm<AccountSetupSingleFormValues>({
    resolver: zodResolver(accountSetupSingleSchema),
    defaultValues: buildInitialSingleFormValues(),
    shouldUnregister: false,
    ...defaultFormValidationOptions,
  })

  const multiForm = useForm<AccountSetupMultiFormValues>({
    resolver: zodResolver(accountSetupMultiSchema),
    defaultValues: buildInitialMultiFormValues(),
    shouldUnregister: false,
    ...defaultFormValidationOptions,
  })

  const activeForm = isMulti ? multiForm : singleForm

  const [attemptedFields, setAttemptedFields] = useState<Set<string>>(new Set())
  const [phase1Status, setPhase1Status] = useState<ProvisioningPhaseStatus>("idle")
  const [phase2Status, setPhase2Status] = useState<ProvisioningPhaseStatus>("idle")
  const [phase3Status, setPhase3Status] = useState<ProvisioningPhaseStatus>("idle")
  const [isWorkspaceReady, setIsWorkspaceReady] = useState(false)
  const [provisioningError, setProvisioningError] = useState<string | null>(null)
  const [provisioningAttempt, setProvisioningAttempt] = useState(0)
  const provisioningRunId = useRef(0)
  const setupPromiseRef = useRef<Promise<void> | null>(null)

  const readyStep = maxStep
  const isProvisioningActive =
    phase1Status === "loading" ||
    phase2Status === "loading" ||
    phase3Status === "loading"

  const updateSearchParams = useCallback(
    (updates: Record<string, string | null>) => {
      setSearchParams(
        (current) => {
          const next = new URLSearchParams(current)
          for (const [key, value] of Object.entries(updates)) {
            if (value === null) {
              next.delete(key)
            } else {
              next.set(key, value)
            }
          }
          return next
        },
        { replace: true }
      )
    },
    [setSearchParams]
  )

  const resetProvisioningState = useCallback(() => {
    provisioningRunId.current += 1
    setupPromiseRef.current = null
    setProvisioningError(null)
    setIsWorkspaceReady(false)
    setPhase1Status("idle")
    setPhase2Status("idle")
    setPhase3Status("idle")
  }, [])

  const logPrototypeState = useCallback(
    (event: string, extra?: Record<string, unknown>) => {
      console.group(`[PROTOTYPE] ${event}`)
      console.log("accountType", accountType)
      console.log("step", step)
      console.log("readyMode", readyMode)
      console.log("formValues", activeForm.getValues())
      if (extra) {
        console.log("extra", extra)
      }
      console.groupEnd()
    },
    [accountType, activeForm, readyMode, step]
  )

  useEffect(() => {
    logPrototypeState("state")
  }, [accountType, step, readyMode, logPrototypeState])

  useEffect(() => {
    const saveSingleDraft = singleForm.watch((values) => {
      savePrototypeFormDraft("single", values)
    })
    const saveMultiDraft = multiForm.watch((values) => {
      savePrototypeFormDraft("multi", values)
    })

    return () => {
      saveSingleDraft.unsubscribe()
      saveMultiDraft.unsubscribe()
    }
  }, [multiForm, singleForm])

  useEffect(() => {
    if (step !== 1) {
      return
    }

    const draft = loadPrototypeFormDraft(isMulti ? "multi" : "single")
    if (!draft?.password) {
      return
    }

    const form = isMulti ? multiForm : singleForm
    const values = form.getValues()

    if (values.password) {
      return
    }

    form.reset(
      {
        ...values,
        password: draft.password,
        confirmPassword: draft.confirmPassword ?? "",
        agree: draft.agree ?? values.agree,
      },
      { keepDirtyValues: true }
    )
  }, [isMulti, multiForm, singleForm, step])

  const handleAccountTypeChange = (type: PrototypeAccountType) => {
    const nextMaxStep = type === "multi" ? 4 : 3
    const nextStep = clampStep(step, nextMaxStep)
    resetProvisioningState()
    updateSearchParams({
      type,
      step: String(nextStep),
      ready: nextStep === nextMaxStep ? readyMode : null,
    })
    if (type === "multi") {
      multiForm.reset(buildPrototypePrefillFormValues("Multi"))
    } else {
      singleForm.reset(buildPrototypePrefillFormValues("Single"))
    }
  }

  const handleStepChange = (nextStep: number) => {
    resetProvisioningState()
    updateSearchParams({
      step: String(clampStep(nextStep, maxStep)),
      ready: clampStep(nextStep, maxStep) === readyStep ? readyMode : null,
    })
  }

  const handleReadyModeChange = (mode: PrototypeReadyMode) => {
    resetProvisioningState()
    updateSearchParams({ ready: mode, step: String(readyStep) })
  }

  const handleSeedPrefill = () => {
    if (isMulti) {
      multiForm.reset(buildPrototypePrefillFormValues("Multi"))
    } else {
      singleForm.reset(buildPrototypePrefillFormValues("Single"))
    }
    logPrototypeState("seeded token prefill", {
      prefill: isMulti ? MULTI_QA_PREFILL : SINGLE_QA_PREFILL,
    })
  }

  const handleSeedAll = () => {
    if (isMulti) {
      multiForm.reset(buildPrototypeMultiFormValues())
    } else {
      singleForm.reset(buildPrototypeSingleFormValues())
    }
    logPrototypeState("seeded all valid fields")
  }

  const handleLogPayload = () => {
    const payload = isMulti
      ? toMultiLocationSetupPayload(multiForm.getValues())
      : toSingleLocationSetupPayload(singleForm.getValues())
    void mockSetupAccountRequest("manual payload preview", payload)
  }

  const runSingleSetupAccount = useCallback(async () => {
    if (setupPromiseRef.current) {
      return setupPromiseRef.current
    }

    const executeSetup = async () => {
      try {
        const values = singleForm.getValues()
        await mockSetupAccountRequest(
          "single setup-account",
          toSingleLocationSetupPayload(values),
          readyMode === "error"
        )
      } catch (error) {
        throw new Error(formatPrototypeSetupError(error), { cause: error })
      }
    }

    const promise = executeSetup().finally(() => {
      if (setupPromiseRef.current === promise) {
        setupPromiseRef.current = null
      }
    })

    setupPromiseRef.current = promise
    return promise
  }, [readyMode, singleForm])

  const runMultiSetupAccount = useCallback(async () => {
    if (setupPromiseRef.current) {
      return setupPromiseRef.current
    }

    const executeSetup = async () => {
      try {
        const values = multiForm.getValues()
        await mockSetupAccountRequest(
          "multi setup-account",
          toMultiLocationSetupPayload(values),
          readyMode === "error"
        )
      } catch (error) {
        throw new Error(formatPrototypeSetupError(error), { cause: error })
      }
    }

    const promise = executeSetup().finally(() => {
      if (setupPromiseRef.current === promise) {
        setupPromiseRef.current = null
      }
    })

    setupPromiseRef.current = promise
    return promise
  }, [multiForm, readyMode])

  useEffect(() => {
    if (step !== readyStep) {
      return
    }

    if (readyMode === "complete") {
      setProvisioningError(null)
      setIsWorkspaceReady(true)
      setPhase1Status("success")
      setPhase2Status("success")
      setPhase3Status("success")
      return
    }

    if (readyMode === "error") {
      setIsWorkspaceReady(false)
      setPhase1Status("idle")
      setPhase2Status("idle")
      setPhase3Status("idle")
      setProvisioningError("Prototype forced provisioning error")
      return
    }

    const runId = ++provisioningRunId.current
    let cancelled = false

    void (async () => {
      setProvisioningError(null)
      setIsWorkspaceReady(false)

      const result = await runProvisioningPhases(
        () => (isMulti ? runMultiSetupAccount() : runSingleSetupAccount()),
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
  }, [
    isMulti,
    provisioningAttempt,
    readyMode,
    readyStep,
    runMultiSetupAccount,
    runSingleSetupAccount,
    step,
  ])

  const handleContinueStep1 = async () => {
    if (isMulti) {
      const valid = await multiForm.trigger(
        Array.from(accountSetupMultiStep1Fields)
      )
      if (!valid) {
        setAttemptedFields((current) =>
          addAttemptedFields(current, accountSetupMultiStep1Fields)
        )
        return
      }
      handleStepChange(2)
      return
    }

    const valid = await singleForm.trigger(
      Array.from(accountSetupSingleStep1Fields)
    )
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupSingleStep1Fields)
      )
      return
    }
    handleStepChange(2)
  }

  const handleContinueStep2 = async () => {
    if (isMulti) {
      const valid = await multiForm.trigger(
        Array.from(accountSetupMultiStep2Fields)
      )
      if (!valid) {
        setAttemptedFields((current) =>
          addAttemptedFields(current, accountSetupMultiStep2Fields)
        )
        return
      }
      handleStepChange(3)
      return
    }

    const valid = await singleForm.trigger(
      Array.from(accountSetupSingleStep2Fields)
    )
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupSingleStep2Fields)
      )
      return
    }

    resetProvisioningState()
    updateSearchParams({ step: "3", ready: "animate" })
  }

  const handleContinueLocations = async () => {
    const locationCount = multiForm.getValues("locations").length
    const fieldsToValidate = getAccountSetupMultiStep3FieldNames(locationCount)
    const valid = await multiForm.trigger(fieldsToValidate)
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, fieldsToValidate)
      )
      return
    }

    resetProvisioningState()
    updateSearchParams({ step: "4", ready: "animate" })
  }

  const handleRetryProvisioning = () => {
    if (isProvisioningActive) {
      return
    }
    resetProvisioningState()
    updateSearchParams({ ready: "animate" })
    setProvisioningAttempt((current) => current + 1)
  }

  const handleBack = () => {
    if (step <= 1) {
      return
    }

    if (step === readyStep) {
      resetProvisioningState()
    }

    handleStepChange(step - 1)
  }

  const handleOpenWorkspace = () => {
    console.log("[PROTOTYPE] navigate → /login?setup=complete")
    logPrototypeState("open workspace")
  }

  const shellProps = {
    showBackButton: step >= 2,
    backButtonDisabled: step === readyStep && !provisioningError,
    onBack: handleBack,
  }

  return (
    <>
      {isMulti ? (
        <Form {...multiForm}>
          <WizardLiveValidationProvider attemptedFields={attemptedFields}>
            <GuestLoopShell {...shellProps}>
              {step === 1 ? (
                <GuestLoopPasswordStep
                  form={multiForm}
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
                  form={multiForm}
                  activeStep={2}
                  onConfirm={handleContinueStep2}
                />
              ) : step === 3 ? (
                <GuestLoopLocationsStep
                  form={multiForm}
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
                  onOpenWorkspace={handleOpenWorkspace}
                  onRetry={handleRetryProvisioning}
                />
              )}
            </GuestLoopShell>
          </WizardLiveValidationProvider>
        </Form>
      ) : (
        <Form {...singleForm}>
          <WizardLiveValidationProvider attemptedFields={attemptedFields}>
            <GuestLoopShell {...shellProps}>
              {step === 1 ? (
                <GuestLoopPasswordStep
                  form={singleForm}
                  activeStep={1}
                  onContinue={handleContinueStep1}
                />
              ) : step === 2 ? (
                <GuestLoopRestaurantStep
                  form={singleForm}
                  activeStep={2}
                  onConfirm={handleContinueStep2}
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
                  onOpenWorkspace={handleOpenWorkspace}
                  onRetry={handleRetryProvisioning}
                />
              )}
            </GuestLoopShell>
          </WizardLiveValidationProvider>
        </Form>
      )}

      <GuestLoopAccountSetupPrototypeBar
        accountType={accountType}
        step={step}
        maxStep={maxStep}
        readyMode={readyMode}
        stepLabels={stepLabels}
        onAccountTypeChange={handleAccountTypeChange}
        onStepChange={handleStepChange}
        onReadyModeChange={handleReadyModeChange}
        onSeedPrefill={handleSeedPrefill}
        onSeedAll={handleSeedAll}
        onLogPayload={handleLogPayload}
      />
    </>
  )
}

export default function GuestLoopAccountSetupPrototypePage() {
  if (import.meta.env.PROD) {
    return <Navigate to="/" replace />
  }

  return <GuestLoopAccountSetupPrototype />
}

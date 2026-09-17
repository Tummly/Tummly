import { useEffect, useState, type ReactNode } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { isAxiosError } from "axios"
import { useForm } from "react-hook-form"
import { useNavigate } from "react-router-dom"
import { z } from "zod"

import {
  getSignupSession,
  saveSignupOnboarding,
  type SignupOnboardingPayload,
} from "@/api/signupApi"
import { SetupAccountStatus } from "@/components/auth/SetupAccountShell"
import { WizardLiveValidationProvider } from "@/components/form/WizardLiveValidationContext"
import { GuestLoopGroupStep } from "@/components/guest-loop/GuestLoopGroupStep"
import { GuestLoopLocationsStep } from "@/components/guest-loop/GuestLoopLocationsStep"
import { GuestLoopPasswordStep } from "@/components/guest-loop/GuestLoopPasswordStep"
import { GuestLoopReadyTeaserStep } from "@/components/guest-loop/GuestLoopReadyTeaserStep"
import { GuestLoopRestaurantStep } from "@/components/guest-loop/GuestLoopRestaurantStep"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import {
  GUEST_LOOP_MULTI_STEPS,
  GUEST_LOOP_SINGLE_STEPS,
} from "@/components/guest-loop/guestLoopSteps"
import { validateWizardStep } from "@/components/guest-loop/useGuestLoopStepCanSubmit"
import { Form } from "@/components/ui/form"
import { addAttemptedFields, defaultFormValidationOptions } from "@/lib/form"
import { readSignupSessionToken } from "@/lib/signupSession"
import {
  accountSetupMultiDefaultValues,
  accountSetupMultiSchema,
  accountSetupMultiStep2Fields,
  accountSetupMultiStep2Schema,
  accountSetupMultiStep3Schema,
  emptyLocationItem,
  getAccountSetupMultiStep3FieldNames,
  toMultiLocationSetupPayload,
  type AccountSetupMultiFormValues,
} from "@/schemas/accountSetupMulti"
import {
  accountSetupSingleDefaultValues,
  accountSetupSingleSchema,
  accountSetupSingleStep1Fields,
  accountSetupSingleStep1Schema,
  accountSetupSingleStep2Fields,
  accountSetupSingleStep2Schema,
  toSingleLocationSetupPayload,
  type AccountSetupSingleFormValues,
} from "@/schemas/accountSetupSingle"
import { validationMessages } from "@/schemas/messages"

const SIGNUP_PASSWORD_DESCRIPTION = (
  <>
    Your email is verified.
    <br className="hidden sm:block" />
    <span className="sm:sr-only"> </span>
    Create a password to continue setting up your Tummly workspace.
  </>
)

const POST_ONBOARDING_STATUSES = new Set([
  "OnboardingComplete",
  "AwaitingPayment",
  "Provisioning",
])

const WIZARD_STATUSES = new Set(["Verified", "OnboardingComplete"])

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

function isSingleLocationCount(numLocations: string | undefined) {
  return (numLocations ?? "").trim() === "1"
}

const restaurantWithLocationCountSchema = accountSetupSingleStep2Schema.and(
  z.object({
    numLocations: z
      .string()
      .min(1, validationMessages.accountSetup.numLocations.required),
  })
)

const restaurantWithLocationCountFields = [
  ...accountSetupSingleStep2Fields,
  "numLocations",
] as const

function toSignupPayloadFromSingle(
  values: AccountSetupSingleFormValues
): SignupOnboardingPayload {
  const setup = toSingleLocationSetupPayload(values)
  return {
    password: setup.password,
    confirmPassword: setup.confirmPassword,
    fullName: setup.fullName,
    groupName: setup.groupName,
    businessCategory: setup.businessCategory,
    primaryPhone: setup.primaryPhone,
    businessLink: setup.businessLink,
    locations: setup.locations,
  }
}

function toSignupPayloadFromMulti(
  values: AccountSetupMultiFormValues
): SignupOnboardingPayload {
  const setup = toMultiLocationSetupPayload(values)
  return {
    password: setup.password,
    confirmPassword: setup.confirmPassword,
    fullName: setup.fullName,
    groupName: setup.groupName,
    businessCategory: setup.businessCategory,
    primaryPhone: setup.primaryPhone,
    businessLink: setup.businessLink,
    locations: setup.locations,
  }
}

type AccountPath = "single" | "multi"

function SignupOnboardingPage() {
  const navigate = useNavigate()
  const sessionToken = readSignupSessionToken()

  const [gateState, setGateState] = useState<"loading" | "ready" | "error">(
    "loading"
  )
  const [gateError, setGateError] = useState<string | null>(null)

  const [path, setPath] = useState<AccountPath>("single")
  const [step, setStep] = useState(1)
  const [attemptedFields, setAttemptedFields] = useState<Set<string>>(new Set())
  const [isSaving, setIsSaving] = useState(false)

  const singleForm = useForm<AccountSetupSingleFormValues>({
    resolver: zodResolver(accountSetupSingleSchema),
    defaultValues: accountSetupSingleDefaultValues,
    ...defaultFormValidationOptions,
  })

  const multiForm = useForm<AccountSetupMultiFormValues>({
    resolver: zodResolver(accountSetupMultiSchema),
    defaultValues: accountSetupMultiDefaultValues,
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

        if (POST_ONBOARDING_STATUSES.has(session.status)) {
          navigate("/signup/choose-plan", { replace: true })
          return
        }

        if (!WIZARD_STATUSES.has(session.status)) {
          navigate("/signup", { replace: true })
          return
        }

        singleForm.reset({
          ...accountSetupSingleDefaultValues,
          token: sessionToken,
          email: session.email,
          fullName: session.fullName ?? "",
          restaurantName: session.groupName ?? "",
          businessCategory:
            session.businessCategory ||
            accountSetupSingleDefaultValues.businessCategory,
          phone: session.primaryPhone ?? "",
          businessLink: session.businessLink ?? "",
        })
        multiForm.reset({
          ...accountSetupMultiDefaultValues,
          token: sessionToken,
          email: session.email,
          fullName: session.fullName ?? "",
          groupName: session.groupName ?? "",
          businessCategory:
            session.businessCategory ||
            accountSetupMultiDefaultValues.businessCategory,
          primaryPhone: session.primaryPhone ?? "",
          businessLink: session.businessLink ?? "",
        })
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
    // One-shot session gate; form instances are stable for this page.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessionToken, navigate])

  const progressSteps =
    path === "multi" ? GUEST_LOOP_MULTI_STEPS : GUEST_LOOP_SINGLE_STEPS

  const handleContinuePassword = () => {
    const fieldsToValidate = Array.from(accountSetupSingleStep1Fields)
    const valid = validateWizardStep(
      singleForm,
      fieldsToValidate,
      accountSetupSingleStep1Schema
    )
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupSingleStep1Fields)
      )
      return
    }

    const values = singleForm.getValues()
    multiForm.setValue("email", values.email)
    multiForm.setValue("fullName", values.fullName)
    multiForm.setValue("password", values.password)
    multiForm.setValue("confirmPassword", values.confirmPassword)
    multiForm.setValue("agree", values.agree)
    multiForm.setValue("token", values.token)

    setStep(2)
  }

  const persistOnboarding = async (
    payload: SignupOnboardingPayload,
    readyStep: number
  ) => {
    if (!sessionToken) {
      navigate("/signup", { replace: true })
      return
    }

    setIsSaving(true)

    try {
      await saveSignupOnboarding(sessionToken, payload)
      setStep(readyStep)
    } catch (error) {
      const message = getApiErrorMessage(
        error,
        "We couldn't save your details. Please try again."
      )
      singleForm.setError("root", { type: "server", message })
      multiForm.setError("root", { type: "server", message })
    } finally {
      setIsSaving(false)
    }
  }

  const handleConfirmRestaurant = async () => {
    const fieldsToValidate = Array.from(restaurantWithLocationCountFields)
    const valid = validateWizardStep(
      singleForm,
      fieldsToValidate,
      restaurantWithLocationCountSchema
    )
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, restaurantWithLocationCountFields)
      )
      return
    }

    const values = singleForm.getValues()

    if (isSingleLocationCount(values.numLocations)) {
      setPath("single")
      await persistOnboarding(toSignupPayloadFromSingle(values), 3)
      return
    }

    setPath("multi")
    multiForm.reset({
      ...accountSetupMultiDefaultValues,
      token: values.token || sessionToken || "",
      email: values.email,
      fullName: values.fullName,
      password: values.password,
      confirmPassword: values.confirmPassword,
      agree: values.agree,
      groupName: values.restaurantName,
      businessCategory: values.businessCategory,
      numLocations: values.numLocations || "",
      primaryPhone: values.phone,
      businessLink: values.businessLink,
      locations: [
        {
          ...emptyLocationItem,
          locationName: values.locationName,
          address: values.address,
          city: values.city,
          postcode: values.postcode,
          addressOverridden: values.addressOverridden,
          locationPhone: values.phone,
          localContact: values.fullName,
        },
      ],
    })
    setStep(2)
  }

  const handleConfirmGroup = () => {
    const fieldsToValidate = Array.from(accountSetupMultiStep2Fields)
    const valid = validateWizardStep(
      multiForm,
      fieldsToValidate,
      accountSetupMultiStep2Schema
    )
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, accountSetupMultiStep2Fields)
      )
      return
    }
    setStep(3)
  }

  const handleContinueLocations = async () => {
    const locationCount = multiForm.getValues("locations").length
    const fieldsToValidate = getAccountSetupMultiStep3FieldNames(locationCount)
    const valid = validateWizardStep(
      multiForm,
      fieldsToValidate,
      accountSetupMultiStep3Schema,
      {
        selectStepValues: (values) => ({ locations: values.locations }),
      }
    )
    if (!valid) {
      setAttemptedFields((current) =>
        addAttemptedFields(current, fieldsToValidate)
      )
      return
    }

    await persistOnboarding(toSignupPayloadFromMulti(multiForm.getValues()), 4)
  }

  const handleBack = () => {
    if (path === "single") {
      if (step === 2) {
        setStep(1)
        return
      }
      if (step === 3) {
        setStep(2)
      }
      return
    }

    if (step === 2) {
      setPath("single")
      setStep(2)
      return
    }

    if (step === 3) {
      setStep(2)
      return
    }

    if (step === 4) {
      setStep(3)
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

  const showMultiSteps = path === "multi" && step >= 2
  const activeForm = showMultiSteps ? multiForm : singleForm

  let stepContent: ReactNode

  if (!showMultiSteps) {
    if (step === 1) {
      stepContent = (
        <GuestLoopPasswordStep
          form={singleForm}
          activeStep={1}
          steps={GUEST_LOOP_SINGLE_STEPS}
          description={SIGNUP_PASSWORD_DESCRIPTION}
          submitLabel="Continue"
          onContinue={handleContinuePassword}
        />
      )
    } else if (step === 2) {
      stepContent = (
        <GuestLoopRestaurantStep
          form={singleForm}
          activeStep={2}
          steps={progressSteps}
          showLocationCount
          onConfirm={handleConfirmRestaurant}
          isSubmitting={isSaving}
        />
      )
    } else {
      stepContent = (
        <GuestLoopReadyTeaserStep
          activeStep={3}
          steps={GUEST_LOOP_SINGLE_STEPS}
          onContinue={() => navigate("/signup/choose-plan")}
        />
      )
    }
  } else if (step === 2) {
    stepContent = (
      <GuestLoopGroupStep
        form={multiForm}
        activeStep={2}
        onConfirm={handleConfirmGroup}
      />
    )
  } else if (step === 3) {
    stepContent = (
      <GuestLoopLocationsStep
        form={multiForm}
        activeStep={3}
        submitLabel="Continue"
        onContinue={handleContinueLocations}
        isSubmitting={isSaving}
      />
    )
  } else {
    stepContent = (
      <GuestLoopReadyTeaserStep
        activeStep={4}
        steps={GUEST_LOOP_MULTI_STEPS}
        onContinue={() => navigate("/signup/choose-plan")}
      />
    )
  }

  return (
    <Form {...activeForm}>
      <WizardLiveValidationProvider attemptedFields={attemptedFields}>
        <GuestLoopShell
          contentAlign="center"
          showBackButton={step >= 2}
          onBack={handleBack}
        >
          {stepContent}
        </GuestLoopShell>
      </WizardLiveValidationProvider>
    </Form>
  )
}

export default SignupOnboardingPage

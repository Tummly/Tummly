import { useEffect } from "react"
import type { UseFormReturn } from "react-hook-form"

import { PasswordStrengthMeter } from "@/components/auth/PasswordStrengthMeter"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { GuestLoopSignupProgress } from "@/components/guest-loop/GuestLoopSignupProgress"
import {
  useGuestLoopStepCanSubmit,
  useGuestLoopStepValidationFeedback,
} from "@/components/guest-loop/useGuestLoopStepCanSubmit"
import { Button } from "@/components/ui/button"
import { PASSWORD_REQUIREMENTS_HINT } from "@/constants/passwordCopy"
import {
  signupAccountStepFields,
  signupAccountStepSchema,
  type SignupOnboardingFormValues,
} from "@/schemas/signupOnboarding"

type SignupAccountStepProps = {
  form: UseFormReturn<SignupOnboardingFormValues>
  onContinue: () => void | Promise<void>
  isSubmitting?: boolean
}

export function SignupAccountStep({
  form,
  onContinue,
  isSubmitting = false,
}: SignupAccountStepProps) {
  const password = form.watch("password")
  const canContinue = useGuestLoopStepCanSubmit(
    form,
    signupAccountStepFields,
    signupAccountStepSchema
  )

  useGuestLoopStepValidationFeedback(
    form,
    signupAccountStepFields,
    signupAccountStepSchema,
    canContinue,
    {
      shouldSkipValidationFeedback: (fieldPath) =>
        fieldPath === "password" || fieldPath === "confirmPassword",
    }
  )

  useEffect(() => {
    const subscription = form.watch((_value, { name, type }) => {
      if (type !== "change" || name !== "password") {
        return
      }

      if (
        !form.getValues("confirmPassword") ||
        !form.getFieldState("confirmPassword").isTouched
      ) {
        return
      }

      const stepValues = Object.fromEntries(
        signupAccountStepFields.map((field) => [field, form.getValues(field)])
      )
      const result = signupAccountStepSchema.safeParse(stepValues)

      if (result.success) {
        form.clearErrors("confirmPassword")
        return
      }

      const confirmIssue = result.error.issues.find(
        (issue) => String(issue.path[0]) === "confirmPassword"
      )

      if (confirmIssue) {
        form.setError("confirmPassword", {
          type: "custom",
          message: confirmIssue.message,
        })
      } else {
        form.clearErrors("confirmPassword")
      }
    })

    return () => subscription.unsubscribe()
  }, [form])

  const isDisabled = !canContinue || isSubmitting

  return (
    <div className="flex w-full flex-col gap-[50px]">
      <GuestLoopSignupProgress
        activeStep={1}
        title="Set up your account"
        description="Add your details and create a password for your Tummly account."
      />

      <div className="flex flex-col gap-9">
        <div className="flex flex-col gap-[18px]">
          <FormFloatingInput
            control={form.control}
            name="email"
            type="email"
            label="Work email"
            autoComplete="email"
            readOnly
          />

          <FormFloatingInput
            control={form.control}
            name="firstName"
            label="First name"
            autoComplete="given-name"
            required
          />

          <FormFloatingInput
            control={form.control}
            name="lastName"
            label="Last name"
            autoComplete="family-name"
            required
          />

          <div className="flex flex-col gap-3">
            <FormFloatingInput
              control={form.control}
              name="password"
              type="password"
              label="Password"
              autoComplete="new-password"
              required
              blurThenLiveValidate
            />

            <PasswordStrengthMeter password={password ?? ""} />

            <p className="m-0 text-sm font-medium tracking-[-0.28px] text-[#232323]">
              {PASSWORD_REQUIREMENTS_HINT}
            </p>
          </div>

          <FormFloatingInput
            control={form.control}
            name="confirmPassword"
            type="password"
            label="Confirm password"
            autoComplete="new-password"
            required
            blurThenLiveValidate
          />
        </div>

        <Button
          type="button"
          variant="default"
          disabled={isDisabled}
          onClick={() => void onContinue()}
          className="h-11 w-auto rounded-[4px] px-[18px] py-3 text-sm font-medium leading-[22px] shadow-none"
        >
          {isSubmitting ? "Please wait..." : "Continue"}
        </Button>
      </div>
    </div>
  )
}

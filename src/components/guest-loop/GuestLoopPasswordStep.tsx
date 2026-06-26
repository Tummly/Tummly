import { useEffect, type ReactNode } from "react"
import type { FieldPath, UseFormReturn } from "react-hook-form"
import { Link } from "react-router-dom"
import type { z } from "zod"

import { PasswordStrengthMeter } from "@/components/auth/PasswordStrengthMeter"
import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { PASSWORD_REQUIREMENTS_HINT } from "@/constants/passwordCopy"
import type { AccountSetupMultiFormValues } from "@/schemas/accountSetupMulti"
import {
  accountSetupSingleStep1Fields,
  accountSetupSingleStep1Schema,
  type AccountSetupSingleFormValues,
} from "@/schemas/accountSetupSingle"

import { GuestLoopStepButton } from "./GuestLoopStepButton"
import { GuestLoopStepFooter } from "./GuestLoopStepFooter"
import { GuestLoopStepHeader } from "./GuestLoopStepHeader"
import { GUEST_LOOP_SINGLE_STEPS, type GuestLoopProgressStep } from "./guestLoopSteps"
import {
  useGuestLoopStepCanSubmit,
  useGuestLoopStepValidationFeedback,
} from "./useGuestLoopStepCanSubmit"

type GuestLoopPasswordFormValues =
  | AccountSetupSingleFormValues
  | AccountSetupMultiFormValues

type GuestLoopPasswordStepProps = {
  form: UseFormReturn<GuestLoopPasswordFormValues>
  activeStep: number
  steps?: readonly GuestLoopProgressStep[]
  step1Fields?: readonly FieldPath<GuestLoopPasswordFormValues>[]
  step1Schema?: z.ZodType
  description?: ReactNode
  submitLabel?: string
  onContinue: () => void | Promise<void>
  isSubmitting?: boolean
}

const DEFAULT_DESCRIPTION = (
  <>
    Your guided trial request has been approved.
    <br className="hidden sm:block" />
    <span className="sm:sr-only"> </span>
    Create a password to access your Tummly workspace.
  </>
)

export function GuestLoopPasswordStep({
  form,
  activeStep,
  steps = GUEST_LOOP_SINGLE_STEPS,
  step1Fields = accountSetupSingleStep1Fields,
  step1Schema = accountSetupSingleStep1Schema,
  description = DEFAULT_DESCRIPTION,
  submitLabel = "Continue",
  onContinue,
  isSubmitting = false,
}: GuestLoopPasswordStepProps) {
  const password = form.watch("password")
  const canContinue = useGuestLoopStepCanSubmit(form, step1Fields, step1Schema)

  useGuestLoopStepValidationFeedback(
    form,
    step1Fields,
    step1Schema,
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
        step1Fields.map((field) => [field, form.getValues(field)])
      )
      const result = step1Schema.safeParse(stepValues)

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
  }, [form, step1Fields, step1Schema])

  return (
    <div className="flex w-full flex-col gap-8 sm:gap-10 lg:gap-12 xl:gap-16">
      <GuestLoopStepHeader
        title="Create your account"
        description={description}
      />

      <div className="flex flex-col gap-6">
        <div className="flex flex-col gap-6">
          <FormFloatingInput
            control={form.control}
            name="email"
            type="email"
            label="Email"
            autoComplete="email"
            readOnly
          />

          <FormFloatingInput
            control={form.control}
            name="fullName"
            label="Your full name"
            autoComplete="name"
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

        <FormCheckboxLabel
          control={form.control}
          name="agree"
          id="guest-loop-agree"
          labelClassName="text-sm font-medium leading-[18px] text-[#141414] gap-1"
        >
          I agree to the{" "}
          <Link
            to={LEGAL_ROUTES.terms}
            className="font-medium underline underline-offset-2"
          >
            Terms
          </Link>{" "}
          and{" "}
          <Link
            to={LEGAL_ROUTES.privacy}
            className="font-medium underline underline-offset-2"
          >
            Privacy Notice
          </Link>
          .
        </FormCheckboxLabel>
      </div>

      <GuestLoopStepFooter steps={steps} activeStep={activeStep}>
        <GuestLoopStepButton
          enabled={canContinue}
          isSubmitting={isSubmitting}
          onClick={onContinue}
        >
          {submitLabel}
        </GuestLoopStepButton>
      </GuestLoopStepFooter>
    </div>
  )
}

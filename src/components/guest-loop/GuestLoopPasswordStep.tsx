import { useEffect, type ReactNode } from "react"
import type { FieldPath, UseFormReturn } from "react-hook-form"
import type { z } from "zod"

import { PasswordStrengthMeter } from "@/components/auth/PasswordStrengthMeter"
import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import {
  accountSetupSingleStep1Fields,
  accountSetupSingleStep1Schema,
} from "@/schemas/accountSetupSingle"

import { GuestLoopStepButton } from "./GuestLoopStepButton"
import { GuestLoopStepFooter } from "./GuestLoopStepFooter"
import { GuestLoopStepHeader } from "./GuestLoopStepHeader"
import { GUEST_LOOP_SINGLE_STEPS, type GuestLoopProgressStep } from "./guestLoopSteps"
import { useGuestLoopStepCanSubmit } from "./useGuestLoopStepCanSubmit"

type GuestLoopPasswordStepFormValues = {
  email: string
  fullName: string
  password: string
  confirmPassword: string
  agree: boolean
}

type GuestLoopPasswordStepProps<
  T extends GuestLoopPasswordStepFormValues = GuestLoopPasswordStepFormValues,
> = {
  form: UseFormReturn<T>
  activeStep: number
  steps?: readonly GuestLoopProgressStep[]
  step1Fields?: readonly FieldPath<T>[]
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

export function GuestLoopPasswordStep<
  T extends GuestLoopPasswordStepFormValues = GuestLoopPasswordStepFormValues,
>({
  form,
  activeStep,
  steps = GUEST_LOOP_SINGLE_STEPS,
  step1Fields = accountSetupSingleStep1Fields as unknown as readonly FieldPath<T>[],
  step1Schema = accountSetupSingleStep1Schema,
  description = DEFAULT_DESCRIPTION,
  submitLabel = "Continue",
  onContinue,
  isSubmitting = false,
}: GuestLoopPasswordStepProps<T>) {
  const password = form.watch("password")
  const canContinue = useGuestLoopStepCanSubmit(form, step1Fields, step1Schema)

  useEffect(() => {
    const subscription = form.watch((_value, { name, type }) => {
      if (type !== "change" || name !== "password") {
        return
      }

      if (form.getValues("confirmPassword")) {
        void form.trigger("confirmPassword")
      }
    })

    return () => subscription.unsubscribe()
  }, [form])

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
            />

            <PasswordStrengthMeter password={password ?? ""} />

            <p className="m-0 text-sm font-medium tracking-[-0.28px] text-[#232323]">
              Use at least 12 characters with a number or symbol.
            </p>
          </div>

          <FormFloatingInput
            control={form.control}
            name="confirmPassword"
            type="password"
            label="Confirm password"
            autoComplete="new-password"
            required
            liveValidate
          />
        </div>

        <FormCheckboxLabel
          control={form.control}
          name="agree"
          id="guest-loop-agree"
          labelClassName="text-sm font-medium leading-[18px] text-[#141414] gap-1"
        >
          I agree to the{" "}
          <span className="font-medium underline underline-offset-2">Terms</span>{" "}
          and{" "}
          <span className="font-medium underline underline-offset-2">
            Privacy Notice
          </span>
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

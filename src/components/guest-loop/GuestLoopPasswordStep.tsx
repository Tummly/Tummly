import { useEffect } from "react"
import type { UseFormReturn } from "react-hook-form"

import { PasswordStrengthMeter } from "@/components/auth/PasswordStrengthMeter"
import { FormCheckboxLabel } from "@/components/form/FormCheckboxLabel"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import {
  accountSetupSingleStep1Fields,
  accountSetupSingleStep1Schema,
  type AccountSetupSingleFormValues,
} from "@/schemas/accountSetupSingle"

import { GuestLoopStepButton } from "./GuestLoopStepButton"
import { GuestLoopStepFooter } from "./GuestLoopStepFooter"
import { GuestLoopStepHeader } from "./GuestLoopStepHeader"
import { GUEST_LOOP_SINGLE_STEPS, type GuestLoopProgressStep } from "./guestLoopSteps"
import { useGuestLoopStepCanSubmit } from "./useGuestLoopStepCanSubmit"

type GuestLoopPasswordStepProps = {
  form: UseFormReturn<AccountSetupSingleFormValues>
  activeStep: number
  steps?: readonly GuestLoopProgressStep[]
  onContinue: () => void | Promise<void>
  isSubmitting?: boolean
}

export function GuestLoopPasswordStep({
  form,
  activeStep,
  steps = GUEST_LOOP_SINGLE_STEPS,
  onContinue,
  isSubmitting = false,
}: GuestLoopPasswordStepProps) {
  const password = form.watch("password")
  const canContinue = useGuestLoopStepCanSubmit(
    form,
    accountSetupSingleStep1Fields,
    accountSetupSingleStep1Schema
  )

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
    <div className="flex w-full flex-col gap-8 sm:gap-10">
      <GuestLoopStepHeader
        title="Create your account"
        description={
          <>
            Your guided trial request has been approved.
            <br className="hidden sm:block" />
            <span className="sm:sr-only"> </span>
            Create a password to access your Tummly workspace.
          </>
        }
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
          Continue
        </GuestLoopStepButton>
      </GuestLoopStepFooter>
    </div>
  )
}

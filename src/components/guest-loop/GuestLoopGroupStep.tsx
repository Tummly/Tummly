import type { UseFormReturn } from "react-hook-form"

import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { FormFloatingSelect } from "@/components/form/FormFloatingSelect"
import {
  BUSINESS_CATEGORY_OPTIONS,
  LOCATION_COUNT_OPTIONS,
} from "@/components/home/hero-trial-options"
import {
  accountSetupMultiStep2Fields,
  accountSetupMultiStep2Schema,
  type AccountSetupMultiFormValues,
} from "@/schemas/accountSetupMulti"

import { GuestLoopStepButton } from "./GuestLoopStepButton"
import { GuestLoopStepFooter } from "./GuestLoopStepFooter"
import { GuestLoopStepHeader } from "./GuestLoopStepHeader"
import { GUEST_LOOP_MULTI_STEPS, type GuestLoopProgressStep } from "./guestLoopSteps"
import { useGuestLoopStepCanSubmit, useGuestLoopStepValidationFeedback } from "./useGuestLoopStepCanSubmit"

type GuestLoopGroupStepProps = {
  form: UseFormReturn<AccountSetupMultiFormValues>
  activeStep: number
  steps?: readonly GuestLoopProgressStep[]
  onConfirm: () => void | Promise<void>
  isSubmitting?: boolean
}

export function GuestLoopGroupStep({
  form,
  activeStep,
  steps = GUEST_LOOP_MULTI_STEPS,
  onConfirm,
  isSubmitting = false,
}: GuestLoopGroupStepProps) {
  const businessLink = form.watch("businessLink")
  const canConfirm = useGuestLoopStepCanSubmit(
    form,
    accountSetupMultiStep2Fields,
    accountSetupMultiStep2Schema
  )

  useGuestLoopStepValidationFeedback(
    form,
    accountSetupMultiStep2Fields,
    accountSetupMultiStep2Schema,
    canConfirm
  )

  return (
    <div className="flex w-full flex-col gap-8 sm:gap-10 lg:gap-12 xl:gap-16">
      <GuestLoopStepHeader
        title="Confirm your restaurant group"
        description="Check the details for the restaurant group or brand you want to manage in Tummly. We use this to create your shared workspace and location structure."
      />

      <div className="flex flex-col gap-6">
        <FormFloatingInput
          control={form.control}
          name="groupName"
          label="Restaurant group or brand name"
          required
        />

        <FormFloatingSelect
          control={form.control}
          name="businessCategory"
          label="Business category"
          options={BUSINESS_CATEGORY_OPTIONS}
          required
        />

        <FormFloatingSelect
          control={form.control}
          name="numLocations"
          label="Number of locations"
          options={LOCATION_COUNT_OPTIONS}
          required
        />

        <FormFloatingInput
          control={form.control}
          name="primaryPhone"
          label="Primary contact phone"
          type="tel"
          autoComplete="tel"
          optional
        />

        <FormFloatingInput
          control={form.control}
          name="businessLink"
          label="Website or social link"
          optional
          liveValidate={Boolean(businessLink?.trim())}
        />

        <p className="m-0 text-base leading-[21px] tracking-[-0.32px] text-[#232323]">
          You can add your first rollout locations now and add more later from
          your workspace.
        </p>
      </div>

      <GuestLoopStepFooter steps={steps} activeStep={activeStep}>
        <GuestLoopStepButton
          enabled={canConfirm}
          isSubmitting={isSubmitting}
          onClick={onConfirm}
        >
          Confirm group
        </GuestLoopStepButton>
      </GuestLoopStepFooter>
    </div>
  )
}

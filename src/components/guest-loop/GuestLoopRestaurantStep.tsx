import { MapPinIcon } from "lucide-react"
import type { UseFormReturn } from "react-hook-form"

import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { FormFloatingSelect } from "@/components/form/FormFloatingSelect"
import { BUSINESS_CATEGORY_OPTIONS } from "@/components/home/hero-trial-options"
import { FieldErrorSlot } from "@/components/ui/field"
import { cn } from "@/lib/utils"
import {
  accountSetupSingleStep2Fields,
  accountSetupSingleStep2Schema,
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

type GuestLoopRestaurantStepProps = {
  form: UseFormReturn<AccountSetupSingleFormValues>
  activeStep: number
  steps?: readonly GuestLoopProgressStep[]
  onConfirm: () => void | Promise<void>
  isSubmitting?: boolean
}

export function GuestLoopRestaurantStep({
  form,
  activeStep,
  steps = GUEST_LOOP_SINGLE_STEPS,
  onConfirm,
  isSubmitting = false,
}: GuestLoopRestaurantStepProps) {
  const address = form.watch("address")
  const businessLink = form.watch("businessLink")
  const canConfirm = useGuestLoopStepCanSubmit(
    form,
    accountSetupSingleStep2Fields,
    accountSetupSingleStep2Schema
  )

  useGuestLoopStepValidationFeedback(
    form,
    accountSetupSingleStep2Fields,
    accountSetupSingleStep2Schema,
    canConfirm,
    {
      shouldSkipValidationFeedback: (fieldPath) => fieldPath === "postcode",
    }
  )

  const rootError = form.formState.errors.root?.message
  const showAddressPin = !address?.trim()

  return (
    <div className="flex w-full flex-col gap-8 sm:gap-10">
      <GuestLoopStepHeader
        title="Confirm restaurant"
        description="Check the details for the location you want to set up first. We use this to prepare your workspace, guest link, QR materials and private feedback form."
      />

      <div className="flex flex-col gap-6">
        <FormFloatingInput
          control={form.control}
          name="restaurantName"
          label="Restaurant or brand name"
          required
        />

        <FormFloatingInput
          control={form.control}
          name="locationName"
          label="Location name"
          required
        />

        <div className="flex flex-col gap-6 sm:flex-row sm:gap-5">
          <div className="relative min-w-0 flex-1">
            {showAddressPin ? (
              <MapPinIcon
                aria-hidden
                className="pointer-events-none absolute left-[13px] top-4 z-10 size-[18px] text-[#7d7d7d]"
              />
            ) : null}
            <FormFloatingInput
              control={form.control}
              name="address"
              label="Address"
              required
              className={cn(
                showAddressPin &&
                  "[&_label]:left-[22px] [&_input]:pl-[22px]"
              )}
            />
          </div>

          <div className="min-w-0 flex-1">
            <FormFloatingInput
              control={form.control}
              name="postcode"
              label="Postcode"
              required
              validateOnBlur
            />
          </div>
        </div>

        <FormFloatingInput
          control={form.control}
          name="phone"
          label="Restaurant phone number"
          type="tel"
          autoComplete="tel"
          required
          liveValidate
        />

        <FormFloatingInput
          control={form.control}
          name="businessLink"
          label="Website or social link"
          optional
          liveValidate={Boolean(businessLink?.trim())}
        />

        <FormFloatingSelect
          control={form.control}
          name="businessCategory"
          label="Business category"
          options={BUSINESS_CATEGORY_OPTIONS}
          required
        />

        <FieldErrorSlot error={rootError} reserveClassName="min-h-0" />
      </div>

      <GuestLoopStepFooter steps={steps} activeStep={activeStep}>
        <GuestLoopStepButton
          enabled={canConfirm}
          isSubmitting={isSubmitting}
          onClick={onConfirm}
        >
          Confirm restaurant
        </GuestLoopStepButton>
      </GuestLoopStepFooter>
    </div>
  )
}

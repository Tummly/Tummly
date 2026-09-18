import type { UseFormReturn } from "react-hook-form"

import { FormAddressPostcodeFields } from "@/components/form/FormAddressPostcodeFields"
import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { GuestLoopSignupProgress } from "@/components/guest-loop/GuestLoopSignupProgress"
import {
  useGuestLoopStepCanSubmit,
  useGuestLoopStepValidationFeedback,
} from "@/components/guest-loop/useGuestLoopStepCanSubmit"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import {
  signupLocationStepFields,
  signupLocationStepSchema,
  type SignupOnboardingFormValues,
} from "@/schemas/signupOnboarding"

type SignupLocationStepProps = {
  form: UseFormReturn<SignupOnboardingFormValues>
  onContinue: () => void | Promise<void>
  onBack?: () => void
  isSubmitting?: boolean
}

export function SignupLocationStep({
  form,
  onContinue,
  onBack,
  isSubmitting = false,
}: SignupLocationStepProps) {
  const canContinue = useGuestLoopStepCanSubmit(
    form,
    signupLocationStepFields,
    signupLocationStepSchema
  )

  useGuestLoopStepValidationFeedback(
    form,
    signupLocationStepFields,
    signupLocationStepSchema,
    canContinue,
    {
      shouldSkipValidationFeedback: (fieldPath) => fieldPath === "postcode",
    }
  )

  const rootError = form.formState.errors.root?.message
  const isDisabled = !canContinue || isSubmitting

  return (
    <div className="flex w-full flex-col gap-[50px]">
      <GuestLoopSignupProgress
        activeStep={3}
        title="Set up your first Location"
        description="Add the Location where you'll start using Tummly. You can add more Locations later if your plan supports them."
      />

      <div className="flex flex-col gap-9">
        <div className="flex flex-col gap-[18px]">
          <FormFloatingInput
            control={form.control}
            name="locationName"
            label="Location name"
            required
          />

          <FormAddressPostcodeFields
            control={form.control}
            addressName="address"
            postcodeName="postcode"
            addressOverriddenName="addressOverridden"
            cityName="city"
          />

          <FormFloatingInput
            control={form.control}
            name="city"
            label="City / town"
            autoComplete="address-level2"
            required
          />

          <FormFloatingInput
            control={form.control}
            name="country"
            label="Country"
            readOnly
          />

          <div className="flex flex-col gap-3">
            <FormFloatingInput
              control={form.control}
              name="timezone"
              label="Timezone"
              readOnly
            />
            <p className="m-0 text-sm leading-5 text-[#141414]">
              We&apos;ll use your Location timezone for reporting, Campaign
              scheduling and activity dates.
            </p>
          </div>

          <FieldErrorSlot error={rootError} reserveClassName="min-h-0" />
        </div>

        <div className="flex w-full gap-4">
          {onBack ? (
            <Button
              type="button"
              variant="outline"
              onClick={onBack}
              disabled={isSubmitting}
              className="h-11 w-[158px] shrink-0 rounded-[4px] border-[#4e4e4e] px-[19px] text-sm font-medium shadow-none"
            >
              Back
            </Button>
          ) : null}

          <Button
            type="button"
            variant="default"
            disabled={isDisabled}
            onClick={() => void onContinue()}
            className="h-11 min-w-0 flex-1 rounded-[4px] px-[18px] py-3 text-sm font-medium leading-[22px] shadow-none"
          >
            {isSubmitting ? "Please wait..." : "Set up your account"}
          </Button>
        </div>
      </div>
    </div>
  )
}

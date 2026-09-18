import type { UseFormReturn } from "react-hook-form"

import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { FormFloatingSelect } from "@/components/form/FormFloatingSelect"
import { GuestLoopSignupProgress } from "@/components/guest-loop/GuestLoopSignupProgress"
import {
  useGuestLoopStepCanSubmit,
  useGuestLoopStepValidationFeedback,
} from "@/components/guest-loop/useGuestLoopStepCanSubmit"
import { BUSINESS_CATEGORY_OPTIONS } from "@/components/home/hero-trial-options"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import {
  signupRestaurantStepFields,
  signupRestaurantStepSchema,
  type SignupOnboardingFormValues,
} from "@/schemas/signupOnboarding"

type SignupRestaurantStepProps = {
  form: UseFormReturn<SignupOnboardingFormValues>
  onContinue: () => void | Promise<void>
  onBack?: () => void
  isSubmitting?: boolean
}

export function SignupRestaurantStep({
  form,
  onContinue,
  onBack,
  isSubmitting = false,
}: SignupRestaurantStepProps) {
  const businessLink = form.watch("businessLink")
  const canContinue = useGuestLoopStepCanSubmit(
    form,
    signupRestaurantStepFields,
    signupRestaurantStepSchema
  )

  useGuestLoopStepValidationFeedback(
    form,
    signupRestaurantStepFields,
    signupRestaurantStepSchema,
    canContinue
  )

  const rootError = form.formState.errors.root?.message
  const isDisabled = !canContinue || isSubmitting

  return (
    <div className="flex w-full flex-col gap-[50px]">
      <GuestLoopSignupProgress
        activeStep={2}
        title="Tell us about your restaurant"
        description="We'll use these details to set up your Tummly workspace."
      />

      <div className="flex flex-col gap-9">
        <div className="flex flex-col gap-[18px]">
          <FormFloatingInput
            control={form.control}
            name="restaurantName"
            label="Restaurant or group name"
            required
          />

          <FormFloatingSelect
            control={form.control}
            name="businessCategory"
            label="Restaurant type*"
            options={BUSINESS_CATEGORY_OPTIONS}
            required
          />

          <FormFloatingInput
            control={form.control}
            name="businessLink"
            label="Website"
            optional
            liveValidate={Boolean(businessLink?.trim())}
          />

          <FormFloatingInput
            control={form.control}
            name="phone"
            label="Phone"
            type="tel"
            autoComplete="tel"
            optional
            liveValidate
          />

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
            {isSubmitting ? "Please wait..." : "Continue"}
          </Button>
        </div>
      </div>
    </div>
  )
}

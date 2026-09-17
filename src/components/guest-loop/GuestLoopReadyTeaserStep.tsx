import { GuestLoopStepButton } from "./GuestLoopStepButton"
import { GuestLoopStepFooter } from "./GuestLoopStepFooter"
import { GuestLoopStepHeader } from "./GuestLoopStepHeader"
import {
  GUEST_LOOP_SINGLE_STEPS,
  type GuestLoopProgressStep,
} from "./guestLoopSteps"

type GuestLoopReadyTeaserStepProps = {
  activeStep: number
  steps?: readonly GuestLoopProgressStep[]
  onContinue: () => void | Promise<void>
  isSubmitting?: boolean
}

/**
 * Pre-plan “ready” step for self-serve signup — continues to choose-plan
 * without calling setup-account or running provisioning phases.
 */
export function GuestLoopReadyTeaserStep({
  activeStep,
  steps = GUEST_LOOP_SINGLE_STEPS,
  onContinue,
  isSubmitting = false,
}: GuestLoopReadyTeaserStepProps) {
  return (
    <div className="flex w-full flex-col gap-8 sm:gap-10 lg:gap-12 xl:gap-16">
      <GuestLoopStepHeader
        title="You're ready to choose a plan"
        description="Your profile is saved. Next, pick Essential or Pro to create your workspace."
      />

      <div className="flex flex-col gap-3 rounded-lg bg-[#f1f1f1] px-4 py-5">
        <p className="m-0 text-sm font-semibold leading-normal tracking-[-0.28px] text-primary">
          Profile saved
        </p>
        <p className="m-0 text-sm leading-[18px] tracking-[-0.28px] text-[#232323]">
          We will create your Smart Guest Link, feedback form and starter QR
          materials after you confirm a plan.
        </p>
      </div>

      <GuestLoopStepFooter
        steps={steps}
        activeStep={activeStep}
        markActiveStepComplete
      >
        <GuestLoopStepButton
          enabled
          isSubmitting={isSubmitting}
          onClick={onContinue}
        >
          Continue to choose plan
        </GuestLoopStepButton>
      </GuestLoopStepFooter>
    </div>
  )
}

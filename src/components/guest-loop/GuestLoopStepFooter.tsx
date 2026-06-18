import type { ReactNode } from "react"

import { GuestLoopProgressStepper } from "./GuestLoopProgressStepper"
import type { GuestLoopProgressStep } from "./guestLoopSteps"

type GuestLoopStepFooterProps = {
  steps: readonly GuestLoopProgressStep[]
  activeStep: number
  markActiveStepComplete?: boolean
  children: ReactNode
}

export function GuestLoopStepFooter({
  steps,
  activeStep,
  markActiveStepComplete,
  children,
}: GuestLoopStepFooterProps) {
  return (
    <div className="flex flex-col gap-8">
      <GuestLoopProgressStepper
        steps={steps}
        activeStep={activeStep}
        markActiveStepComplete={markActiveStepComplete}
      />
      {children}
    </div>
  )
}

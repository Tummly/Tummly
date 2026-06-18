import { cn } from "@/lib/utils"

import type { GuestLoopProgressStep } from "./guestLoopSteps"

type GuestLoopProgressStepperProps = {
  steps: readonly GuestLoopProgressStep[]
  activeStep: number
  /** When true, the current active step renders as complete (e.g. provisioning finished on Ready). */
  markActiveStepComplete?: boolean
  className?: string
}

type StepState = "complete" | "active" | "upcoming"

function StepCircle({
  number,
  state,
}: {
  number: number
  state: StepState
}) {
  return (
    <span
      className={cn(
        "flex size-[23px] shrink-0 items-center justify-center rounded-[30px] border-2 px-2 py-1 text-xs font-bold leading-none",
        state === "complete" &&
          "border-primary bg-primary text-white",
        state === "active" &&
          "border-[#141414] bg-white text-[#141414]",
        state === "upcoming" &&
          "border-[#7d7d7d] bg-white text-[#7d7d7d]"
      )}
    >
      {number}
    </span>
  )
}

function StepConnector({ isComplete }: { isComplete: boolean }) {
  return (
    <span
      aria-hidden
      className={cn(
        "h-[2px] min-w-[clamp(0.75rem,3vw,1.5rem)] flex-1",
        isComplete ? "bg-primary" : "bg-[#d2d2d2]"
      )}
    />
  )
}

function getStepState(
  stepNumber: number,
  activeStep: number,
  markActiveStepComplete: boolean
): StepState {
  const isComplete =
    stepNumber < activeStep ||
    (markActiveStepComplete && stepNumber === activeStep)

  if (isComplete) {
    return "complete"
  }

  if (stepNumber === activeStep) {
    return "active"
  }

  return "upcoming"
}

export function GuestLoopProgressStepper({
  steps,
  activeStep,
  markActiveStepComplete = false,
  className,
}: GuestLoopProgressStepperProps) {
  const allStepsComplete =
    markActiveStepComplete && activeStep === steps.length

  return (
    <div
      className={cn("flex w-full items-center gap-2 sm:gap-3", className)}
      aria-label={
        allStepsComplete
          ? `All ${steps.length} steps complete`
          : `Step ${activeStep} of ${steps.length}`
      }
    >
      {steps.map((step, index) => {
        const state = getStepState(
          step.number,
          activeStep,
          markActiveStepComplete
        )

        return (
          <div key={step.number} className="contents">
            <div className="flex shrink-0 items-center gap-2 sm:gap-2.5">
              <StepCircle number={step.number} state={state} />
              <span
                className={cn(
                  "whitespace-nowrap text-sm font-bold leading-none",
                  state === "complete" && "text-primary",
                  state === "active" && "text-[#141414]",
                  state === "upcoming" && "text-[#7d7d7d]"
                )}
              >
                {step.label}
              </span>
            </div>

            {index < steps.length - 1 ? (
              <StepConnector isComplete={state === "complete"} />
            ) : null}
          </div>
        )
      })}
    </div>
  )
}

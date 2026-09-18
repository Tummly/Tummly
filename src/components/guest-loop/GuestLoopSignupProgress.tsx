import type { ReactNode } from "react"

import { cn } from "@/lib/utils"

import { SIGNUP_ONBOARDING_STEPS } from "./guestLoopSteps"

type GuestLoopSignupProgressProps = {
  activeStep: 1 | 2 | 3
  title: string
  description: ReactNode
}

export function GuestLoopSignupProgress({
  activeStep,
  title,
  description,
}: GuestLoopSignupProgressProps) {
  const step = SIGNUP_ONBOARDING_STEPS[activeStep - 1]
  const stepLabel = step?.label ?? ""

  return (
    <div className="flex w-full flex-col items-start gap-[30px]">
      <div
        className="flex h-1.5 w-full gap-2.5"
        role="progressbar"
        aria-valuemin={1}
        aria-valuemax={3}
        aria-valuenow={activeStep}
        aria-label={`Step ${activeStep} of 3`}
      >
        {SIGNUP_ONBOARDING_STEPS.map((entry) => {
          const isFilled = entry.number <= activeStep

          return (
            <div
              key={entry.number}
              className={cn(
                "h-full min-w-0 flex-1 rounded-lg",
                isFilled ? "bg-[#14a74a]" : "bg-[#e5e5e5]"
              )}
            />
          )
        })}
      </div>

      <div className="flex flex-col items-start gap-3 text-[#141414]">
        <p className="m-0 text-lg font-medium leading-normal text-black">
          {activeStep} of 3 · {stepLabel}
        </p>
        <h1 className="m-0 text-[36px] font-medium leading-normal tracking-normal">
          {title}
        </h1>
        {typeof description === "string" ? (
          <p className="m-0 text-base leading-[22px]">{description}</p>
        ) : (
          <div className="m-0 text-base leading-[22px]">{description}</div>
        )}
      </div>
    </div>
  )
}

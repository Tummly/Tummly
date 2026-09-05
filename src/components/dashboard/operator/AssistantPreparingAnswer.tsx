import { motion } from "framer-motion"

import type { OperatorAiAssistantAnalysisScope } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import { periodPhraseForReportingPeriod } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"

function DaisySpinner({ className }: { className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
      aria-hidden="true"
    >
      <rect x="10.9" y="1.5" width="2.2" height="5" rx="1.1" transform="rotate(0 12 12)" />
      <rect x="10.9" y="1.5" width="2.2" height="5" rx="1.1" transform="rotate(45 12 12)" />
      <rect x="10.9" y="1.5" width="2.2" height="5" rx="1.1" transform="rotate(90 12 12)" />
      <rect x="10.9" y="1.5" width="2.2" height="5" rx="1.1" transform="rotate(135 12 12)" />
      <rect x="10.9" y="1.5" width="2.2" height="5" rx="1.1" transform="rotate(180 12 12)" />
      <rect x="10.9" y="1.5" width="2.2" height="5" rx="1.1" transform="rotate(225 12 12)" />
      <rect x="10.9" y="1.5" width="2.2" height="5" rx="1.1" transform="rotate(270 12 12)" />
      <rect x="10.9" y="1.5" width="2.2" height="5" rx="1.1" transform="rotate(315 12 12)" />
    </svg>
  )
}

type AssistantPreparingAnswerProps = {
  scope?: OperatorAiAssistantAnalysisScope | null
}

export function AssistantPreparingAnswer({ scope }: AssistantPreparingAnswerProps) {
  const periodText = scope ? periodPhraseForReportingPeriod(scope.reportingPeriod) : "the last 7 days"
  const previousPeriodText = periodText.replace(/^the\s+/, "")

  const steps = [
    `Checking Feedback from ${periodText}`,
    "Grouping recurring issues",
    `Comparing with the previous ${previousPeriodText}`,
    "Checking which Locations and QR sources are involved...",
  ]

  return (
    <div
      className="flex flex-col gap-2"
      role="status"
      aria-live="polite"
      aria-label="Preparing answer"
    >
      <span data-assistant-wait-phrase className="sr-only">
        Preparing answer…
      </span>
      <div className="flex items-center gap-2.5">
        <DaisySpinner className="size-5 shrink-0 animate-spin text-white" />
        <p className="text-sm font-normal text-white">Preparing answer...</p>
      </div>

      <div className="flex flex-col gap-1.5 pl-[30px]">
        {steps.map((step, index) => (
          <motion.p
            key={step}
            initial={{ opacity: 0, y: 3 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.35, delay: index * 0.45 }}
            className="text-sm font-normal leading-5 text-neutral-400"
          >
            {step}
          </motion.p>
        ))}
      </div>
    </div>
  )
}

import {
  ASSISTANT_WAIT_TEXT_CLASS,
} from "@/lib/operatorAiAssistant/assistantWaitPresentation"

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
  /** Live wait phrase from the turn pipeline (gerund / Retrieving / Preparing). */
  phrase: string
}

export function AssistantPreparingAnswer({
  phrase,
}: AssistantPreparingAnswerProps) {
  const label = phrase.trim() || "Preparing answer…"

  return (
    <div
      className="flex items-center gap-2.5"
      role="status"
      aria-live="polite"
      aria-label={label}
    >
      <DaisySpinner className="size-5 shrink-0 animate-spin text-white" />
      <p className={ASSISTANT_WAIT_TEXT_CLASS}>
        <span key={label} data-assistant-wait-phrase>
          {label}
        </span>
      </p>
    </div>
  )
}

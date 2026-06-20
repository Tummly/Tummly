import { AlertCircleIcon } from "lucide-react"

type GuestFeedbackNotFoundProps = {
  message?: string
}

export function GuestFeedbackNotFound({
  message = "This link was not found or is no longer active.",
}: GuestFeedbackNotFoundProps) {
  return (
    <div className="flex w-full flex-col items-center gap-4 py-10 text-center">
      <span className="flex size-12 items-center justify-center rounded-full border border-guest-feedback-border bg-guest-feedback-surface">
        <AlertCircleIcon className="size-5 text-guest-feedback-muted" />
      </span>
      <div className="flex max-w-[280px] flex-col gap-2">
        <h1 className="text-xl font-medium text-guest-feedback-text">
          Link not found
        </h1>
        <p className="text-sm leading-relaxed text-guest-feedback-muted">
          {message}
        </p>
      </div>
    </div>
  )
}

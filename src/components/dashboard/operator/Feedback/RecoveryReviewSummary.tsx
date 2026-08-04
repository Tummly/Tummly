import type { ReactNode } from "react"

import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import {
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_INPUT_CLASS,
  FEEDBACK_TEXTAREA_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import {
  formatReviewAiUsage,
  formatReviewDeliveryUsage,
  formatReviewFeedbackReference,
  labelForReviewChannel,
  REVIEW_RESPONSE_EMPTY_VALUE,
} from "@/lib/operatorFeedback/reviewResponsePresentation"
import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"
import { cn } from "@/lib/utils"

function ReviewRow({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className="flex w-full items-start justify-between gap-4">
      <dt className="shrink-0 text-base font-semibold text-[var(--op-color-gray-550)]">
        {label}
      </dt>
      <dd className="min-w-0 text-right text-base font-medium text-op-text-primary">
        {children}
      </dd>
    </div>
  )
}

function IssueTagsValue({ labels }: { labels: string[] | null }) {
  if (labels == null) {
    return <span>{REVIEW_RESPONSE_EMPTY_VALUE}</span>
  }
  if (labels.length === 0) {
    return <span>No issues detected.</span>
  }
  return (
    <ul className="flex flex-wrap justify-end gap-3">
      {labels.map((label) => (
        <li key={label}>
          <Badge variant="tag">{label}</Badge>
        </li>
      ))}
    </ul>
  )
}

export type RecoveryReviewSummaryProps = {
  idPrefix: string
  guestName: string
  channel: RespondToGuestChannel | null
  maskedDestination: string | null
  feedbackComment: string
  feedbackId: number | null
  issueTagLabels: string[] | null
  subject: string
  message: string
  aiActionCount: number
}

/**
 * Shared Review and send left column — confirmation rows, read-only Final
 * response, and Usage. Reused by guest-messaging recovery wizards.
 */
export function RecoveryReviewSummary({
  idPrefix,
  guestName,
  channel,
  maskedDestination,
  feedbackComment,
  feedbackId,
  issueTagLabels,
  subject,
  message,
  aiActionCount,
}: RecoveryReviewSummaryProps) {
  const destination =
    maskedDestination != null && maskedDestination.trim() !== ""
      ? maskedDestination.trim()
      : REVIEW_RESPONSE_EMPTY_VALUE

  return (
    <div className="flex w-full flex-col gap-6">
      <dl className="flex flex-col gap-6">
        <ReviewRow label="Guest">{guestName}</ReviewRow>
        <ReviewRow label="Channel">{labelForReviewChannel(channel)}</ReviewRow>
        <ReviewRow label="Destination">{destination}</ReviewRow>
        <ReviewRow label="Original feedback">
          “{feedbackComment}”
        </ReviewRow>
        <ReviewRow label="Feedback reference">
          {feedbackId != null
            ? formatReviewFeedbackReference(feedbackId)
            : REVIEW_RESPONSE_EMPTY_VALUE}
        </ReviewRow>
        <ReviewRow label="Issue tags">
          <IssueTagsValue labels={issueTagLabels} />
        </ReviewRow>
      </dl>

      <Separator className="bg-op-card-border" />

      <section className="flex flex-col gap-6" aria-labelledby={`${idPrefix}-final-response`}>
        <h3
          id={`${idPrefix}-final-response`}
          className="text-lg font-semibold text-op-text-primary"
        >
          Final response
        </h3>
        <div className="flex flex-col gap-4">
          {channel === "email" ? (
            <div className="flex flex-col gap-3">
              <label
                htmlFor={`${idPrefix}-subject`}
                className={FEEDBACK_FIELD_LABEL_CLASS}
              >
                Subject
              </label>
              <Input
                id={`${idPrefix}-subject`}
                value={subject}
                readOnly
                tabIndex={0}
                className={cn(
                  FEEDBACK_INPUT_CLASS,
                  "h-12 cursor-default focus-visible:border-op-input-border focus-visible:ring-0"
                )}
              />
            </div>
          ) : null}
          <div className="flex flex-col gap-3">
            <label
              htmlFor={`${idPrefix}-message`}
              className={FEEDBACK_FIELD_LABEL_CLASS}
            >
              Message
            </label>
            <Textarea
              id={`${idPrefix}-message`}
              value={message}
              readOnly
              tabIndex={0}
              className={cn(
                FEEDBACK_TEXTAREA_CLASS,
                "min-h-[220px] cursor-default focus-visible:border-op-input-border focus-visible:ring-0"
              )}
            />
          </div>
        </div>
      </section>

      <Separator className="bg-op-card-border" />

      <section className="flex flex-col gap-6" aria-labelledby={`${idPrefix}-usage`}>
        <h3
          id={`${idPrefix}-usage`}
          className="text-lg font-semibold text-op-text-primary"
        >
          Usage
        </h3>
        <dl className="flex flex-col gap-4">
          <ReviewRow label="AI usage">
            {formatReviewAiUsage(aiActionCount)}
          </ReviewRow>
          <ReviewRow label="Delivery usage">
            {formatReviewDeliveryUsage(channel)}
          </ReviewRow>
        </dl>
      </section>
    </div>
  )
}

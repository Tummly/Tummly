import type { ReactNode } from "react"

import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"
import type {
  ClassificationStatus,
  FeedbackSentiment,
} from "@/types/dashboard"

function ClassificationValue({
  status,
  sentiment,
}: {
  status: ClassificationStatus
  sentiment: FeedbackSentiment | null
}) {
  if (status === "Pending") {
    return (
      <span className="text-base font-medium text-op-text-primary">
        Pending
      </span>
    )
  }
  if (status === "Failed" || sentiment == null) {
    return (
      <span className="text-base font-medium text-op-text-primary">
        Unavailable
      </span>
    )
  }

  const label =
    sentiment === "positive"
      ? "Positive"
      : sentiment === "neutral"
        ? "Neutral"
        : "Negative"

  return <Badge variant={sentiment}>{label}</Badge>
}

function SummaryRow({
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

export type RecoveryFeedbackSummaryPanelProps = {
  guestName: string
  classificationStatus: ClassificationStatus
  classificationSentiment: FeedbackSentiment | null
  contactLabel: string
  feedbackComment: string
  issueTagLabels: string[] | null
  /** Extra rows after Issue tags (Purpose, Tone, Offer, …). */
  extraRows?: readonly { label: string; children: ReactNode }[]
}

/** Mid-flow Feedback summary with Classification, Contact, and Issue tags. */
export function RecoveryFeedbackSummaryPanel({
  guestName,
  classificationStatus,
  classificationSentiment,
  contactLabel,
  feedbackComment,
  issueTagLabels,
  extraRows = [],
}: RecoveryFeedbackSummaryPanelProps) {
  return (
    <aside className="flex w-full flex-1 flex-col gap-6 rounded-[6px] bg-op-background-secondary p-5">
      <h2 className="text-lg font-semibold text-op-text-primary">
        Feedback summary
      </h2>
      <dl className="flex flex-col gap-3.5">
        <SummaryRow label="Guest:">{guestName}</SummaryRow>
        <Separator className="bg-op-card-border" />
        <SummaryRow label="Classification:">
          <ClassificationValue
            status={classificationStatus}
            sentiment={classificationSentiment}
          />
        </SummaryRow>
        <Separator className="bg-op-card-border" />
        <SummaryRow label="Contact:">{contactLabel}</SummaryRow>
        <Separator className="bg-op-card-border" />
        <SummaryRow label="Feedback:">“{feedbackComment}”</SummaryRow>
        <Separator className="bg-op-card-border" />
        <SummaryRow label="Issue tags:">
          {issueTagLabels == null ? (
            <span>—</span>
          ) : issueTagLabels.length === 0 ? (
            <span>No issues detected.</span>
          ) : (
            <ul className="flex flex-wrap justify-end gap-3">
              {issueTagLabels.map((label) => (
                <li key={label}>
                  <Badge variant="tag">{label}</Badge>
                </li>
              ))}
            </ul>
          )}
        </SummaryRow>
        <Separator className="bg-op-card-border" />
        {extraRows.map((row) => (
          <div key={row.label} className="contents">
            <SummaryRow label={row.label}>{row.children}</SummaryRow>
            <Separator className="bg-op-card-border" />
          </div>
        ))}
      </dl>
    </aside>
  )
}

import type { ReactNode } from "react"

import {
  SERVICE_REPLY_HISTORY_COPY,
  serviceReplyHistoryRows,
  type ServiceReplyStatusTone,
} from "@/lib/operatorFeedback/serviceReplyHistoryPresentation"
import type { FeedbackDetailsActivityEventDto } from "@/types/dashboard"
import { cn } from "@/lib/utils"

const STATUS_TONE_CLASS: Record<ServiceReplyStatusTone, string> = {
  default: "text-foreground",
  muted: "text-[var(--op-color-gray-550)]",
  destructive: "text-destructive",
}

/**
 * Body of the drawer's "Service reply history" section. Stacked per reply
 * (not a table) so the five labels fit the 620px drawer with long status copy.
 */
export function ServiceReplyHistorySection({
  events,
}: {
  events: FeedbackDetailsActivityEventDto[]
}) {
  const rows = serviceReplyHistoryRows(events)

  if (rows.length === 0) {
    return (
      <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
        {SERVICE_REPLY_HISTORY_COPY.empty}
      </p>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      <ul className="flex flex-col gap-3">
        {rows.map((row, index) => (
          <li
            key={`${row.id ?? "reply"}-${index}`}
            className="rounded-[4px] border border-border p-3"
          >
            <dl className="grid grid-cols-2 gap-x-4 gap-y-3">
              <ReplyField label={SERVICE_REPLY_HISTORY_COPY.sentAtLabel}>
                {row.sentAtLabel}
              </ReplyField>
              <ReplyField label={SERVICE_REPLY_HISTORY_COPY.channelLabel}>
                {row.channelLabel}
              </ReplyField>
              <ReplyField label={SERVICE_REPLY_HISTORY_COPY.maskedRecipientLabel}>
                {row.maskedRecipient}
              </ReplyField>
              <ReplyField label={SERVICE_REPLY_HISTORY_COPY.sentByLabel}>
                {row.sentBy}
              </ReplyField>
              <ReplyField
                label={SERVICE_REPLY_HISTORY_COPY.statusLabel}
                className="col-span-2"
                valueClassName={STATUS_TONE_CLASS[row.statusTone]}
              >
                {row.statusText}
              </ReplyField>
            </dl>
          </li>
        ))}
      </ul>
      <p className="text-xs font-normal text-muted-foreground">
        {SERVICE_REPLY_HISTORY_COPY.footer}
      </p>
    </div>
  )
}

function ReplyField({
  label,
  children,
  className,
  valueClassName,
}: {
  label: string
  children: ReactNode
  className?: string
  valueClassName?: string
}) {
  return (
    <div className={cn("flex flex-col gap-0.5", className)}>
      <dt className="text-xs font-medium text-muted-foreground">{label}</dt>
      <dd className={cn("text-sm font-medium text-foreground", valueClassName)}>
        {children}
      </dd>
    </div>
  )
}

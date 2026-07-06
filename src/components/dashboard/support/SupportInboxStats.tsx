import { HelpCentreStatusBadge } from "@/components/help-centre/HelpCentreStatusBadge"
import type { HelpCentreQueryStatus } from "@/types/helpCentre"
import type { SupportQueryListItem } from "@/types/support"

const STATUS_ORDER: HelpCentreQueryStatus[] = [
  "NEW",
  "IN_PROGRESS",
  "WAITING_ON_CUSTOMER",
  "ESCALATED_TO_ADMIN",
  "RESOLVED",
  "CLOSED",
]

const STATUS_LABELS: Record<HelpCentreQueryStatus, string> = {
  NEW: "New",
  IN_PROGRESS: "In progress",
  WAITING_ON_CUSTOMER: "Waiting on customer",
  ESCALATED_TO_ADMIN: "Escalated",
  RESOLVED: "Resolved",
  CLOSED: "Closed",
}

type SupportInboxStatsProps = {
  queries: SupportQueryListItem[]
  activeStatus?: string
  onStatusClick?: (status: HelpCentreQueryStatus | "ALL") => void
}

export function SupportInboxStats({
  queries,
  activeStatus = "ALL",
  onStatusClick,
}: SupportInboxStatsProps) {
  const counts = STATUS_ORDER.reduce(
    (acc, status) => {
      acc[status] = queries.filter((query) => query.status === status).length
      return acc
    },
    {} as Record<HelpCentreQueryStatus, number>
  )

  return (
    <div className="flex flex-wrap gap-2">
      <button
        type="button"
        onClick={() => onStatusClick?.("ALL")}
        className={`rounded-full border px-3 py-1.5 text-sm transition-colors ${
          activeStatus === "ALL"
            ? "border-primary bg-primary text-primary-foreground"
            : "border-border bg-background text-foreground hover:bg-muted"
        }`}
      >
        All ({queries.length})
      </button>
      {STATUS_ORDER.map((status) => {
        const count = counts[status]
        if (count === 0) {
          return null
        }

        return (
          <button
            key={status}
            type="button"
            onClick={() => onStatusClick?.(status)}
            className={`inline-flex items-center gap-2 rounded-full border px-3 py-1.5 text-sm transition-colors ${
              activeStatus === status
                ? "border-primary bg-primary/5"
                : "border-border bg-background hover:bg-muted"
            }`}
          >
            <HelpCentreStatusBadge
              status={status}
              statusLabel={STATUS_LABELS[status]}
            />
            <span className="font-medium tabular-nums">{count}</span>
          </button>
        )
      })}
    </div>
  )
}

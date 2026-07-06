import { cn } from "@/lib/utils"
import type { HelpCentreQueryStatus } from "@/types/helpCentre"

const STATUS_STYLES: Record<HelpCentreQueryStatus, string> = {
  NEW: "bg-blue-50 text-blue-700",
  IN_PROGRESS: "bg-amber-50 text-amber-800",
  WAITING_ON_CUSTOMER: "bg-violet-50 text-violet-800",
  ESCALATED_TO_ADMIN: "bg-rose-50 text-rose-800",
  RESOLVED: "bg-emerald-50 text-emerald-800",
  CLOSED: "bg-muted text-muted-foreground",
}

const STATUS_LABELS: Record<HelpCentreQueryStatus, string> = {
  NEW: "New",
  IN_PROGRESS: "In progress",
  WAITING_ON_CUSTOMER: "Waiting on customer",
  ESCALATED_TO_ADMIN: "Escalated to Admin",
  RESOLVED: "Resolved",
  CLOSED: "Closed",
}

type HelpCentreStatusBadgeProps = {
  status: HelpCentreQueryStatus
  statusLabel?: string
  className?: string
}

export function HelpCentreStatusBadge({
  status,
  statusLabel,
  className,
}: HelpCentreStatusBadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex rounded-full px-2.5 py-1 text-xs font-medium",
        STATUS_STYLES[status],
        className
      )}
    >
      {statusLabel ?? STATUS_LABELS[status]}
    </span>
  )
}

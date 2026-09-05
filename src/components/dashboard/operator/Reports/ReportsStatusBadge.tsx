import { Badge } from "@/components/ui/badge"
import { resolveReportsStatusBadgeVariant } from "@/lib/operatorReports/reportsPresentation"
import { cn } from "@/lib/utils"

type ReportsStatusBadgeProps = {
  status: string
  className?: string
}

/** Operator Badge for report table status labels. */
export function ReportsStatusBadge({
  status,
  className,
}: ReportsStatusBadgeProps) {
  return (
    <Badge
      variant={resolveReportsStatusBadgeVariant(status)}
      className={cn(className)}
    >
      {status}
    </Badge>
  )
}

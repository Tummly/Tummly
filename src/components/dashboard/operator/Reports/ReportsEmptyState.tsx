import type { ReactNode } from "react"

import {
  REPORTS_EMPTY_ACTIONS_CLASS,
  REPORTS_EMPTY_HELPER_CLASS,
  REPORTS_EMPTY_SHELL_CLASS,
  REPORTS_EMPTY_TITLE_CLASS,
  REPORTS_HUB_PAGE_COPY,
} from "@/lib/operatorReports/reportsPresentation"
import { cn } from "@/lib/utils"

type ReportsEmptyStateProps = {
  title?: string
  subtitle?: string
  action?: ReactNode
  className?: string
}

/** Shared Reports empty shell — Capture empty chrome. */
export function ReportsEmptyState({
  title = REPORTS_HUB_PAGE_COPY.emptyTitle,
  subtitle = REPORTS_HUB_PAGE_COPY.emptySubtitle,
  action,
  className,
}: ReportsEmptyStateProps) {
  return (
    <div className={cn(REPORTS_EMPTY_SHELL_CLASS, className)}>
      <div className="flex flex-col items-center gap-2.5 text-center">
        <p className={REPORTS_EMPTY_TITLE_CLASS}>{title}</p>
        <p className={REPORTS_EMPTY_HELPER_CLASS}>{subtitle}</p>
      </div>
      {action != null ? (
        <div className={REPORTS_EMPTY_ACTIONS_CLASS}>{action}</div>
      ) : null}
    </div>
  )
}

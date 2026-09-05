import { ChevronRight } from "lucide-react"
import { Link } from "react-router-dom"

import {
  REPORTS_BREADCRUMB_COPY,
  REPORTS_BREADCRUMB_CURRENT_CLASS,
  REPORTS_BREADCRUMB_LINK_CLASS,
  REPORTS_BREADCRUMB_NAV_CLASS,
} from "@/lib/operatorReports/reportsPresentation"

type ReportsBreadcrumbProps = {
  reportsBasePath: string
  currentLabel: string
}

/** Reports → current report crumb. */
export function ReportsBreadcrumb({
  reportsBasePath,
  currentLabel,
}: ReportsBreadcrumbProps) {
  return (
    <nav aria-label="Breadcrumb" className={REPORTS_BREADCRUMB_NAV_CLASS}>
      <Link to={reportsBasePath} className={REPORTS_BREADCRUMB_LINK_CLASS}>
        {REPORTS_BREADCRUMB_COPY.reports}
      </Link>
      <ChevronRight
        className="size-4 shrink-0 text-op-text-muted"
        aria-hidden
      />
      <span className={REPORTS_BREADCRUMB_CURRENT_CLASS}>{currentLabel}</span>
    </nav>
  )
}

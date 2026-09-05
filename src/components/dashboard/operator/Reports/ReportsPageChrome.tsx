import type { ReactNode } from "react"

import { ReportsBreadcrumb } from "@/components/dashboard/operator/Reports/ReportsBreadcrumb"
import {
  REPORTS_PAGE_ACTIONS_CLASS,
  REPORTS_PAGE_HEADER_COPY_CLASS,
  REPORTS_PAGE_HEADER_ROW_CLASS,
  REPORTS_PAGE_STACK_CLASS,
  REPORTS_PAGE_SUBTITLE_CLASS,
  REPORTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorReports/reportsPresentation"

type ReportsPageChromeProps = {
  title: string
  subtitle: string
  /** When set, shows Reports → current crumb. Hub omits this. */
  breadcrumb?: {
    reportsBasePath: string
    currentLabel: string
  }
  actions?: ReactNode
  children: ReactNode
}

/** Shared Reports page shell — optional breadcrumb, title row, body. */
export function ReportsPageChrome({
  title,
  subtitle,
  breadcrumb,
  actions,
  children,
}: ReportsPageChromeProps) {
  return (
    <div className={REPORTS_PAGE_STACK_CLASS}>
      {breadcrumb != null ? (
        <ReportsBreadcrumb
          reportsBasePath={breadcrumb.reportsBasePath}
          currentLabel={breadcrumb.currentLabel}
        />
      ) : null}

      <div className={REPORTS_PAGE_HEADER_ROW_CLASS}>
        <header className={REPORTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={REPORTS_PAGE_TITLE_CLASS}>{title}</h1>
          <p className={REPORTS_PAGE_SUBTITLE_CLASS}>{subtitle}</p>
        </header>
        {actions != null ? (
          <div className={REPORTS_PAGE_ACTIONS_CLASS}>{actions}</div>
        ) : null}
      </div>

      {children}
    </div>
  )
}

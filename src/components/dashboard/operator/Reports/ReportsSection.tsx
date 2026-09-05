import type { ReactNode } from "react"

import {
  REPORTS_SECTION_CLASS,
  REPORTS_SECTION_HEADER_CLASS,
  REPORTS_SECTION_SUBTITLE_CLASS,
  REPORTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorReports/reportsPresentation"
import { cn } from "@/lib/utils"

type ReportsSectionProps = {
  title?: string
  subtitle?: string
  headerEnd?: ReactNode
  children: ReactNode
  className?: string
}

/** Shared Reports section card — Capture / Performance section chrome. */
export function ReportsSection({
  title,
  subtitle,
  headerEnd,
  children,
  className,
}: ReportsSectionProps) {
  const hasHeader = title != null || subtitle != null || headerEnd != null

  return (
    <section className={cn(REPORTS_SECTION_CLASS, className)}>
      {hasHeader ? (
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className={REPORTS_SECTION_HEADER_CLASS}>
            {title != null ? (
              <h2 className={REPORTS_SECTION_TITLE_CLASS}>{title}</h2>
            ) : null}
            {subtitle != null ? (
              <p className={REPORTS_SECTION_SUBTITLE_CLASS}>{subtitle}</p>
            ) : null}
          </div>
          {headerEnd != null ? (
            <div className="shrink-0">{headerEnd}</div>
          ) : null}
        </div>
      ) : null}
      {children}
    </section>
  )
}

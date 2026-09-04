import type { ReactNode } from "react"

import { AiIcon } from "@/components/ui/ai-icon"
import {
  REPORTS_INSIGHT_BANNER_CLASS,
  REPORTS_INSIGHT_BODY_CLASS,
  REPORTS_INSIGHT_TITLE_CLASS,
} from "@/lib/operatorReports/reportsPresentation"
import { cn } from "@/lib/utils"

type ReportsInsightBannerProps = {
  title?: string
  children: ReactNode
  className?: string
}

/** Shared AI / insight callout used inside report sections. */
export function ReportsInsightBanner({
  title,
  children,
  className,
}: ReportsInsightBannerProps) {
  return (
    <div className={cn(REPORTS_INSIGHT_BANNER_CLASS, className)}>
      <AiIcon size={16} className="mt-0.5" />
      <div className="flex min-w-0 flex-col gap-1">
        {title != null ? (
          <p className={REPORTS_INSIGHT_TITLE_CLASS}>{title}</p>
        ) : null}
        <div className={REPORTS_INSIGHT_BODY_CLASS}>{children}</div>
      </div>
    </div>
  )
}

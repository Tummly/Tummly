import { Fragment } from "react"

import type { OperatorCampaignsSummaryViewModel } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import {
  GUESTS_KPI_CELL_CLASS,
  GUESTS_KPI_CONTENT_CLASS,
  GUESTS_KPI_DIVIDER_CLASS,
  GUESTS_KPI_HELPER_CLASS,
  GUESTS_KPI_META_STACK_CLASS,
  GUESTS_KPI_ROW_CLASS,
  GUESTS_KPI_STRIP_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import { cn } from "@/lib/utils"

type CampaignsSummaryProps = {
  summary: OperatorCampaignsSummaryViewModel
}

/** Campaign summary — Guests/Feedback-style KPI strip (Figma 3462:61952). */
export function CampaignsSummary({ summary }: CampaignsSummaryProps) {
  return (
    <section className={GUESTS_SECTION_CLASS} aria-label={summary.title}>
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{summary.title}</h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{summary.subtitle}</p>
      </header>

      <div className={GUESTS_KPI_STRIP_CLASS}>
        <div className={GUESTS_KPI_ROW_CLASS}>
          {summary.kpis.map((kpi, index) => (
            <Fragment key={kpi.id}>
              {index > 0 ? (
                <div aria-hidden className={GUESTS_KPI_DIVIDER_CLASS} />
              ) : null}
              <div className={cn(GUESTS_KPI_CELL_CLASS, "h-full")}>
                <div className={cn(GUESTS_KPI_CONTENT_CLASS, "h-full")}>
                  <div className="leading-[0]">
                    <p className={PERFORMANCE_KPI_LABEL_CLASS}>{kpi.label}</p>
                  </div>
                  <div className="leading-[0]">
                    <p className={PERFORMANCE_KPI_VALUE_CLASS}>
                      {(kpi.value ?? 0).toLocaleString("en-GB")}
                    </p>
                  </div>
                  {kpi.description ? (
                    <div className={cn(GUESTS_KPI_META_STACK_CLASS, "mt-auto")}>
                      <p className={GUESTS_KPI_HELPER_CLASS}>{kpi.description}</p>
                    </div>
                  ) : null}
                </div>
              </div>
            </Fragment>
          ))}
        </div>
      </div>
    </section>
  )
}

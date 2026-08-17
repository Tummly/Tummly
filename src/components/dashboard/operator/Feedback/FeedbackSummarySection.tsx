import { Fragment } from "react"

import { Button } from "@/components/ui/button"
import {
  FEEDBACK_KPI_CELL_CLASS,
  FEEDBACK_KPI_COMPARISON_CLASS,
  FEEDBACK_KPI_CONTENT_CLASS,
  FEEDBACK_KPI_DIVIDER_CLASS,
  FEEDBACK_KPI_META_STACK_CLASS,
  FEEDBACK_KPI_ROW_CLASS,
  FEEDBACK_KPI_SHARE_CLASS,
  FEEDBACK_KPI_SHARE_STUB_CLASS,
  FEEDBACK_KPI_STRIP_CLASS,
  FEEDBACK_PAGE_COPY,
  FEEDBACK_SUMMARY_SUBTITLE_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import {
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type { OperatorFeedbackSummarySection } from "@/types/operatorFeedback"

type FeedbackSummarySectionProps = {
  summary: OperatorFeedbackSummarySection
  onChangePeriod: () => void
  onViewCapture: () => void
}

/** Feedback summary — four-cell KPI strip or empty card (no in-section date control). */
export function FeedbackSummarySection({
  summary,
  onChangePeriod,
  onViewCapture,
}: FeedbackSummarySectionProps) {
  const copy = FEEDBACK_PAGE_COPY.summary

  return (
    <section className={GUESTS_SECTION_CLASS}>
      <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
        <header className="flex flex-col gap-2 leading-[0]">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.title}</h2>
          <p className={FEEDBACK_SUMMARY_SUBTITLE_CLASS}>{copy.subtitle}</p>
        </header>
      </div>

      {summary.kind === "empty" ? (
        <div className="flex flex-col items-center gap-4 py-6 text-center">
          <div className="flex flex-col items-center gap-2.5">
            <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>{copy.emptyTitle}</p>
            <p className={`${GUESTS_TABLE_EMPTY_HELPER_CLASS} max-w-none whitespace-nowrap`}>
              {copy.emptyHelper}
            </p>
          </div>
          <div className="flex flex-wrap items-center justify-center gap-3">
            <Button
              type="button"
              variant="op-secondary"
              onClick={onChangePeriod}
            >
              {copy.changePeriod}
            </Button>
            <Button type="button" variant="op-tertiary" onClick={onViewCapture}>
              {copy.viewCapture}
            </Button>
          </div>
        </div>
      ) : (
        <div className={FEEDBACK_KPI_STRIP_CLASS}>
          <div className={FEEDBACK_KPI_ROW_CLASS}>
            {summary.kpis.map((kpi, index) => (
              <Fragment key={kpi.id}>
                {index > 0 ? (
                  <div aria-hidden className={FEEDBACK_KPI_DIVIDER_CLASS} />
                ) : null}
                <div className={FEEDBACK_KPI_CELL_CLASS}>
                  <div className={FEEDBACK_KPI_CONTENT_CLASS}>
                    <div className="leading-[0]">
                      <p className={PERFORMANCE_KPI_LABEL_CLASS}>{kpi.label}</p>
                    </div>
                    <div className="leading-[0]">
                      <p className={PERFORMANCE_KPI_VALUE_CLASS}>{kpi.value}</p>
                    </div>
                    <div className={FEEDBACK_KPI_META_STACK_CLASS}>
                      {kpi.shareLabel != null ? (
                        <p className={FEEDBACK_KPI_SHARE_CLASS}>
                          {kpi.shareLabel}
                        </p>
                      ) : (
                        <p aria-hidden className={FEEDBACK_KPI_SHARE_STUB_CLASS}>
                          0% of feedback
                        </p>
                      )}
                      <p className={FEEDBACK_KPI_COMPARISON_CLASS}>
                        {kpi.comparisonLabel}
                      </p>
                    </div>
                  </div>
                </div>
              </Fragment>
            ))}
          </div>
        </div>
      )}
    </section>
  )
}

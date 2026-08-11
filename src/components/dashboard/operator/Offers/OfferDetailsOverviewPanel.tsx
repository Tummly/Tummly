import { GuestProfileDetailRows } from "@/components/dashboard/operator/GuestProfile/GuestProfileDetailRows"
import { OfferDetailsDateRangeControl } from "@/components/dashboard/operator/Offers/OfferDetailsDateRangeControl"
import { SparklesIcon } from "lucide-react"

import {
  OFFERS_KPI_HELPER_CLASS,
  OFFERS_KPI_HELPER_ROW_CLASS,
} from "@/lib/operatorOffers/offersPresentation"
import type { OfferDetailsOverviewViewModel } from "@/lib/operatorOffers/createOfferDetailsPageModule"
import type { OfferDetailsDateRange } from "@/lib/operatorOffers/offerDetailsPresentation"
import {
  PERFORMANCE_KPI_CELL_CLASS,
  PERFORMANCE_KPI_CONTENT_CLASS,
  PERFORMANCE_KPI_DIVIDER_CLASS,
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_ROW_CLASS,
  PERFORMANCE_KPI_STRIP_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
  PERFORMANCE_SECTION_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import {
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { Fragment } from "react"

type OfferDetailsOverviewPanelProps = {
  overview: OfferDetailsOverviewViewModel
  onCommitDateRange: (range: OfferDetailsDateRange) => void
}

export function OfferDetailsOverviewPanel({
  overview,
  onCommitDateRange,
}: OfferDetailsOverviewPanelProps) {
  return (
    <div className="flex flex-col gap-5">
      <section
        className={PERFORMANCE_SECTION_CLASS}
        aria-label="Overview performance"
      >
        <div className={PERFORMANCE_KPI_STRIP_CLASS}>
          <div className={PERFORMANCE_KPI_ROW_CLASS}>
            {overview.kpis.map((kpi, index) => (
              <Fragment key={kpi.id}>
                {index > 0 ? (
                  <div aria-hidden className={PERFORMANCE_KPI_DIVIDER_CLASS} />
                ) : null}
                <div className={PERFORMANCE_KPI_CELL_CLASS}>
                  <div className={PERFORMANCE_KPI_CONTENT_CLASS}>
                    <div className="leading-[0]">
                      <p className={PERFORMANCE_KPI_LABEL_CLASS}>{kpi.label}</p>
                    </div>
                    <div className="leading-[0]">
                      <p className={PERFORMANCE_KPI_VALUE_CLASS}>
                        {kpi.primaryText}
                      </p>
                    </div>
                    <div className={OFFERS_KPI_HELPER_ROW_CLASS}>
                      <p className={OFFERS_KPI_HELPER_CLASS}>{kpi.helperText}</p>
                    </div>
                  </div>
                </div>
              </Fragment>
            ))}
          </div>
        </div>
      </section>

      <section className={GUESTS_SECTION_CLASS}>
        <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>
            {overview.definitionTitle}
          </h2>
          <OfferDetailsDateRangeControl
            dateRangeLabel={overview.dateRangeLabel}
            selectedRange={overview.dateRange}
            onCommitRange={onCommitDateRange}
          />
        </div>
        <GuestProfileDetailRows rows={overview.definitionFields} />
      </section>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>
            {overview.recommendation.title}
          </h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {overview.recommendation.subtitle}
          </p>
        </div>
        <div className="flex flex-col gap-3 rounded-op-lg border border-op-card-border bg-op-surface-secondary p-5">
          <div className="flex items-start gap-3">
            <SparklesIcon
              className="mt-0.5 size-5 shrink-0 text-op-action-primary"
              aria-hidden
            />
            <div className="flex min-w-0 flex-col gap-1">
              <p className="m-0 text-base font-semibold text-foreground">
                {overview.recommendation.emptyTitle}
              </p>
              <p className="m-0 text-sm text-op-text-secondary">
                {overview.recommendation.emptyHelper}
              </p>
            </div>
          </div>
        </div>
      </section>
    </div>
  )
}

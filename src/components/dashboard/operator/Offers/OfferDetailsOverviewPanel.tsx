import { Fragment } from "react"

import { GuestProfileDetailRows } from "@/components/dashboard/operator/GuestProfile/GuestProfileDetailRows"
import { OfferDetailsDateRangeControl } from "@/components/dashboard/operator/Offers/OfferDetailsDateRangeControl"
import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import type { OfferDetailsOverviewViewModel } from "@/lib/operatorOffers/createOfferDetailsPageModule"
import {
  OFFER_DETAILS_COPY,
  type OfferDetailsDateRange,
} from "@/lib/operatorOffers/offerDetailsPresentation"
import {
  OFFERS_KPI_HELPER_CLASS,
  OFFERS_KPI_HELPER_ROW_CLASS,
} from "@/lib/operatorOffers/offersPresentation"
import {
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
  OPERATOR_HOME_WHITE_CARD_TITLE_CLASS,
  RECOMMENDED_EMPTY_COPY_CLASS,
  RECOMMENDED_HEADER_CLASS,
  RECOMMENDED_INNER_PANEL_CLASS,
  RECOMMENDED_SECTION_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
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
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type OfferDetailsOverviewPanelProps = {
  overview: OfferDetailsOverviewViewModel
  onCommitDateRange: (range: OfferDetailsDateRange) => void
  onRetryRecommendation: () => void
}

export function OfferDetailsOverviewPanel({
  overview,
  onCommitDateRange,
  onRetryRecommendation,
}: OfferDetailsOverviewPanelProps) {
  const recommendation = overview.recommendation

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

      <section
        className={RECOMMENDED_SECTION_CLASS}
        aria-label={recommendation.title}
      >
        <div className={RECOMMENDED_HEADER_CLASS}>
          <AiIcon size={32} />
          <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
            <h2 className={OPERATOR_HOME_WHITE_CARD_TITLE_CLASS}>
              {recommendation.title}
            </h2>
            <p className={OPERATOR_HOME_SUBTITLE_CLASS}>
              {recommendation.subtitle}
            </p>
          </div>
        </div>

        {recommendation.status === "loading"
        || recommendation.status === "idle" ? (
          <div
            className={`${RECOMMENDED_INNER_PANEL_CLASS} flex min-h-[120px] items-center justify-center`}
            role="status"
            aria-live="polite"
            aria-label="Loading recommendation"
          >
            <Spinner />
          </div>
        ) : null}

        {recommendation.status === "error" ? (
          <div className={`${RECOMMENDED_INNER_PANEL_CLASS} flex flex-col gap-3`}>
            <p className={RECOMMENDED_EMPTY_COPY_CLASS}>
              {recommendation.errorMessage
                ?? OFFER_DETAILS_COPY.recommendedFailCopy}
            </p>
            {recommendation.errorRetryable ? (
              <div>
                <Button
                  type="button"
                  variant="op-secondary"
                  className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                  onClick={onRetryRecommendation}
                >
                  {OFFER_DETAILS_COPY.recommendedRetry}
                </Button>
              </div>
            ) : null}
          </div>
        ) : null}

        {recommendation.status === "ready" && recommendation.isNone ? (
          <div className={RECOMMENDED_INNER_PANEL_CLASS}>
            <p className={RECOMMENDED_EMPTY_COPY_CLASS}>
              {recommendation.emptyCopy}
            </p>
          </div>
        ) : null}

        {recommendation.status === "ready"
        && recommendation.recommendation != null ? (
          <div className={RECOMMENDED_INNER_PANEL_CLASS}>
            <p className={RECOMMENDED_EMPTY_COPY_CLASS}>
              {recommendation.recommendation.title
                ?? recommendation.emptyCopy}
            </p>
          </div>
        ) : null}
      </section>
    </div>
  )
}

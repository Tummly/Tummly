import { Link } from "react-router-dom"

import { OffersRedemptionLogEmptyState } from "@/components/dashboard/operator/Offers/OffersRedemptionLogEmptyState"
import { OffersRedemptionLogTable } from "@/components/dashboard/operator/Offers/OffersRedemptionLogTable"
import type { OperatorOffersRedemptionLogViewModel } from "@/lib/operatorOffers/createOperatorOffersRedemptionLogModule"
import { OFFERS_PAGE_META_CLASS } from "@/lib/operatorOffers/offersPresentation"
import {
  CAPTURE_BREADCRUMB_CURRENT_CLASS,
  CAPTURE_BREADCRUMB_LINK_CLASS,
  CAPTURE_BREADCRUMB_NAV_CLASS,
  CAPTURE_PAGE_HEADER_COPY_CLASS,
  CAPTURE_PAGE_STACK_CLASS,
  CAPTURE_PAGE_SUBTITLE_CLASS,
  CAPTURE_PAGE_TITLE_CLASS,
} from "@/lib/operatorCapture/capturePresentation"

type OffersRedemptionLogBodyProps = {
  viewModel: OperatorOffersRedemptionLogViewModel
  offersHref: string
  onRetry: () => void
}

/** Location-wide redemption log page body — table chrome + honest empty. */
export function OffersRedemptionLogBody({
  viewModel,
  offersHref,
  onRetry,
}: OffersRedemptionLogBodyProps) {
  return (
    <div className={CAPTURE_PAGE_STACK_CLASS}>
      <nav className={CAPTURE_BREADCRUMB_NAV_CLASS} aria-label="Breadcrumb">
        <Link to={offersHref} className={CAPTURE_BREADCRUMB_LINK_CLASS}>
          {viewModel.backLabel}
        </Link>
        <span className={CAPTURE_BREADCRUMB_CURRENT_CLASS} aria-hidden>
          /
        </span>
        <span className={CAPTURE_BREADCRUMB_CURRENT_CLASS}>
          {viewModel.title}
        </span>
      </nav>

      <header className={CAPTURE_PAGE_HEADER_COPY_CLASS}>
        <h1 className={CAPTURE_PAGE_TITLE_CLASS}>{viewModel.title}</h1>
        <p className={CAPTURE_PAGE_SUBTITLE_CLASS}>{viewModel.subtitle}</p>
        <p className={OFFERS_PAGE_META_CLASS}>{viewModel.locationName}</p>
      </header>

      <OffersRedemptionLogTable
        columns={viewModel.columns}
        rows={viewModel.rows}
      />

      {viewModel.rows.length === 0 ? (
        <OffersRedemptionLogEmptyState
          empty={viewModel.empty}
          onRetry={onRetry}
        />
      ) : null}
    </div>
  )
}

import { OffersNeedsAttentionSection } from "@/components/dashboard/operator/Offers/OffersNeedsAttentionSection"
import { OffersPerformanceSection } from "@/components/dashboard/operator/Offers/OffersPerformanceSection"
import { Button } from "@/components/ui/button"
import type { OperatorOffersPageViewModel } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import {
  OFFERS_PAGE_COPY,
  OFFERS_PAGE_META_CLASS,
} from "@/lib/operatorOffers/offersPresentation"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type OffersBodyProps = {
  viewModel: OperatorOffersPageViewModel
  onCommitPerformanceDateRange: (range: HomePerformanceDateRange) => void
}

/** Offers page shell — header, Performance, Needs attention; list later. */
export function OffersBody({
  viewModel,
  onCommitPerformanceDateRange,
}: OffersBodyProps) {
  const copy = OFFERS_PAGE_COPY

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <header className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>{copy.subtitle}</p>
          <p className={OFFERS_PAGE_META_CLASS}>{viewModel.locationName}</p>
        </header>

        <div className="flex shrink-0 flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="op-primary"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            disabled
          >
            {viewModel.header.createOfferLabel}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            disabled
          >
            {viewModel.header.openStaffRedeemLabel}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            disabled
          >
            {viewModel.header.viewRedemptionLogLabel}
          </Button>
        </div>
      </div>

      <OffersPerformanceSection
        performance={viewModel.performance}
        onCommitRange={onCommitPerformanceDateRange}
      />

      <OffersNeedsAttentionSection
        needsAttention={viewModel.needsAttention}
      />

      <section className={GUESTS_SECTION_CLASS} aria-label={copy.listSlotLabel}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.listSlotLabel}</h2>
      </section>
    </div>
  )
}

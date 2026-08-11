import { Button } from "@/components/ui/button"
import type { OperatorOffersPageViewModel } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import {
  OFFERS_PAGE_COPY,
  OFFERS_PAGE_META_CLASS,
} from "@/lib/operatorOffers/offersPresentation"
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
}

/** Offers page shell — Guests/Campaigns layout chrome; list/Performance children later. */
export function OffersBody({ viewModel }: OffersBodyProps) {
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

      <section className={GUESTS_SECTION_CLASS} aria-label={copy.performanceSlotLabel}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.performanceSlotLabel}</h2>
      </section>

      <section
        className={GUESTS_SECTION_CLASS}
        aria-label={copy.needsAttentionSlotLabel}
      >
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>
          {copy.needsAttentionSlotLabel}
        </h2>
      </section>

      <section className={GUESTS_SECTION_CLASS} aria-label={copy.listSlotLabel}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.listSlotLabel}</h2>
      </section>
    </div>
  )
}

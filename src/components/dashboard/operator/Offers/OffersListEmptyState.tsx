import { Button } from "@/components/ui/button"
import type { OperatorOffersListEmptyViewModel } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import {
  OFFERS_PAGE_COPY,
  OFFERS_TRUE_EMPTY_ACTIONS_CLASS,
  OFFERS_TRUE_EMPTY_HELPER_CLASS,
} from "@/lib/operatorOffers/offersPresentation"
import {
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type OffersListEmptyStateProps = {
  empty: OperatorOffersListEmptyViewModel
  onCreateOffer?: () => void
  onUseTemplate?: () => void
  onViewAllOffers?: () => void
  onClearAllFilters?: () => void
}

/** Campaigns-style empties adapted for Offers (ticket 03 / 20). */
export function OffersListEmptyState({
  empty,
  onCreateOffer,
  onUseTemplate,
  onViewAllOffers,
  onClearAllFilters,
}: OffersListEmptyStateProps) {
  return (
    <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
      <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
        <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>{empty.title}</p>
        <p className={OFFERS_TRUE_EMPTY_HELPER_CLASS}>{empty.helper}</p>
      </div>

      {empty.kind === "true-empty" ? (
        <div className={OFFERS_TRUE_EMPTY_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-primary"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            disabled
            onClick={onCreateOffer}
          >
            {empty.createOfferLabel ?? OFFERS_PAGE_COPY.createOffer}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            disabled
            onClick={onUseTemplate}
          >
            {empty.useTemplateLabel ?? OFFERS_PAGE_COPY.useTemplate}
          </Button>
        </div>
      ) : null}

      {empty.kind === "filter-search" ? (
        <div className={OFFERS_TRUE_EMPTY_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onViewAllOffers}
          >
            {empty.viewAllOffersLabel ?? OFFERS_PAGE_COPY.viewAllOffers}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onClearAllFilters}
          >
            {empty.clearAllFiltersLabel ?? OFFERS_PAGE_COPY.clearAllFilters}
          </Button>
        </div>
      ) : null}
    </div>
  )
}

import { Button } from "@/components/ui/button"
import type { OfferDetailsLifecycleEmptyState as EmptyVm } from "@/lib/operatorOffers/offerDetailsPresentation"
import {
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_TABLE_EMPTY_ACTIONS_CLASS,
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type OfferDetailsLifecycleEmptyStateProps = {
  empty: EmptyVm
  onPrimaryCta?: () => void
}

/** Honest-empty chrome for Claims / Redemptions / Campaigns / Void (ticket 24). */
export function OfferDetailsLifecycleEmptyState({
  empty,
  onPrimaryCta,
}: OfferDetailsLifecycleEmptyStateProps) {
  return (
    <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
      <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
        <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>{empty.title}</p>
        <p className={GUESTS_TABLE_EMPTY_HELPER_CLASS}>{empty.helper}</p>
      </div>
      {empty.primaryCtaLabel != null ? (
        <div className={GUESTS_TABLE_EMPTY_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-primary"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            onClick={onPrimaryCta}
          >
            {empty.primaryCtaLabel}
          </Button>
        </div>
      ) : null}
    </div>
  )
}

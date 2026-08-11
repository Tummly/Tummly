import { Button } from "@/components/ui/button"
import {
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_TABLE_EMPTY_ACTIONS_CLASS,
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type OffersRedemptionLogEmptyStateProps = {
  empty: {
    title: string
    helper: string
    retryLabel: string
  }
  onRetry: () => void
}

/** Honest empty for location-wide redemption log — Retry until API. */
export function OffersRedemptionLogEmptyState({
  empty,
  onRetry,
}: OffersRedemptionLogEmptyStateProps) {
  return (
    <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
      <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
        <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>{empty.title}</p>
        <p className={GUESTS_TABLE_EMPTY_HELPER_CLASS}>{empty.helper}</p>
      </div>
      <div className={GUESTS_TABLE_EMPTY_ACTIONS_CLASS}>
        <Button
          type="button"
          variant="op-secondary"
          className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          onClick={onRetry}
        >
          {empty.retryLabel}
        </Button>
      </div>
    </div>
  )
}

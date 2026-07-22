import { Button } from "@/components/ui/button"
import {
  GUESTS_TABLE_EMPTY_ACTIONS_CLASS,
  GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS,
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
  OPERATOR_GUESTS_TABLE_EMPTY_COPY,
} from "@/lib/operatorGuests/guestsPresentation"
import type { GuestsTableEmptyStateKind } from "@/types/operatorGuests"

type GuestsTableEmptyStateProps = {
  kind: GuestsTableEmptyStateKind
  onClearSearchAndFilters?: () => void
}

/** Figma table empty states — 3388:16065 / 3388:15695 (light). */
export function GuestsTableEmptyState({
  kind,
  onClearSearchAndFilters,
}: GuestsTableEmptyStateProps) {
  const copy = OPERATOR_GUESTS_TABLE_EMPTY_COPY[kind]

  return (
    <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
      <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
        <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>{copy.title}</p>
        <p className={GUESTS_TABLE_EMPTY_HELPER_CLASS}>{copy.helper}</p>
      </div>

      {kind === "no-guests-found" && onClearSearchAndFilters ? (
        <div className={GUESTS_TABLE_EMPTY_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="operator-tertiary"
            onClick={onClearSearchAndFilters}
            className={GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS}
          >
            Clear search and filters
          </Button>
        </div>
      ) : null}
    </div>
  )
}

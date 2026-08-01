import { Button } from "@/components/ui/button"
import {
  GUESTS_TABLE_EMPTY_ACTIONS_CLASS,
  GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS,
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { OPERATOR_FEEDBACK_INBOX_EMPTY_COPY } from "@/lib/operatorFeedback/feedbackPresentation"
import type { OperatorFeedbackInboxEmptyStateKind } from "@/types/operatorFeedback"

type FeedbackInboxTableEmptyStateProps = {
  kind: OperatorFeedbackInboxEmptyStateKind
  onClearSearchAndFilters?: () => void
  onChangePeriod?: () => void
}

/** Feedback inbox table empty — no-match and true-empty (PRD). */
export function FeedbackInboxTableEmptyState({
  kind,
  onClearSearchAndFilters,
  onChangePeriod,
}: FeedbackInboxTableEmptyStateProps) {
  const copy = OPERATOR_FEEDBACK_INBOX_EMPTY_COPY[kind]

  return (
    <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
      <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
        <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>{copy.title}</p>
        <p className={GUESTS_TABLE_EMPTY_HELPER_CLASS}>{copy.helper}</p>
      </div>

      {kind === "no-match" && onClearSearchAndFilters ? (
        <div className={GUESTS_TABLE_EMPTY_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-tertiary"
            onClick={onClearSearchAndFilters}
            className={GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS}
          >
            {copy.actionLabel}
          </Button>
        </div>
      ) : null}

      {kind === "true-empty" && onChangePeriod ? (
        <div className={GUESTS_TABLE_EMPTY_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-tertiary"
            onClick={onChangePeriod}
            className={GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS}
          >
            {copy.actionLabel}
          </Button>
        </div>
      ) : null}
    </div>
  )
}

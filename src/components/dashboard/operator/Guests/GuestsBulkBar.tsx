import { Button } from "@/components/ui/button"
import {
  GUESTS_BULK_BAR_ACTIONS_CLASS,
  GUESTS_BULK_BAR_CLEAR_BUTTON_CLASS,
  GUESTS_BULK_BAR_DISABLED_ACTION_CLASS,
  GUESTS_BULK_BAR_LABEL_CLASS,
  GUESTS_BULK_BAR_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestsBulkBarProps = {
  selectionLabel: string
  onClearSelection: () => void
}

/** Figma bulk bar — nodes 3388:14440–3388:14446. */
export function GuestsBulkBar({
  selectionLabel,
  onClearSelection,
}: GuestsBulkBarProps) {
  return (
    <div
      role="status"
      aria-live="polite"
      className={GUESTS_BULK_BAR_ROW_CLASS}
    >
      <p className={GUESTS_BULK_BAR_LABEL_CLASS}>{selectionLabel}</p>

      <div className={GUESTS_BULK_BAR_ACTIONS_CLASS}>
        <Button
          type="button"
          variant="ghost"
          disabled
          aria-disabled
          className={GUESTS_BULK_BAR_DISABLED_ACTION_CLASS}
        >
          Create campaign
        </Button>
        <Button
          type="button"
          variant="ghost"
          disabled
          aria-disabled
          className={GUESTS_BULK_BAR_DISABLED_ACTION_CLASS}
        >
          Add tag
        </Button>
        <Button
          type="button"
          variant="ghost"
          disabled
          aria-disabled
          className={GUESTS_BULK_BAR_DISABLED_ACTION_CLASS}
        >
          Export selected
        </Button>
        <Button
          type="button"
          variant="link"
          onClick={onClearSelection}
          className={GUESTS_BULK_BAR_CLEAR_BUTTON_CLASS}
        >
          Clear selection
        </Button>
      </div>
    </div>
  )
}

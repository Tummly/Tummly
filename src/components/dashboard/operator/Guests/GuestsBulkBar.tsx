import { Button } from "@/components/ui/button"
import {
  GUESTS_BULK_BAR_ACTIONS_CLASS,
  GUESTS_BULK_BAR_CLEAR_BUTTON_CLASS,
  GUESTS_BULK_BAR_LABEL_CLASS,
  GUESTS_BULK_BAR_PRIMARY_BUTTON_CLASS,
  GUESTS_BULK_BAR_ROW_CLASS,
  GUESTS_BULK_BAR_TERTIARY_BUTTON_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestsBulkBarProps = {
  selectionLabel: string
  onClearSelection: () => void
  onAddTag?: () => void
  onExportSelected?: () => void
  exportBusy?: boolean
}

/** Figma bulk / row-selection actions — nodes 3388:14440–3388:14446. */
export function GuestsBulkBar({
  selectionLabel,
  onClearSelection,
  onAddTag,
  onExportSelected,
  exportBusy = false,
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
          variant="op-primary"
          disabled
          aria-disabled
          className={GUESTS_BULK_BAR_PRIMARY_BUTTON_CLASS}
        >
          Create campaign
        </Button>
        <Button
          type="button"
          variant="op-tertiary"
          disabled={onAddTag == null}
          aria-disabled={onAddTag == null}
          className={GUESTS_BULK_BAR_TERTIARY_BUTTON_CLASS}
          onClick={onAddTag}
        >
          Add tag
        </Button>
        <Button
          type="button"
          variant="op-tertiary"
          disabled={onExportSelected == null || exportBusy}
          aria-disabled={onExportSelected == null || exportBusy}
          className={GUESTS_BULK_BAR_TERTIARY_BUTTON_CLASS}
          onClick={onExportSelected}
        >
          Export selected
        </Button>
        <Button
          type="button"
          variant="op-link"
          onClick={onClearSelection}
          className={GUESTS_BULK_BAR_CLEAR_BUTTON_CLASS}
        >
          Clear selection
        </Button>
      </div>
    </div>
  )
}

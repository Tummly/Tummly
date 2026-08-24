import { OperatorDestructiveConfirmDialog } from "@/components/dashboard/operator/OperatorDestructiveConfirmDialog"
import { OPERATOR_NOTE_ACTIONS } from "@/lib/operatorGuestProfile/guestProfilePresentation"

type OperatorNoteDeleteDialogProps = {
  open: boolean
  busy?: boolean
  error?: string | null
  onOpenChange: (open: boolean) => void
  onConfirm: () => void | Promise<void>
}

/** Shared confirm dialog for soft-deleting Feedback or Location Guest notes. */
export function OperatorNoteDeleteDialog({
  open,
  busy = false,
  error = null,
  onOpenChange,
  onConfirm,
}: OperatorNoteDeleteDialogProps) {
  return (
    <OperatorDestructiveConfirmDialog
      open={open}
      busy={busy}
      error={error}
      title={OPERATOR_NOTE_ACTIONS.deleteDialogTitle}
      description={OPERATOR_NOTE_ACTIONS.deleteDialogDescription}
      confirmLabel={OPERATOR_NOTE_ACTIONS.deleteDialogConfirm}
      cancelLabel={OPERATOR_NOTE_ACTIONS.deleteDialogCancel}
      onOpenChange={onOpenChange}
      onConfirm={onConfirm}
    />
  )
}

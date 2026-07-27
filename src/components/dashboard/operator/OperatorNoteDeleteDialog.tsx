import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
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
    <AlertDialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && busy) {
          return
        }
        onOpenChange(nextOpen)
      }}
    >
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>
            {OPERATOR_NOTE_ACTIONS.deleteDialogTitle}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {OPERATOR_NOTE_ACTIONS.deleteDialogDescription}
          </AlertDialogDescription>
        </AlertDialogHeader>
        {error != null ? (
          <p className="text-sm text-destructive" role="alert">
            {error}
          </p>
        ) : null}
        <AlertDialogFooter>
          <AlertDialogCancel disabled={busy}>
            {OPERATOR_NOTE_ACTIONS.deleteDialogCancel}
          </AlertDialogCancel>
          <AlertDialogAction
            variant="destructive"
            disabled={busy}
            onClick={(event) => {
              event.preventDefault()
              void onConfirm()
            }}
          >
            {busy
              ? "Deleting…"
              : OPERATOR_NOTE_ACTIONS.deleteDialogConfirm}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

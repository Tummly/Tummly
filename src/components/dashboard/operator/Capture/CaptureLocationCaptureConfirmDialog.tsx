import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  CAPTURE_PAUSE_ACTIVATE_DIALOG_BODY_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_CONTENT_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_FOOTER_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_TITLE_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import type { OperatorMultiCaptureLocationCaptureConfirmSnapshot } from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"

type CaptureLocationCaptureConfirmDialogProps = {
  snapshot: OperatorMultiCaptureLocationCaptureConfirmSnapshot
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
}

/**
 * Stub Pause/Activate location capture confirm — mutations land in ticket 22.
 * Reuses Pause/Activate confirm dialog chrome tokens.
 */
export function CaptureLocationCaptureConfirmDialog({
  snapshot,
  onOpenChange,
  onConfirm,
}: CaptureLocationCaptureConfirmDialogProps) {
  const isPause = snapshot.kind === "pause"
  const title = isPause
    ? "Pause location capture?"
    : "Activate location capture?"
  const description = isPause
    ? `Pause capture for ${snapshot.locationName}? Full confirm copy and mutations ship in a follow-up.`
    : `Activate capture for ${snapshot.locationName}? Full confirm copy and mutations ship in a follow-up.`
  const primaryLabel = isPause
    ? "Pause location capture"
    : "Activate location capture"

  return (
    <Dialog open={snapshot.isOpen} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton
        className={CAPTURE_PAUSE_ACTIVATE_DIALOG_CONTENT_CLASS}
      >
        <DialogHeader className="gap-3 pr-10">
          <DialogTitle className={CAPTURE_PAUSE_ACTIVATE_DIALOG_TITLE_CLASS}>
            {title}
          </DialogTitle>
          <DialogDescription
            className={CAPTURE_PAUSE_ACTIVATE_DIALOG_BODY_CLASS}
          >
            {description}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter className={CAPTURE_PAUSE_ACTIVATE_DIALOG_FOOTER_CLASS}>
          <Button
            type="button"
            variant="op-primary"
            onClick={() => {
              onConfirm()
            }}
          >
            {primaryLabel}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            onClick={() => {
              onOpenChange(false)
            }}
          >
            Cancel
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

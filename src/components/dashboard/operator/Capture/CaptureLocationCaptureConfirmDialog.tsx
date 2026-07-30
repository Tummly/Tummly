import type { ReactNode } from "react"
import { XIcon } from "lucide-react"

import { Badge } from "@/components/ui/badge"
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
  CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_LABEL_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_TITLE_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_WARNING_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import type { OperatorMultiCaptureLocationCaptureConfirmSnapshot } from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"

type CaptureLocationCaptureConfirmDialogProps = {
  snapshot: OperatorMultiCaptureLocationCaptureConfirmSnapshot
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
}

function DetailRow({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_CLASS}>
      <p className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_LABEL_CLASS}>{label}:</p>
      <div className="min-w-0">{children}</div>
    </div>
  )
}

/** Pause / Activate location capture confirm — shared PauseActivate chrome. */
export function CaptureLocationCaptureConfirmDialog({
  snapshot,
  onOpenChange,
  onConfirm,
}: CaptureLocationCaptureConfirmDialogProps) {
  const details = snapshot.details
  const busy = snapshot.busy

  return (
    <Dialog
      open={snapshot.isOpen && details != null}
      onOpenChange={(open) => {
        if (!open && busy) {
          return
        }
        onOpenChange(open)
      }}
    >
      <DialogContent
        className={CAPTURE_PAUSE_ACTIVATE_DIALOG_CONTENT_CLASS}
        showCloseButton={false}
      >
        {details != null ? (
          <>
            <DialogHeader className="gap-[30px]">
              <div className="flex items-start gap-[22px]">
                <div className="flex min-w-0 flex-1 flex-col gap-3">
                  <DialogTitle
                    className={CAPTURE_PAUSE_ACTIVATE_DIALOG_TITLE_CLASS}
                  >
                    {details.title}
                  </DialogTitle>
                  <DialogDescription
                    className={CAPTURE_PAUSE_ACTIVATE_DIALOG_BODY_CLASS}
                  >
                    {details.body}
                  </DialogDescription>
                </div>
                <Button
                  type="button"
                  variant="op-ghost"
                  size="icon"
                  disabled={busy}
                  aria-label="Close"
                  className="size-[42px] shrink-0 rounded-[2px] bg-[var(--op-color-gray-950)] text-white hover:bg-[var(--op-color-gray-950)]/90 hover:text-white"
                  onClick={() => onOpenChange(false)}
                >
                  <XIcon className="size-[18px]" aria-hidden />
                </Button>
              </div>

              <div className="h-px w-full bg-[var(--op-color-gray-980)]" />

              <div className="flex flex-col gap-5">
                <DetailRow label="Location">
                  <p className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS}>
                    {details.locationName}
                  </p>
                </DetailRow>
                <DetailRow label="Current status">
                  <Badge variant="soft">{details.currentStatus}</Badge>
                </DetailRow>
                <DetailRow label={details.codesCountLabel}>
                  <p className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS}>
                    {details.codesCount}
                  </p>
                </DetailRow>
              </div>

              {details.warningText != null ? (
                <>
                  <div className="h-px w-full bg-[var(--op-color-gray-980)]" />
                  <div
                    className={CAPTURE_PAUSE_ACTIVATE_DIALOG_WARNING_CLASS}
                    role="note"
                  >
                    {details.warningText}
                  </div>
                </>
              ) : null}
            </DialogHeader>

            <DialogFooter className={CAPTURE_PAUSE_ACTIVATE_DIALOG_FOOTER_CLASS}>
              <Button
                type="button"
                variant="op-primary"
                disabled={busy}
                onClick={onConfirm}
              >
                {details.primaryLabel}
              </Button>
              <Button
                type="button"
                variant="op-tertiary"
                disabled={busy}
                onClick={() => onOpenChange(false)}
              >
                {details.cancelLabel}
              </Button>
            </DialogFooter>
          </>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}

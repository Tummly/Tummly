import type { ReactNode } from "react"
import { XIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import type { RestoreConfirmSnapshot } from "@/lib/operatorCapture/createOperatorCapturePageModule"
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

type CaptureRestoreConfirmDialogProps = {
  snapshot: RestoreConfirmSnapshot
  busy?: boolean
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

/** Restore confirm dialogue — physical print warning or digital guest URL warning. */
export function CaptureRestoreConfirmDialog({
  snapshot,
  busy = false,
  onOpenChange,
  onConfirm,
}: CaptureRestoreConfirmDialogProps) {
  const details = snapshot.details

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
                <DetailRow label={details.nameLabel}>
                  <p className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS}>
                    {details.nameValue}
                  </p>
                </DetailRow>
                <DetailRow label="Location">
                  <p className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS}>
                    {details.locationName}
                  </p>
                </DetailRow>
                {details.channelLabel != null ? (
                  <DetailRow label={details.channelLabel}>
                    <p className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS}>
                      {details.channelValue}
                    </p>
                  </DetailRow>
                ) : null}
                <DetailRow label="Connected guest form">
                  <p className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS}>
                    {details.connectedGuestForm}
                  </p>
                </DetailRow>
                <DetailRow label="Connected offer">
                  <p className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS}>
                    {details.connectedOfferText}
                  </p>
                </DetailRow>
              </div>
            </DialogHeader>

            {details.warningText != null ? (
              <div className={CAPTURE_PAUSE_ACTIVATE_DIALOG_WARNING_CLASS}>
                {details.warningText}
              </div>
            ) : null}

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

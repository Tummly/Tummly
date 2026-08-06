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
import { useHeldForExit } from "@/hooks/useHeldForExit"
import type { PauseActivateConfirmSnapshot } from "@/lib/operatorCapture/createOperatorCapturePageModule"
import {
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_BODY_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_CONTENT_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_FOOTER_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_LABEL_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_TITLE_CLASS,
  CAPTURE_PAUSE_ACTIVATE_DIALOG_WARNING_CLASS,
} from "@/lib/operatorCapture/capturePresentation"

type CapturePauseActivateConfirmDialogProps = {
  snapshot: PauseActivateConfirmSnapshot
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

/** Pause / Activate confirm dialogue — placement + digital link copy variants. */
export function CapturePauseActivateConfirmDialog({
  snapshot,
  busy = false,
  onOpenChange,
  onConfirm,
}: CapturePauseActivateConfirmDialogProps) {
  // Keep last details while Radix exit-animates after module clears payload.
  const details = useHeldForExit(snapshot.isOpen, snapshot.details)

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
              <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
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
                  variant="op-collapse"
                  disabled={busy}
                  aria-label="Close"
                  className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
                  onClick={() => onOpenChange(false)}
                >
                  <XIcon aria-hidden />
                </Button>
              </div>

              <div className="h-px w-full bg-op-border-default" />

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
                {details.action === "pause" ? (
                  <>
                    <DetailRow label="Current status">
                      <Badge variant="soft">{details.currentStatus}</Badge>
                    </DetailRow>
                    <DetailRow label="Last scan">
                      <p
                        className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS}
                      >
                        {details.lastScanText}
                      </p>
                    </DetailRow>
                  </>
                ) : (
                  <>
                    <DetailRow label="Connected guest form">
                      <p
                        className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS}
                      >
                        {details.connectedGuestForm}
                      </p>
                    </DetailRow>
                    <DetailRow label="Connected offer">
                      <p
                        className={CAPTURE_PAUSE_ACTIVATE_DIALOG_ROW_VALUE_CLASS}
                      >
                        {details.connectedOfferText}
                      </p>
                    </DetailRow>
                  </>
                )}
              </div>

              {details.warningText != null ? (
                <>
                  <div className="h-px w-full bg-op-border-default" />
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

import { XIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import { ACCOUNT_WORKSPACE_PAGE_COPY } from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"

type AccountWorkspaceConfirmDialogProps = {
  open: boolean
  title: string
  body: string
  primaryLabel: string
  busy?: boolean
  onOpenChange: (open: boolean) => void
  onPrimary: () => void
  onCancel: () => void
}

export function AccountWorkspaceConfirmDialog({
  open,
  title,
  body,
  primaryLabel,
  busy = false,
  onOpenChange,
  onPrimary,
  onCancel,
}: AccountWorkspaceConfirmDialogProps) {
  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (busy) {
          return
        }
        onOpenChange(next)
      }}
    >
      <DialogContent
        showCloseButton={false}
        className="gap-[60px] rounded-op-md bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[633px]"
      >
        <div className="flex flex-col gap-[30px]">
          <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3">
              <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                {title}
              </DialogTitle>
              <DialogDescription className="max-w-[503px] text-base font-medium leading-[22px] text-op-text-muted">
                {body}
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
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
            </DialogClose>
          </div>
        </div>

        <DialogFooter className="flex flex-row flex-wrap justify-start gap-3 sm:justify-start">
          <Button
            type="button"
            variant="op-primary"
            disabled={busy}
            onClick={onPrimary}
          >
            {primaryLabel}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled={busy}
            onClick={onCancel}
          >
            {ACCOUNT_WORKSPACE_PAGE_COPY.cancel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

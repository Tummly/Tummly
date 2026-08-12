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
import { useHeldForExit } from "@/hooks/useHeldForExit"
import {
  OFFERS_CONFIRM_DIALOG_CONTENT_CLASS,
  OFFERS_CONFIRM_DIALOG_DESCRIPTION_CLASS,
  OFFERS_CONFIRM_DIALOG_DIVIDER_CLASS,
  OFFERS_CONFIRM_DIALOG_FOOTER_CLASS,
  OFFERS_CONFIRM_DIALOG_HEADER_ROW_CLASS,
  OFFERS_CONFIRM_DIALOG_HEADER_STACK_CLASS,
  OFFERS_CONFIRM_DIALOG_TITLE_CLASS,
  OFFERS_PAGE_COPY,
} from "@/lib/operatorOffers/offersPresentation"
import { cn } from "@/lib/utils"

type OffersConfirmDialogProps = {
  open: boolean
  title: string
  description: string
  confirmLabel?: string
  cancelLabel?: string
  busy?: boolean
  /** When open over a high-z drawer (e.g. Create Offer), pass `z-[150]+`. */
  overlayClassName?: string
  className?: string
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
}

/** Pause / Duplicate / Archive / Discard confirm — Figma `5223:76624` / `4789:43034`. */
export function OffersConfirmDialog({
  open,
  title,
  description,
  confirmLabel = OFFERS_PAGE_COPY.confirmAction,
  cancelLabel = OFFERS_PAGE_COPY.cancelAction,
  busy = false,
  overlayClassName,
  className,
  onOpenChange,
  onConfirm,
}: OffersConfirmDialogProps) {
  // Keep last copy while Radix exit-animates after callers clear pending payload.
  const display =
    useHeldForExit(
      open,
      open
        ? {
            title,
            description,
            confirmLabel,
            cancelLabel,
          }
        : null
    ) ?? {
      title,
      description,
      confirmLabel,
      cancelLabel,
    }

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && busy) {
          return
        }
        onOpenChange(nextOpen)
      }}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName={overlayClassName}
        className={cn(OFFERS_CONFIRM_DIALOG_CONTENT_CLASS, className)}
      >
        <div className={OFFERS_CONFIRM_DIALOG_HEADER_STACK_CLASS}>
          <div className={OFFERS_CONFIRM_DIALOG_HEADER_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
              <DialogTitle className={OFFERS_CONFIRM_DIALOG_TITLE_CLASS}>
                {display.title}
              </DialogTitle>
              <DialogDescription
                className={OFFERS_CONFIRM_DIALOG_DESCRIPTION_CLASS}
              >
                {display.description}
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                size="icon"
                disabled={busy}
                className="shrink-0"
                aria-label="Close"
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>
          <div className={OFFERS_CONFIRM_DIALOG_DIVIDER_CLASS} aria-hidden />
        </div>

        <DialogFooter className={OFFERS_CONFIRM_DIALOG_FOOTER_CLASS}>
          <Button
            type="button"
            variant="op-primary"
            disabled={busy}
            onClick={onConfirm}
          >
            {display.confirmLabel}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled={busy}
            onClick={() => {
              onOpenChange(false)
            }}
          >
            {display.cancelLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

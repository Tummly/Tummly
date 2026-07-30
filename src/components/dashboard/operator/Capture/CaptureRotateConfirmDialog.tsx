import { useState } from "react"
import { XIcon } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
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
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
  OPERATOR_CAPTURE_ROTATE_CONFIRM_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { PlacementRotateConfirmSnapshot } from "@/lib/operatorCapture/createOperatorCapturePageModule"

type CaptureRotateConfirmDialogProps = {
  confirm: PlacementRotateConfirmSnapshot
  onOpenChange: (open: boolean) => void
  onAcknowledgedChange: (acknowledged: boolean) => void
  onConfirm: () => Promise<"rotated" | "failed" | "noop">
}

/** Rotate QR code confirm — Figma `4252:60151`. */
export function CaptureRotateConfirmDialog({
  confirm,
  onOpenChange,
  onAcknowledgedChange,
  onConfirm,
}: CaptureRotateConfirmDialogProps) {
  const [busy, setBusy] = useState(false)
  const copy = OPERATOR_CAPTURE_ROTATE_CONFIRM_COPY
  // Keep last open payload while Radix exit-animates after module clears fields.
  const displayConfirm =
    useHeldForExit(confirm.isOpen, confirm.isOpen ? confirm : null) ?? confirm

  return (
    <Dialog
      open={confirm.isOpen}
      onOpenChange={(open) => {
        if (busy) {
          return
        }
        onOpenChange(open)
      }}
    >
      <DialogContent
        showCloseButton={false}
        className="gap-[60px] bg-[var(--op-color-gray-995)] p-8 text-op-text-primary sm:max-w-[633px]"
      >
        <div className="flex flex-col gap-[30px]">
          <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3">
              <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                {copy.title}
              </DialogTitle>
              <DialogDescription className="max-w-[503px] text-base font-medium leading-[22px] text-op-text-secondary dark:text-[var(--op-color-gray-550)]">
                {copy.description}
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                disabled={busy}
                aria-label="Close"
                className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>

          <div
            className="h-px w-full bg-op-border-default"
            aria-hidden
          />

          <dl className="flex flex-col gap-5">
            <div className="flex items-center justify-between gap-4">
              <dt className="text-base font-medium text-op-text-primary">
                {copy.placementLabel}
              </dt>
              <dd className="text-sm font-medium text-op-text-secondary dark:text-[var(--op-color-gray-550)]">
                {displayConfirm.placementLabel}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="text-base font-medium text-op-text-primary">
                {copy.locationLabel}
              </dt>
              <dd className="text-sm font-medium text-op-text-secondary dark:text-[var(--op-color-gray-550)]">
                {displayConfirm.locationName}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="text-base font-medium text-op-text-primary">
                {copy.currentStatusLabel}
              </dt>
              <dd>
                {displayConfirm.status != null ? (
                  <Badge variant="soft">{displayConfirm.status}</Badge>
                ) : null}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="text-base font-medium text-op-text-primary">
                {copy.lastScanLabel}
              </dt>
              <dd className="text-sm font-medium text-op-text-secondary dark:text-[var(--op-color-gray-550)]">
                {displayConfirm.lastScanText}
              </dd>
            </div>
          </dl>

          <div
            className="h-px w-full bg-op-border-default"
            aria-hidden
          />

          <CheckboxLabel
            checked={displayConfirm.printMaterialsAcknowledged}
            onCheckedChange={onAcknowledgedChange}
            disabled={busy}
            labelClassName="text-sm font-medium leading-normal text-op-text-secondary dark:text-[var(--op-color-gray-550)]"
          >
            {copy.acknowledgment}
          </CheckboxLabel>
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            variant="op-primary"
            disabled={!displayConfirm.canConfirm || busy}
            onClick={() => {
              void (async () => {
                setBusy(true)
                try {
                  await onConfirm()
                } finally {
                  setBusy(false)
                }
              })()
            }}
          >
            {copy.confirmCta}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled={busy}
            onClick={() => {
              onOpenChange(false)
            }}
          >
            {copy.cancelCta}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

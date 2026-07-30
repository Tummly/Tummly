import { useState } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { OPERATOR_CAPTURE_ROTATE_CONFIRM_COPY } from "@/lib/operatorCapture/capturePresentation"
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
        showCloseButton
        className="gap-[60px] bg-[var(--op-color-gray-995)] p-8 text-op-text-primary sm:max-w-[633px]"
      >
        <div className="flex flex-col gap-[30px]">
          <DialogHeader className="gap-3 pr-10">
            <DialogTitle className="text-2xl font-bold tracking-normal text-op-text-primary">
              {copy.title}
            </DialogTitle>
            <DialogDescription className="max-w-[503px] text-base font-medium leading-[22px] text-op-text-secondary dark:text-[var(--op-color-gray-550)]">
              {copy.description}
            </DialogDescription>
          </DialogHeader>

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
                {confirm.placementLabel}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="text-base font-medium text-op-text-primary">
                {copy.locationLabel}
              </dt>
              <dd className="text-sm font-medium text-op-text-secondary dark:text-[var(--op-color-gray-550)]">
                {confirm.locationName}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="text-base font-medium text-op-text-primary">
                {copy.currentStatusLabel}
              </dt>
              <dd>
                {confirm.status != null ? (
                  <Badge variant="soft">{confirm.status}</Badge>
                ) : null}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="text-base font-medium text-op-text-primary">
                {copy.lastScanLabel}
              </dt>
              <dd className="text-sm font-medium text-op-text-secondary dark:text-[var(--op-color-gray-550)]">
                {confirm.lastScanText}
              </dd>
            </div>
          </dl>

          <div
            className="h-px w-full bg-op-border-default"
            aria-hidden
          />

          <CheckboxLabel
            checked={confirm.printMaterialsAcknowledged}
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
            disabled={!confirm.canConfirm || busy}
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

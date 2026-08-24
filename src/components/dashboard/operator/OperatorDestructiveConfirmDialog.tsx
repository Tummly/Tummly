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

export type OperatorDestructiveConfirmDialogProps = {
  open: boolean
  busy?: boolean
  error?: string | null
  title: string
  description: string
  confirmLabel: string
  cancelLabel?: string
  busyLabel?: string
  onOpenChange: (open: boolean) => void
  onConfirm: () => void | Promise<void>
}

/**
 * Shared Operator delete / destructive confirm — Figma 4029:46828.
 * Uses light/dark Operator tokens (same chrome as note delete).
 */
export function OperatorDestructiveConfirmDialog({
  open,
  busy = false,
  error = null,
  title,
  description,
  confirmLabel,
  cancelLabel = "Cancel",
  busyLabel = "Deleting…",
  onOpenChange,
  onConfirm,
}: OperatorDestructiveConfirmDialogProps) {
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
        className="flex flex-col gap-[60px] border-0 bg-op-surface-secondary p-8 text-op-text-primary shadow-lg sm:max-w-[560px] dark:bg-[var(--op-color-gray-1000)]"
      >
        <div className="flex w-full items-start gap-[22px]">
          <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
            <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
              {title}
            </DialogTitle>
            <DialogDescription className="text-sm font-medium leading-normal text-[var(--op-color-gray-550)] dark:text-[var(--op-color-gray-550)]">
              {description}
            </DialogDescription>
          </DialogHeader>
          <DialogClose asChild>
            <Button
              type="button"
              variant="op-collapse"
              size="icon"
              aria-label="Close"
              className="shrink-0"
              disabled={busy}
            >
              <XIcon aria-hidden />
            </Button>
          </DialogClose>
        </div>

        {error != null ? (
          <p
            className="text-sm font-medium text-[var(--op-color-red-550)]"
            role="alert"
          >
            {error}
          </p>
        ) : null}

        <DialogFooter className="flex flex-row flex-wrap items-center justify-start gap-3 sm:justify-start">
          <Button
            type="button"
            variant="destructive-solid"
            size="op"
            disabled={busy}
            className="rounded-op-sm bg-[var(--op-color-red-500)] hover:bg-[var(--op-color-red-500)]/90"
            onClick={() => {
              void onConfirm()
            }}
          >
            {busy ? busyLabel : confirmLabel}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled={busy}
            onClick={() => {
              onOpenChange(false)
            }}
          >
            {cancelLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

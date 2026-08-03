import { XIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent } from "@/components/ui/dialog"
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
        className="flex flex-col gap-[60px] bg-[var(--main-bg\/colour,#1b1b1b)] p-[32px] text-foreground sm:max-w-[560px]"
      >
        <div className="flex w-full shrink-0 flex-col">
          <div className="flex w-full shrink-0 items-start justify-center gap-[22px]">
            <div className="flex min-w-px flex-[1_0_0] flex-col gap-[12px]">
              <div className="flex shrink-0 flex-col justify-center text-2xl font-bold text-[color:var(--main-bg\/title,white)]">
                <p className="leading-[normal]">
                  {OPERATOR_NOTE_ACTIONS.deleteDialogTitle}
                </p>
              </div>
              <div className="flex shrink-0 flex-col justify-center text-sm font-medium text-[color:var(--main-bg\/subtitle,#7c7c7c)]">
                <p className="leading-[normal]">
                  {OPERATOR_NOTE_ACTIONS.deleteDialogDescription}
                </p>
              </div>
            </div>
            <Button
              type="button"
              variant="op-collapse"
              aria-label="Close"
              className="shrink-0 bg-[var(--button\/collaps\/bg-colour,#2c2c2c)] p-[12px]"
              disabled={busy}
              onClick={() => {
                onOpenChange(false)
              }}
            >
              <XIcon className="size-[18px]" aria-hidden />
            </Button>
          </div>
        </div>

        {error != null ? (
          <p
            className="text-sm font-medium text-[var(--op-color-red-550)]"
            role="alert"
          >
            {error}
          </p>
        ) : null}

        <div className="flex w-full shrink-0 flex-col">
          <div className="flex shrink-0 items-center gap-[12px]">
            <Button
              type="button"
              variant="destructive"
              disabled={busy}
              className="h-[42px] shrink-0 rounded-[var(--button\/radius,2px)] px-[var(--button\/right-left_padding,16px)] py-[var(--button\/top-bottom_padding,10px)]"
              onClick={() => {
                void onConfirm()
              }}
            >
              <span className="text-[length:var(--button\/text-size,14px)] leading-[20px]">
                {busy ? "Deleting…" : OPERATOR_NOTE_ACTIONS.deleteDialogConfirm}
              </span>
            </Button>
            <Button
              type="button"
              variant="op-tertiary"
              disabled={busy}
              className="h-[42px] shrink-0 rounded-[var(--button\/radius,2px)] border border-[var(--button\/tertiary\/colour-default,#4e4e4e)] px-[var(--button\/right-left_padding,17px)] py-[var(--button\/top-bottom_padding,11px)]"
              onClick={() => {
                onOpenChange(false)
              }}
            >
              <span className="text-[length:var(--button\/text-size,14px)] leading-[20px]">
                {OPERATOR_NOTE_ACTIONS.deleteDialogCancel}
              </span>
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}

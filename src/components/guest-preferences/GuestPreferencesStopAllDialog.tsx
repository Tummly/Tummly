import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import { cn } from "@/lib/utils"
import { GUEST_PREFERENCES_PILL_BUTTON_CLASS } from "@/lib/guestPreferences/guestPreferencesPresentation"

type GuestPreferencesStopAllDialogProps = {
  open: boolean
  busy: boolean
  onOpenChange: (open: boolean) => void
  onKeep: () => void
  onConfirm: () => void
}

/** Confirmation before turning off all Tummly marketing — Figma 7194:1138. */
export function GuestPreferencesStopAllDialog({
  open,
  busy,
  onOpenChange,
  onKeep,
  onConfirm,
}: GuestPreferencesStopAllDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-black/50"
        className="w-full max-w-[calc(100%-2rem)] gap-[38px] rounded-[20px] bg-guest-feedback-card p-5 pt-10 text-guest-feedback-text sm:max-w-[353px]"
      >
        <DialogHeader className="items-center gap-3 text-center">
          <DialogTitle className="pr-0 text-center text-[20px] leading-6 font-bold tracking-normal text-guest-feedback-text">
            {GUEST_PREFERENCES_COPY.stopAllDialogTitle}
          </DialogTitle>
          <DialogDescription className="max-w-none text-center font-sans text-sm leading-5 text-guest-feedback-muted-soft tracking-normal">
            {GUEST_PREFERENCES_COPY.stopAllDialogBody}
          </DialogDescription>
        </DialogHeader>
        <div className="flex w-full flex-col gap-3">
          <Button
            type="button"
            onClick={onKeep}
            className="h-[42px] min-h-[42px] w-full rounded-[54px] bg-guest-feedback-submit px-4 py-0 font-sans text-sm font-medium text-guest-feedback-submit-fg shadow-none hover:bg-white"
          >
            {GUEST_PREFERENCES_COPY.stopAllKeepLabel}
          </Button>
          <Button
            type="button"
            disabled={busy}
            onClick={onConfirm}
            className={cn(
              GUEST_PREFERENCES_PILL_BUTTON_CLASS,
              "h-[42px] min-h-[42px] py-0"
            )}
          >
            {GUEST_PREFERENCES_COPY.stopAllConfirmLabel}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}

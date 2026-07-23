import { useState } from "react"
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
import { Textarea } from "@/components/ui/textarea"
import {
  GUEST_PROFILE_NOTE_COMPOSE,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  OPERATOR_HOME_CHROME_BUTTON_CLASS,
  OPERATOR_HOME_CHROME_ICON_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { GUESTS_PAGE_PRIMARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"

type GuestProfileAddNoteDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (body: string) => Promise<boolean>
  busy?: boolean
}

/** Figma Add note dialog — node 3388:14290. */
export function GuestProfileAddNoteDialog({
  open,
  onOpenChange,
  onSave,
  busy = false,
}: GuestProfileAddNoteDialogProps) {
  const [body, setBody] = useState("")
  const trimmed = body.trim()
  const canSave =
    trimmed.length > 0 &&
    trimmed.length <= GUEST_PROFILE_NOTE_COMPOSE.maxLength &&
    !busy

  const resetAndClose = (nextOpen: boolean) => {
    if (!nextOpen) {
      setBody("")
    }
    onOpenChange(nextOpen)
  }

  return (
    <Dialog open={open} onOpenChange={resetAndClose}>
      <DialogContent
        showCloseButton={false}
        className="gap-10 bg-[var(--operator-shell-main)] p-8 text-foreground sm:max-w-[560px]"
      >
        <div className="flex flex-col gap-[30px]">
          <DialogHeader className="flex-row items-start justify-between gap-[22px]">
            <div className="flex min-w-0 flex-1 flex-col gap-3">
              <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-foreground">
                {GUEST_PROFILE_NOTE_COMPOSE.dialogTitle}
              </DialogTitle>
              <DialogDescription className="text-sm font-medium leading-normal text-muted-foreground dark:text-[#7c7c7c]">
                {GUEST_PROFILE_NOTE_COMPOSE.dialogDescription}
              </DialogDescription>
            </div>
            {!busy ? (
              <DialogClose asChild>
                <Button
                  type="button"
                  variant="ghost"
                  className={`${OPERATOR_HOME_CHROME_BUTTON_CLASS} hover:opacity-90`}
                  aria-label="Close"
                >
                  <XIcon className={OPERATOR_HOME_CHROME_ICON_CLASS} aria-hidden />
                </Button>
              </DialogClose>
            ) : null}
          </DialogHeader>

          <div className="flex min-h-[169px] flex-col gap-2">
            <label
              htmlFor="guest-profile-add-note"
              className="text-sm font-semibold leading-5 text-foreground"
            >
              {GUEST_PROFILE_NOTE_COMPOSE.fieldLabel}
            </label>
            <Textarea
              id="guest-profile-add-note"
              value={body}
              onChange={(event) => {
                setBody(event.target.value)
              }}
              placeholder={GUEST_PROFILE_NOTE_COMPOSE.placeholder}
              maxLength={GUEST_PROFILE_NOTE_COMPOSE.maxLength}
              disabled={busy}
              aria-invalid={
                body.length > 0 && trimmed.length === 0 ? true : undefined
              }
              className="min-h-[120px] flex-1 rounded border border-[rgba(74,74,76,0.4)] px-[15px] py-[15px] text-sm placeholder:text-[#7d7d7d] focus-visible:border-ring md:text-sm"
            />
          </div>
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            disabled={!canSave}
            onClick={() => {
              void (async () => {
                const ok = await onSave(trimmed)
                if (ok) {
                  resetAndClose(false)
                }
              })()
            }}
          >
            {GUEST_PROFILE_NOTE_COMPOSE.saveLabel}
          </Button>
          <Button
            type="button"
            variant="operator-tertiary"
            className="rounded-[2px]"
            disabled={busy}
            onClick={() => {
              resetAndClose(false)
            }}
          >
            {GUEST_PROFILE_NOTE_COMPOSE.cancelLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

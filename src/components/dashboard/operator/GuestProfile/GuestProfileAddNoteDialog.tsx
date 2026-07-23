import { useState } from "react"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import {
  GUEST_PROFILE_NOTE_COMPOSE,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestProfileAddNoteDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (body: string) => Promise<boolean>
  busy?: boolean
}

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
        showCloseButton={!busy}
        className="gap-6 p-8 sm:max-w-[560px]"
      >
        <DialogHeader>
          <DialogTitle>{GUEST_PROFILE_NOTE_COMPOSE.title}</DialogTitle>
        </DialogHeader>

        <div className="flex flex-col gap-2">
          <Label htmlFor="guest-profile-add-note">
            {GUEST_PROFILE_NOTE_COMPOSE.title}
          </Label>
          <Textarea
            id="guest-profile-add-note"
            value={body}
            onChange={(event) => {
              setBody(event.target.value)
            }}
            placeholder={GUEST_PROFILE_NOTE_COMPOSE.placeholder}
            maxLength={GUEST_PROFILE_NOTE_COMPOSE.maxLength}
            rows={5}
            disabled={busy}
            aria-invalid={
              body.length > 0 && trimmed.length === 0 ? true : undefined
            }
          />
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            variant="outline"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            disabled={busy}
            onClick={() => {
              resetAndClose(false)
            }}
          >
            {GUEST_PROFILE_NOTE_COMPOSE.cancelLabel}
          </Button>
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
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

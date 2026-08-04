import { useEffect, useState } from "react"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Textarea } from "@/components/ui/textarea"
import {
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_TEXTAREA_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import { cn } from "@/lib/utils"
import {
  FEEDBACK_INTERNAL_NOTE_EDIT,
  GUEST_PROFILE_NOTE_COMPOSE,
  GUEST_PROFILE_NOTE_EDIT,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import { GUESTS_PAGE_PRIMARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"

type NoteEditCopy =
  | typeof GUEST_PROFILE_NOTE_EDIT
  | typeof FEEDBACK_INTERNAL_NOTE_EDIT

type GuestProfileAddNoteDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (body: string) => Promise<boolean>
  busy?: boolean
  mode?: "create" | "edit"
  initialBody?: string
  /** Override create/edit copy (e.g. Feedback internal note vs Location Guest note). */
  editCopy?: NoteEditCopy
}

/** Figma Add note dialog — node 3388:14290. */
export function GuestProfileAddNoteDialog({
  open,
  onOpenChange,
  onSave,
  busy = false,
  mode = "create",
  initialBody = "",
  editCopy = GUEST_PROFILE_NOTE_EDIT,
}: GuestProfileAddNoteDialogProps) {
  const [body, setBody] = useState(initialBody)
  const copy = mode === "edit" ? editCopy : GUEST_PROFILE_NOTE_COMPOSE
  const trimmed = body.trim()
  const canSave =
    trimmed.length > 0 &&
    trimmed.length <= GUEST_PROFILE_NOTE_COMPOSE.maxLength &&
    !busy

  useEffect(() => {
    if (open) {
      setBody(initialBody)
    }
  }, [open, initialBody])

  const resetAndClose = (nextOpen: boolean) => {
    if (!nextOpen) {
      setBody("")
    }
    onOpenChange(nextOpen)
  }

  return (
    <Dialog open={open} onOpenChange={resetAndClose}>
      <DialogContent
        showCloseButton
        className="gap-10 bg-op-surface-secondary p-8 text-foreground sm:max-w-[560px]"
      >
        <div className="flex flex-col gap-[30px]">
          <DialogHeader className="gap-3 pr-10">
            <DialogTitle className="text-2xl font-bold tracking-normal text-foreground">
              {copy.dialogTitle}
            </DialogTitle>
            <DialogDescription className="text-sm font-medium leading-normal text-muted-foreground dark:text-[#7c7c7c]">
              {copy.dialogDescription}
            </DialogDescription>
          </DialogHeader>

          <div className="flex min-h-[169px] flex-col gap-2">
            <label
              htmlFor="guest-profile-add-note"
              className={FEEDBACK_FIELD_LABEL_CLASS}
            >
              {copy.fieldLabel}
            </label>
            <Textarea
              id="guest-profile-add-note"
              value={body}
              onChange={(event) => {
                setBody(event.target.value)
              }}
              placeholder={
                mode === "create" ? GUEST_PROFILE_NOTE_COMPOSE.placeholder : undefined
              }
              maxLength={GUEST_PROFILE_NOTE_COMPOSE.maxLength}
              disabled={busy}
              aria-invalid={
                body.length > 0 && trimmed.length === 0 ? true : undefined
              }
              className={cn(
                FEEDBACK_TEXTAREA_CLASS,
                "min-h-[120px] flex-1 shadow-none"
              )}
            />
          </div>
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            variant="op-primary"
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
            {copy.saveLabel}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className="rounded-[2px]"
            disabled={busy}
            onClick={() => {
              resetAndClose(false)
            }}
          >
            {copy.cancelLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

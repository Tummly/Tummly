import { Button } from "@/components/ui/button"
import { GUEST_PROFILE_ADD_NOTE_LABEL } from "@/lib/operatorGuestProfile/guestProfilePresentation"
import { GUESTS_PAGE_PRIMARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"

type GuestProfileAddNoteButtonProps = {
  onClick?: () => void
}

export function GuestProfileAddNoteButton({
  onClick,
}: GuestProfileAddNoteButtonProps) {
  return (
    <Button variant="op-primary"
      type="button"
      className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
      aria-label={GUEST_PROFILE_ADD_NOTE_LABEL}
      onClick={() => {
        onClick?.()
      }}
    >
      {GUEST_PROFILE_ADD_NOTE_LABEL}
    </Button>
  )
}

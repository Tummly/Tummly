import { GuestPreferencesContactInput } from "@/components/guest-preferences/GuestPreferencesContactInput"
import { GuestPreferencesContactVerifyDialogs } from "@/components/guest-preferences/GuestPreferencesContactVerifyDialogs"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import { useGuestPreferencesContactFlow } from "@/hooks/useGuestPreferencesContactFlow"
import {
  GUEST_PREFERENCES_DIALOG_CLASS,
  GUEST_PREFERENCES_DIALOG_TITLE_CLASS,
} from "@/lib/guestPreferences/guestPreferencesPresentation"

type GuestPreferencesAddContactDialogProps = {
  /** Contacts on the overview when the dialog opened. */
  baselineContactCount: number
  onClose: () => void
  /** The contact is confirmed. The page closes the dialog and refreshes the overview. */
  onVerified: () => void
  onSessionExpired: () => void
}

/**
 * Add another contact method, on the open session. The input step is this
 * dialog. Email and SMS steps are separate dialogs that take over from it.
 */
export function GuestPreferencesAddContactDialog({
  baselineContactCount,
  onClose,
  onVerified,
  onSessionExpired,
}: GuestPreferencesAddContactDialogProps) {
  const flow = useGuestPreferencesContactFlow({
    variant: "add-contact",
    baselineContactCount,
    onVerified,
    onSessionExpired,
  })

  return (
    <>
      <Dialog
        open={flow.state.step === "input"}
        onOpenChange={(open) => {
          if (!open) {
            onClose()
          }
        }}
      >
        <DialogContent
          aria-describedby={undefined}
          overlayClassName="bg-black/50"
          className={GUEST_PREFERENCES_DIALOG_CLASS}
        >
          <DialogHeader className="items-center gap-3 text-center">
            <DialogTitle className={GUEST_PREFERENCES_DIALOG_TITLE_CLASS}>
              {GUEST_PREFERENCES_COPY.addContactLabel}
            </DialogTitle>
          </DialogHeader>
          <GuestPreferencesContactInput
            value={flow.state.inputValue}
            error={flow.state.error}
            busy={flow.state.busy}
            onChange={flow.setInputValue}
            onSubmit={flow.submit}
          />
        </DialogContent>
      </Dialog>
      <GuestPreferencesContactVerifyDialogs
        flow={flow}
        mobileTitle={GUEST_PREFERENCES_COPY.verifyMobileAddTitle}
      />
    </>
  )
}

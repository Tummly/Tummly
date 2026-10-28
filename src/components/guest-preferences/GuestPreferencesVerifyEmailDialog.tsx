import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import {
  GUEST_PREFERENCES_DIALOG_CLASS,
  GUEST_PREFERENCES_DIALOG_DESCRIPTION_CLASS,
  GUEST_PREFERENCES_DIALOG_TITLE_CLASS,
  GUEST_PREFERENCES_LINK_CLASS,
  GUEST_PREFERENCES_PILL_BUTTON_CLASS,
} from "@/lib/guestPreferences/guestPreferencesPresentation"
import { cn } from "@/lib/utils"

type GuestPreferencesVerifyEmailActionsProps = {
  busy: boolean
  error: string | null
  resendSeconds: number
  onResend: () => void
  onChangeEmail: () => void
}

/** Resend link and Change Email. Resend is held for the cooldown. */
export function GuestPreferencesVerifyEmailActions({
  busy,
  error,
  resendSeconds,
  onResend,
  onChangeEmail,
}: GuestPreferencesVerifyEmailActionsProps) {
  return (
    <div className="flex w-full flex-col items-center gap-[14px]">
      {error ? (
        <p role="alert" className="m-0 text-center font-sans text-sm leading-5 text-destructive">
          {error}
        </p>
      ) : null}
      <Button
        type="button"
        disabled={busy || resendSeconds > 0}
        onClick={onResend}
        className={GUEST_PREFERENCES_PILL_BUTTON_CLASS}
      >
        {GUEST_PREFERENCES_COPY.resendLinkLabel}
      </Button>
      <Button
        type="button"
        variant="link"
        size="link-sm"
        disabled={busy}
        onClick={onChangeEmail}
        className={cn(GUEST_PREFERENCES_LINK_CLASS, "text-xs")}
      >
        {GUEST_PREFERENCES_COPY.changeEmailLabel}
      </Button>
    </div>
  )
}

type GuestPreferencesVerifyEmailDialogProps = GuestPreferencesVerifyEmailActionsProps & {
  open: boolean
  /** Masked address from maskEmailAddress. Never the raw address. */
  maskedEmail: string
  onOpenChange: (open: boolean) => void
}

/** Confirmation-link dialog. Figma 7198:1284. */
export function GuestPreferencesVerifyEmailDialog({
  open,
  maskedEmail,
  onOpenChange,
  ...actions
}: GuestPreferencesVerifyEmailDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-black/50"
        className={GUEST_PREFERENCES_DIALOG_CLASS}
      >
        <DialogHeader className="items-center gap-3 text-center">
          <DialogTitle className={GUEST_PREFERENCES_DIALOG_TITLE_CLASS}>
            {GUEST_PREFERENCES_COPY.verifyEmailTitle}
          </DialogTitle>
          <DialogDescription className={GUEST_PREFERENCES_DIALOG_DESCRIPTION_CLASS}>
            {GUEST_PREFERENCES_COPY.verifyEmailBody(maskedEmail)}
          </DialogDescription>
        </DialogHeader>
        <GuestPreferencesVerifyEmailActions {...actions} />
      </DialogContent>
    </Dialog>
  )
}

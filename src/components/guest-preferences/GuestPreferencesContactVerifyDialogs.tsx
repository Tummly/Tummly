import { GuestPreferencesVerifyEmailDialog } from "@/components/guest-preferences/GuestPreferencesVerifyEmailDialog"
import { GuestPreferencesVerifyMobileDialog } from "@/components/guest-preferences/GuestPreferencesVerifyMobileDialog"
import type { GuestPreferencesContactFlow } from "@/hooks/useGuestPreferencesContactFlow"
import { maskEmailAddress } from "@/lib/guestPreferences/contactInput"

type GuestPreferencesContactVerifyDialogsProps = {
  flow: GuestPreferencesContactFlow
  /** Entry and add-contact use different mobile headings. */
  mobileTitle: string
}

/**
 * The email-link and SMS-code dialogs for one contact flow. Each one is open
 * only on its own step. Closing either returns to the contact input.
 */
export function GuestPreferencesContactVerifyDialogs({
  flow,
  mobileTitle,
}: GuestPreferencesContactVerifyDialogsProps) {
  const { state, resendSeconds } = flow
  const busy = state.busy
  const onDismiss = (open: boolean) => {
    if (!open) {
      flow.changeContact()
    }
  }

  return (
    <>
      <GuestPreferencesVerifyEmailDialog
        open={state.step === "email-sent"}
        maskedEmail={maskEmailAddress(state.contact)}
        onOpenChange={onDismiss}
        busy={busy}
        error={state.error}
        resendSeconds={resendSeconds}
        onResend={flow.resend}
        onChangeEmail={flow.changeContact}
      />
      <GuestPreferencesVerifyMobileDialog
        open={state.step === "mobile-code"}
        title={mobileTitle}
        onOpenChange={onDismiss}
        busy={busy}
        error={state.error}
        resendSeconds={resendSeconds}
        onVerify={flow.verifyCode}
        onResend={flow.resend}
        onChangeNumber={flow.changeContact}
      />
    </>
  )
}

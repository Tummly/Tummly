import { GuestPreferencesContactInput } from "@/components/guest-preferences/GuestPreferencesContactInput"
import { GuestPreferencesContactVerifyDialogs } from "@/components/guest-preferences/GuestPreferencesContactVerifyDialogs"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import { useGuestPreferencesContactFlow } from "@/hooks/useGuestPreferencesContactFlow"

type GuestPreferencesAddContactFormProps = {
  /** Contacts on the overview when the form opened. A larger count means the new contact is confirmed. */
  baselineContactCount: number
  onVerified: () => void
  onSessionExpired: () => void
}

/**
 * Add another contact method. Replaces the link in the Verified contact block
 * (Figma 7196:1157). The email and SMS verify dialogs open over it.
 */
export function GuestPreferencesAddContactForm({
  baselineContactCount,
  onVerified,
  onSessionExpired,
}: GuestPreferencesAddContactFormProps) {
  const flow = useGuestPreferencesContactFlow({
    variant: "add-contact",
    baselineContactCount,
    onVerified,
    onSessionExpired,
  })

  return (
    <>
      <GuestPreferencesContactInput
        value={flow.state.inputValue}
        error={flow.state.step === "input" ? flow.state.error : null}
        busy={flow.state.busy}
        onChange={flow.setInputValue}
        onSubmit={flow.submit}
      />
      <GuestPreferencesContactVerifyDialogs
        flow={flow}
        mobileTitle={GUEST_PREFERENCES_COPY.verifyMobileAddTitle}
      />
    </>
  )
}

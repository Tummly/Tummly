import { GuestPreferencesContactInput } from "@/components/guest-preferences/GuestPreferencesContactInput"
import { GuestPreferencesContactVerifyDialogs } from "@/components/guest-preferences/GuestPreferencesContactVerifyDialogs"
import { GuestPreferencesHeader } from "@/components/guest-preferences/GuestPreferencesHeader"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import { useGuestPreferencesContactFlow } from "@/hooks/useGuestPreferencesContactFlow"
import { GUEST_PREFERENCES_CARD_CLASS } from "@/lib/guestPreferences/guestPreferencesPresentation"

type GuestPreferencesEntryProps = {
  /** Set when the link that brought the guest here was invalid or expired. */
  linkExpired: boolean
  /** Called once the session is open. The page loads the overview. */
  onVerified: () => void
}

/** No-session entry: contact input, then the email or SMS verify dialog. */
export function GuestPreferencesEntry({
  linkExpired,
  onVerified,
}: GuestPreferencesEntryProps) {
  const flow = useGuestPreferencesContactFlow({
    variant: "entry",
    onVerified,
  })

  return (
    <>
      <GuestPreferencesHeader
        lead={
          linkExpired
            ? GUEST_PREFERENCES_COPY.linkExpired
            : GUEST_PREFERENCES_COPY.entryLine
        }
      />
      <section className={GUEST_PREFERENCES_CARD_CLASS}>
        <GuestPreferencesContactInput
          value={flow.state.inputValue}
          error={flow.state.step === "input" ? flow.state.error : null}
          busy={flow.state.busy}
          onChange={flow.setInputValue}
          onSubmit={flow.submit}
        />
      </section>
      <GuestPreferencesContactVerifyDialogs
        flow={flow}
        mobileTitle={GUEST_PREFERENCES_COPY.verifyMobileEntryTitle}
      />
    </>
  )
}

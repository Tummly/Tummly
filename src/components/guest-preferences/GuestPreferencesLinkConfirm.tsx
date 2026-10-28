import { useReducer } from "react"

import { verifyContactEmail } from "@/api/publicGuestPreferencesApi"
import { GuestPreferencesHeader } from "@/components/guest-preferences/GuestPreferencesHeader"
import { Button } from "@/components/ui/button"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import {
  GUEST_PREFERENCES_CARD_CLASS,
  GUEST_PREFERENCES_PILL_BUTTON_CLASS,
} from "@/lib/guestPreferences/guestPreferencesPresentation"
import { confirmLink } from "@/lib/guestPreferences/guestPreferencesRequestErrors"
import {
  LINK_CONFIRM_INITIAL,
  reduceLinkConfirm,
} from "@/lib/guestPreferences/linkConfirmState"

type GuestPreferencesLinkConfirmProps = {
  /** The add-contact link parameter, `<challengeId>.<code>`. */
  v: string
  /** The address is added. The page shows the confirmed notice. */
  onConfirmed: () => void
  /** The link is dead (400). The page shows the expired entry view. */
  onExpired: () => void
}

/**
 * Add-contact link page. The link is used only when the guest presses Confirm,
 * never on load, so a mail scanner or a preview cannot add the address.
 */
export function GuestPreferencesLinkConfirm({
  v,
  onConfirmed,
  onExpired,
}: GuestPreferencesLinkConfirmProps) {
  const [state, dispatch] = useReducer(reduceLinkConfirm, LINK_CONFIRM_INITIAL)

  async function handleConfirm() {
    dispatch({ type: "confirm" })
    const outcome = await confirmLink(() => verifyContactEmail(v))
    dispatch({ type: "settled", outcome })
    if (outcome === "ok") {
      onConfirmed()
    } else if (outcome === "invalid") {
      onExpired()
    }
  }

  const busy = state.status === "confirming"
  const failed = state.status === "error"

  return (
    <>
      <GuestPreferencesHeader
        title={GUEST_PREFERENCES_COPY.confirmContactTitle}
        lead={GUEST_PREFERENCES_COPY.confirmContactBody}
      />
      <section className={GUEST_PREFERENCES_CARD_CLASS}>
        {failed ? (
          <p role="alert" className="m-0 font-sans text-sm leading-body text-guest-feedback-text">
            {GUEST_PREFERENCES_COPY.loadFailed}
          </p>
        ) : null}
        <Button
          type="button"
          disabled={busy}
          onClick={() => void handleConfirm()}
          className={GUEST_PREFERENCES_PILL_BUTTON_CLASS}
        >
          {failed
            ? GUEST_PREFERENCES_COPY.tryAgainLabel
            : GUEST_PREFERENCES_COPY.confirmContactLabel}
        </Button>
      </section>
    </>
  )
}

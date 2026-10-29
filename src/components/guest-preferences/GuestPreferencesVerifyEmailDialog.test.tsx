import { renderToStaticMarkup } from "react-dom/server"
import { describe, expect, it } from "vitest"

import { GuestPreferencesVerifyEmailActions } from "@/components/guest-preferences/GuestPreferencesVerifyEmailDialog"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import { maskEmailAddress } from "@/lib/guestPreferences/contactInput"

/** The attribute React writes on a held button (not the `disabled:` class variant). */
const DISABLED_ATTR = 'disabled=""'

/** The opening tag of the button whose label matches, e.g. `disabled=""` when held. */
function buttonOpeningTag(html: string, label: string): string {
  const end = html.indexOf(`>${label}</button>`)
  if (end < 0) {
    throw new Error(`No button labelled ${label}`)
  }
  return html.slice(html.lastIndexOf("<button", end), end + 1)
}

const noop = () => undefined

describe("GuestPreferencesVerifyEmailDialog", () => {
  it("masks the typed address in the body copy", () => {
    const body = GUEST_PREFERENCES_COPY.verifyEmailBody(
      maskEmailAddress("sophie@example.com")
    )

    expect(body).toBe(
      "We sent a confirmation link to so•••••@example.com. Open the link to confirm the Email address."
    )
    expect(body).not.toContain("sophie@example.com")
  })

  it("Resend link is held for the cooldown and Change Email stays available", () => {
    const html = renderToStaticMarkup(
      <GuestPreferencesVerifyEmailActions
        busy={false}
        error={null}
        resendSeconds={60}
        onResend={noop}
        onChangeEmail={noop}
      />
    )

    expect(buttonOpeningTag(html, GUEST_PREFERENCES_COPY.resendLinkLabel)).toContain(DISABLED_ATTR)
    expect(buttonOpeningTag(html, GUEST_PREFERENCES_COPY.changeEmailLabel)).not.toContain(DISABLED_ATTR)
  })

  it("Resend link is enabled once the cooldown has ended", () => {
    const html = renderToStaticMarkup(
      <GuestPreferencesVerifyEmailActions
        busy={false}
        error={null}
        resendSeconds={0}
        onResend={noop}
        onChangeEmail={noop}
      />
    )

    expect(buttonOpeningTag(html, GUEST_PREFERENCES_COPY.resendLinkLabel)).not.toContain(DISABLED_ATTR)
  })
})

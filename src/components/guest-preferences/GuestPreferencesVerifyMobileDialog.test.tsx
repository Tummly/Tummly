import { renderToStaticMarkup } from "react-dom/server"
import { describe, expect, it } from "vitest"

import { GuestPreferencesVerifyMobileForm } from "@/components/guest-preferences/GuestPreferencesVerifyMobileDialog"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"

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

function render(overrides: Partial<Parameters<typeof GuestPreferencesVerifyMobileForm>[0]> = {}) {
  return renderToStaticMarkup(
    <GuestPreferencesVerifyMobileForm
      busy={false}
      error={null}
      resendSeconds={0}
      onVerify={noop}
      onResend={noop}
      onChangeNumber={noop}
      {...overrides}
    />
  )
}

describe("GuestPreferencesVerifyMobileForm", () => {
  it("renders six code slots, Verify, Resend code and Change number", () => {
    const html = render()

    expect(html.match(/data-slot="input-otp-slot"/g)).toHaveLength(6)
    expect(html).toContain(GUEST_PREFERENCES_COPY.verifyLabel)
    expect(html).toContain(GUEST_PREFERENCES_COPY.resendCodeLabel)
    expect(html).toContain(GUEST_PREFERENCES_COPY.changeNumberLabel)
  })

  it("Verify starts disabled because no code is entered", () => {
    const html = render()

    expect(buttonOpeningTag(html, GUEST_PREFERENCES_COPY.verifyLabel)).toContain(DISABLED_ATTR)
  })

  it("Resend code is disabled for the 60 second cooldown", () => {
    const html = render({ resendSeconds: 60 })

    expect(
      buttonOpeningTag(html, GUEST_PREFERENCES_COPY.resendCodeLabel)
    ).toContain(DISABLED_ATTR)
  })

  it("Resend code is enabled once the cooldown has ended", () => {
    const html = render({ resendSeconds: 0 })

    expect(
      buttonOpeningTag(html, GUEST_PREFERENCES_COPY.resendCodeLabel)
    ).not.toContain(DISABLED_ATTR)
  })

  it("shows the wrong code message as an alert", () => {
    const html = render({ error: GUEST_PREFERENCES_COPY.codeWrong })

    expect(html).toContain('role="alert"')
    expect(html).toContain(GUEST_PREFERENCES_COPY.codeWrong)
  })
})

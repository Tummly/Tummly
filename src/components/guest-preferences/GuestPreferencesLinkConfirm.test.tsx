import { renderToStaticMarkup } from "react-dom/server"
import { beforeEach, describe, expect, it, vi } from "vitest"

import { verifyContactEmail } from "@/api/publicGuestPreferencesApi"
import { GuestPreferencesLinkConfirm } from "@/components/guest-preferences/GuestPreferencesLinkConfirm"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"

vi.mock("@/api/publicGuestPreferencesApi", () => ({
  verifyContactEmail: vi.fn(),
}))

/** The attribute React writes on a held button. */
const DISABLED_ATTR = 'disabled=""'

/** The opening tag of the button whose label matches. */
function buttonOpeningTag(html: string, label: string): string {
  const end = html.indexOf(`>${label}</button>`)
  if (end < 0) {
    throw new Error(`No button labelled ${label}`)
  }
  return html.slice(html.lastIndexOf("<button", end), end + 1)
}

describe("GuestPreferencesLinkConfirm", () => {
  beforeEach(() => {
    vi.mocked(verifyContactEmail).mockClear()
  })

  it("shows the title, the body and an enabled Confirm email button", () => {
    const html = renderToStaticMarkup(
      <GuestPreferencesLinkConfirm
        v="7.123456"
        onConfirmed={() => undefined}
        onExpired={() => undefined}
      />
    )

    expect(html).toContain(GUEST_PREFERENCES_COPY.confirmContactTitle)
    expect(html).toContain(GUEST_PREFERENCES_COPY.confirmContactBody)
    expect(html).toContain(GUEST_PREFERENCES_COPY.confirmContactLabel)
    expect(
      buttonOpeningTag(html, GUEST_PREFERENCES_COPY.confirmContactLabel)
    ).not.toContain(DISABLED_ATTR)
  })

  it("does not confirm on load: the link is only used when the guest presses the button", () => {
    renderToStaticMarkup(
      <GuestPreferencesLinkConfirm
        v="7.123456"
        onConfirmed={() => undefined}
        onExpired={() => undefined}
      />
    )

    expect(verifyContactEmail).not.toHaveBeenCalled()
  })
})

import { renderToStaticMarkup } from "react-dom/server"
import { describe, expect, it } from "vitest"

import { GuestPreferencesEntry } from "@/components/guest-preferences/GuestPreferencesEntry"
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

describe("GuestPreferencesEntry", () => {
  it("shows the entry line, the contact input and a Continue button held until text is typed", () => {
    const html = renderToStaticMarkup(
      <GuestPreferencesEntry linkExpired={false} onVerified={() => undefined} />
    )

    expect(html).toContain(GUEST_PREFERENCES_COPY.title)
    expect(html).toContain(GUEST_PREFERENCES_COPY.entryLine)
    expect(html).toContain(GUEST_PREFERENCES_COPY.contactInputLabel)
    expect(html).not.toContain(GUEST_PREFERENCES_COPY.linkExpired)
    expect(buttonOpeningTag(html, GUEST_PREFERENCES_COPY.continueLabel)).toContain(DISABLED_ATTR)
  })

  it("invalid link shows the expired message and the entry form", () => {
    const html = renderToStaticMarkup(
      <GuestPreferencesEntry linkExpired onVerified={() => undefined} />
    )

    expect(html).toContain(GUEST_PREFERENCES_COPY.linkExpired)
    // The expired copy contains the entry line, so match the whole lead paragraph.
    expect(html).not.toContain(`>${GUEST_PREFERENCES_COPY.entryLine}</p>`)
    expect(html).toContain(GUEST_PREFERENCES_COPY.contactInputLabel)
  })
})

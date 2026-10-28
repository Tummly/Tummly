import { renderToStaticMarkup } from "react-dom/server"
import { describe, expect, it } from "vitest"

import { GuestPreferencesEntryPlaceholder } from "@/components/guest-preferences/GuestPreferencesEntryPlaceholder"
import { GuestPreferencesNameRow } from "@/components/guest-preferences/GuestPreferencesNameRow"
import { GuestPreferencesOverview } from "@/components/guest-preferences/GuestPreferencesOverview"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import type { GuestPreferencesOverview as Overview } from "@/api/publicGuestPreferencesApi"

const overview: Overview = {
  firstName: "Sophie",
  contacts: [{ id: 1, type: "email", masked: "so••••@example.com" }],
  emailOn: true,
  smsOn: false,
  restaurantCount: 2,
}

/** The opening tag of the element whose id matches, e.g. a Radix checkbox button. */
function openingTagWithId(html: string, id: string): string {
  const match = html.match(new RegExp(`<[a-z]+[^>]*id="${id}"[^>]*>`))
  if (match === null) {
    throw new Error(`No element with id ${id}`)
  }
  return match[0]
}

describe("GuestPreferencesOverview", () => {
  it("renders name, masked contact and channel state from overview", () => {
    const html = renderToStaticMarkup(
      <GuestPreferencesOverview
        overview={overview}
        onSessionExpired={() => undefined}
        onAddContact={() => undefined}
        onManageRestaurants={() => undefined}
      />
    )

    expect(html).toContain(GUEST_PREFERENCES_COPY.title)
    expect(html).toContain("Sophie")
    expect(html).toContain("so••••@example.com")
    expect(html).toContain(GUEST_PREFERENCES_COPY.addContactLabel)
    expect(html).toContain(GUEST_PREFERENCES_COPY.manageRestaurantsLabel)
    expect(html).toContain(GUEST_PREFERENCES_COPY.savePreferencesLabel)
    expect(html).toContain(GUEST_PREFERENCES_COPY.stopAllLabel)

    expect(
      openingTagWithId(html, "guest-preferences-email")
    ).toContain('aria-checked="true"')
    expect(
      openingTagWithId(html, "guest-preferences-sms")
    ).toContain('aria-checked="false"')
  })

  it("Edit name shows input and Save calls updateFirstName", () => {
    const html = renderToStaticMarkup(
      <GuestPreferencesNameRow
        firstName="Sophie"
        draft="Sophie"
        busy={false}
        onEdit={() => undefined}
        onDraftChange={() => undefined}
        onSave={() => undefined}
      />
    )

    expect(html).toContain('value="Sophie"')
    expect(html).toContain(GUEST_PREFERENCES_COPY.firstNameLabel)
    expect(html).toContain(GUEST_PREFERENCES_COPY.saveNameLabel)
    expect(html).not.toContain(GUEST_PREFERENCES_COPY.editLabel)
  })

  it("session expired shows entry form", () => {
    const html = renderToStaticMarkup(<GuestPreferencesEntryPlaceholder />)

    expect(html).toContain(GUEST_PREFERENCES_COPY.title)
    expect(html).toContain(GUEST_PREFERENCES_COPY.entryLine)
  })
})

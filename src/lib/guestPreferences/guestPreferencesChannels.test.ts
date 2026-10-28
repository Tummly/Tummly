import { describe, expect, it } from "vitest"

import { diffChannelChanges } from "@/lib/guestPreferences/guestPreferencesChannels"

describe("diffChannelChanges", () => {
  it("returns only the keys whose value changed", () => {
    expect(
      diffChannelChanges(
        { email: true, sms: false },
        { email: false, sms: false }
      )
    ).toEqual({ email: false })
  })

  it("returns both keys when both changed", () => {
    expect(
      diffChannelChanges({ email: true, sms: false }, { email: false, sms: true })
    ).toEqual({ email: false, sms: true })
  })

  it("returns an empty object when nothing changed", () => {
    expect(
      diffChannelChanges({ email: true, sms: true }, { email: true, sms: true })
    ).toEqual({})
  })
})

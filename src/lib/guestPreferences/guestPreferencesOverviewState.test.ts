import { AxiosError, AxiosHeaders } from "axios"
import { describe, expect, it, vi } from "vitest"

import {
  commitChannelChanges,
  commitFirstName,
  createGuestPreferencesOverviewState,
  guestPreferencesErrorMessage,
  guestPreferencesOverviewReducer,
} from "@/lib/guestPreferences/guestPreferencesOverviewState"

function initialState() {
  return createGuestPreferencesOverviewState({
    firstName: "Sophie",
    emailOn: true,
    smsOn: false,
  })
}

describe("guestPreferencesOverviewState", () => {
  it("Save sends only changed keys", async () => {
    const updateChannels = vi.fn(async () => {})
    const saved = { email: true, sms: false }
    const current = { email: false, sms: false }

    const changed = await commitChannelChanges(updateChannels, saved, current)

    expect(changed).toBe(true)
    expect(updateChannels).toHaveBeenCalledTimes(1)
    expect(updateChannels).toHaveBeenCalledWith({ email: false })
  })

  it("Save with no change does not call updateChannels", async () => {
    const updateChannels = vi.fn(async () => {})
    const saved = { email: true, sms: false }

    const changed = await commitChannelChanges(updateChannels, saved, {
      ...saved,
    })

    expect(changed).toBe(false)
    expect(updateChannels).not.toHaveBeenCalled()
  })

  it("Edit name shows input and Save calls updateFirstName", async () => {
    const updateFirstName = vi.fn<(name: string) => Promise<void>>(
      async () => {}
    )

    const opened = guestPreferencesOverviewReducer(initialState(), {
      type: "name-edit-started",
    })
    expect(opened.nameDraft).toBe("Sophie")

    const edited = guestPreferencesOverviewReducer(opened, {
      type: "name-draft-changed",
      value: "Sophia",
    })
    const firstName = await commitFirstName(
      updateFirstName,
      edited.firstName,
      edited.nameDraft ?? ""
    )
    const saved = guestPreferencesOverviewReducer(edited, {
      type: "name-saved",
      firstName,
    })

    expect(updateFirstName).toHaveBeenCalledWith("Sophia")
    expect(saved.firstName).toBe("Sophia")
    expect(saved.nameDraft).toBeNull()
  })

  it("Edit name without a changed value does not call updateFirstName", async () => {
    const updateFirstName = vi.fn<(name: string) => Promise<void>>(
      async () => {}
    )

    const firstName = await commitFirstName(updateFirstName, "Sophie", "  Sophie ")

    expect(firstName).toBe("Sophie")
    expect(updateFirstName).not.toHaveBeenCalled()
  })

  it("Stop all opens dialog; Keep closes without call", () => {
    const opened = guestPreferencesOverviewReducer(initialState(), {
      type: "stop-all-opened",
    })
    expect(opened.stopAllOpen).toBe(true)

    const kept = guestPreferencesOverviewReducer(opened, {
      type: "stop-all-closed",
    })
    expect(kept.stopAllOpen).toBe(false)
    expect(kept.channels).toEqual({ email: true, sms: false })
  })

  it("Stop all confirm turns both channels off and closes the dialog", () => {
    const opened = guestPreferencesOverviewReducer(initialState(), {
      type: "stop-all-opened",
    })

    const confirmed = guestPreferencesOverviewReducer(opened, {
      type: "stop-all-confirmed",
    })

    expect(confirmed.stopAllOpen).toBe(false)
    expect(confirmed.channels).toEqual({ email: false, sms: false })
    expect(confirmed.savedChannels).toEqual({ email: false, sms: false })
  })

  it("channel toggle changes only the current selection, not the saved baseline", () => {
    const toggled = guestPreferencesOverviewReducer(initialState(), {
      type: "channel-toggled",
      channel: "email",
      checked: false,
    })

    expect(toggled.channels).toEqual({ email: false, sms: false })
    expect(toggled.savedChannels).toEqual({ email: true, sms: false })
  })
})

describe("guestPreferencesErrorMessage", () => {
  const fallback = "Fallback copy."

  it("reads the API error message from the response body", () => {
    const error = new AxiosError(
      "Request failed",
      "ERR_BAD_REQUEST",
      undefined,
      undefined,
      {
        status: 400,
        statusText: "Bad Request",
        headers: {},
        config: { headers: new AxiosHeaders() },
        data: { error: "First name is too long." },
      }
    )

    expect(guestPreferencesErrorMessage(error, fallback)).toBe(
      "First name is too long."
    )
  })

  it("falls back when the error has no API message", () => {
    expect(guestPreferencesErrorMessage(new Error("network"), fallback)).toBe(
      fallback
    )
  })
})

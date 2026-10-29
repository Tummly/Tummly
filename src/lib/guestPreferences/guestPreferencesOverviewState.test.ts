import { describe, expect, it, vi } from "vitest"

import {
  commitChannelChanges,
  commitFirstName,
  commitStopAll,
  createGuestPreferencesOverviewState,
  guestPreferencesOverviewReducer,
  guestPreferencesOverviewSeedKey,
  isGuestPreferencesBusy,
  type GuestPreferencesOverviewAction,
  type GuestPreferencesPendingAction,
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

  it("Stop all confirm calls stopAllMarketing once and applies the confirmed state", async () => {
    const stopAllMarketing = vi.fn(async () => {})
    const dispatched: GuestPreferencesOverviewAction[] = []

    await commitStopAll(stopAllMarketing, (action) => dispatched.push(action))

    expect(stopAllMarketing).toHaveBeenCalledTimes(1)
    expect(dispatched).toEqual([{ type: "stop-all-confirmed" }])

    const confirmed = guestPreferencesOverviewReducer(
      guestPreferencesOverviewReducer(initialState(), { type: "stop-all-opened" }),
      dispatched[0]
    )
    expect(confirmed.stopAllOpen).toBe(false)
    expect(confirmed.channels).toEqual({ email: false, sms: false })
    expect(confirmed.savedChannels).toEqual({ email: false, sms: false })
  })

  it("Stop all that fails dispatches nothing and rethrows", async () => {
    const failure = new Error("network")
    const stopAllMarketing = vi.fn(async () => {
      throw failure
    })
    const dispatch = vi.fn()

    await expect(commitStopAll(stopAllMarketing, dispatch)).rejects.toBe(failure)
    expect(dispatch).not.toHaveBeenCalled()
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

  it("any pending request makes every request control busy", () => {
    const idle: GuestPreferencesPendingAction = null
    expect(isGuestPreferencesBusy(idle)).toBe(false)

    const pendingActions: GuestPreferencesPendingAction[] = [
      "name",
      "channels",
      "stop-all",
    ]
    for (const pending of pendingActions) {
      expect(isGuestPreferencesBusy(pending)).toBe(true)
    }
  })
})

describe("guestPreferencesOverviewSeedKey", () => {
  const base = {
    contacts: [{ id: 1, type: "email", masked: "so•••••@example.com" }],
    emailOn: true,
    smsOn: false,
    firstName: "Sophie",
    restaurantCount: 2,
  }

  it("is the same for the same contacts and channels", () => {
    expect(guestPreferencesOverviewSeedKey({ ...base })).toBe(
      guestPreferencesOverviewSeedKey({ ...base })
    )
  })

  it("changes when a contact is added after an add-contact success", () => {
    const added = {
      ...base,
      contacts: [...base.contacts, { id: 2, type: "phone", masked: "•••• ••• 789" }],
    }

    expect(guestPreferencesOverviewSeedKey(added)).not.toBe(
      guestPreferencesOverviewSeedKey(base)
    )
  })

  it("changes when a channel differs on the refreshed overview", () => {
    expect(guestPreferencesOverviewSeedKey({ ...base, smsOn: true })).not.toBe(
      guestPreferencesOverviewSeedKey(base)
    )
    expect(guestPreferencesOverviewSeedKey({ ...base, emailOn: false })).not.toBe(
      guestPreferencesOverviewSeedKey(base)
    )
  })
})

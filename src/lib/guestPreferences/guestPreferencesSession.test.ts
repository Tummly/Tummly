import { beforeEach, describe, expect, it } from "vitest"

import {
  clearGuestPreferencesSession,
  readGuestPreferencesSession,
  writeGuestPreferencesSession,
} from "./guestPreferencesSession"

const STORAGE_KEY = "tummly.guestPreferences.session"

describe("guestPreferencesSession", () => {
  beforeEach(() => {
    sessionStorage.clear()
  })

  it("reads null when no session is stored", () => {
    expect(readGuestPreferencesSession()).toBeNull()
  })

  it("writes the token under the session storage key", () => {
    writeGuestPreferencesSession("token-1")

    expect(sessionStorage.getItem(STORAGE_KEY)).toBe("token-1")
    expect(readGuestPreferencesSession()).toBe("token-1")
  })

  it("overwrites an earlier token", () => {
    writeGuestPreferencesSession("token-1")
    writeGuestPreferencesSession("token-2")

    expect(readGuestPreferencesSession()).toBe("token-2")
  })

  it("clears the stored token", () => {
    writeGuestPreferencesSession("token-1")

    clearGuestPreferencesSession()

    expect(readGuestPreferencesSession()).toBeNull()
    expect(sessionStorage.getItem(STORAGE_KEY)).toBeNull()
  })
})

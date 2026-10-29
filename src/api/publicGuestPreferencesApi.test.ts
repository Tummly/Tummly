import { afterEach, beforeEach, describe, expect, it } from "vitest"
import axios, {
  AxiosHeaders,
  type AxiosRequestConfig,
  type RawAxiosHeaders,
} from "axios"
import MockAdapter from "axios-mock-adapter"

import { API_BASE_URL } from "@/config/api"
import {
  readGuestPreferencesSession,
  writeGuestPreferencesSession,
} from "@/lib/guestPreferences/guestPreferencesSession"

import {
  GuestPreferencesSessionExpiredError,
  addContact,
  fetchOverview,
  fetchRestaurants,
  openSessionFromLink,
  setLocationMuted,
  startSession,
  updateChannels,
  verifyContactEmail,
  verifySessionCode,
  verifySessionEmail,
} from "./publicGuestPreferencesApi"

const BASE = `${API_BASE_URL}/public/preferences`
const SESSION_HEADER = "X-Guest-Preferences-Session"

function sessionHeaderOf(config: AxiosRequestConfig): string | undefined {
  const value = AxiosHeaders.from(config.headers as RawAxiosHeaders).get(
    SESSION_HEADER
  )
  return typeof value === "string" ? value : undefined
}

describe("publicGuestPreferencesApi", () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(axios)
    sessionStorage.clear()
  })

  afterEach(() => {
    mock.restore()
  })

  describe("session endpoints", () => {
    it("opens a session from a signed link and stores the body token", async () => {
      mock
        .onPost(`${BASE}/session/link`, { token: "signed-token" })
        .reply(200, { sessionToken: "session-1" })

      await expect(openSessionFromLink("signed-token")).resolves.toEqual({
        valid: true,
      })
      expect(readGuestPreferencesSession()).toBe("session-1")
    })

    it("reports an invalid link and keeps any stored session", async () => {
      writeGuestPreferencesSession("existing")
      mock.onPost(`${BASE}/session/link`).reply(200, { valid: false })

      await expect(openSessionFromLink("bad-token")).resolves.toEqual({
        valid: false,
      })
      expect(readGuestPreferencesSession()).toBe("existing")
    })

    it("posts the start contact and resolves on 202", async () => {
      mock
        .onPost(`${BASE}/session/start`, { contact: "guest@example.com" })
        .reply(202, { sent: true })

      await expect(startSession("guest@example.com")).resolves.toBeUndefined()
    })

    it("stores the body token after a verified code", async () => {
      mock
        .onPost(`${BASE}/session/verify-code`, {
          contact: "guest@example.com",
          code: "123456",
        })
        .reply(200, { sessionToken: "session-2" })

      await verifySessionCode("guest@example.com", "123456")

      expect(readGuestPreferencesSession()).toBe("session-2")
    })

    it("stores the body token after a verified entry email", async () => {
      mock
        .onPost(`${BASE}/session/verify-email`, { v: "1.abc" })
        .reply(200, { sessionToken: "session-3" })

      await verifySessionEmail("1.abc")

      expect(readGuestPreferencesSession()).toBe("session-3")
    })

    it("rejects a bad entry email with the error body and stores nothing", async () => {
      mock
        .onPost(`${BASE}/session/verify-email`, { v: "1.abc" })
        .reply(400, { error: "invalid" })

      await expect(verifySessionEmail("1.abc")).rejects.toMatchObject({
        response: { status: 400, data: { error: "invalid" } },
      })
      expect(readGuestPreferencesSession()).toBeNull()
    })
  })

  describe("session-gated endpoints", () => {
    it("sends the X-Guest-Preferences-Session header", async () => {
      writeGuestPreferencesSession("session-1")
      let sent: string | undefined
      mock.onGet(BASE).reply((config) => {
        sent = sessionHeaderOf(config)
        return [200, overviewBody()]
      })

      await fetchOverview()

      expect(sent).toBe("session-1")
    })

    it("stores the renewed session from the response header", async () => {
      writeGuestPreferencesSession("session-1")
      mock
        .onGet(BASE)
        .reply(200, overviewBody(), { [SESSION_HEADER]: "session-2" })

      await fetchOverview()

      expect(readGuestPreferencesSession()).toBe("session-2")
    })

    it("stores the renewed session even when the write is a 404", async () => {
      writeGuestPreferencesSession("session-1")
      mock
        .onPut(`${BASE}/locations/9/mute`)
        .reply(404, { error: "not-found" }, { [SESSION_HEADER]: "session-2" })

      await expect(setLocationMuted(9, true)).rejects.toMatchObject({
        response: { status: 404 },
      })
      expect(readGuestPreferencesSession()).toBe("session-2")
    })

    it("clears the session and throws on 401", async () => {
      writeGuestPreferencesSession("session-old")
      mock.onGet(BASE).reply(401, { error: "session" })

      await expect(fetchOverview()).rejects.toBeInstanceOf(
        GuestPreferencesSessionExpiredError
      )
      expect(readGuestPreferencesSession()).toBeNull()
    })

    it("sends only the changed channel keys", async () => {
      writeGuestPreferencesSession("session-1")
      mock.onPut(`${BASE}/channels`, { email: false }).reply(204)

      await expect(updateChannels({ email: false })).resolves.toBeUndefined()
    })

    it("sends the search query for restaurants", async () => {
      writeGuestPreferencesSession("session-1")
      mock
        .onGet(`${BASE}/restaurants`, { params: { q: "camden" } })
        .reply(200, [])

      await expect(fetchRestaurants("camden")).resolves.toEqual([])
    })

    it("posts the add-contact request with the session", async () => {
      writeGuestPreferencesSession("session-1")
      mock
        .onPost(`${BASE}/contacts`, { contact: "new@example.com" })
        .reply(202, { sent: true })

      await expect(addContact("new@example.com")).resolves.toBeUndefined()
    })
  })

  describe("contacts/verify-email", () => {
    it("needs no session and never writes one", async () => {
      writeGuestPreferencesSession("session-1")
      let sent: string | undefined = "not-called"
      mock.onPost(`${BASE}/contacts/verify-email`, { v: "789.012" }).reply((config) => {
        sent = sessionHeaderOf(config)
        return [200, { linked: true }]
      })

      await expect(verifyContactEmail("789.012")).resolves.toBeUndefined()
      expect(sent).toBeUndefined()
      expect(readGuestPreferencesSession()).toBe("session-1")
    })
  })
})

function overviewBody() {
  return {
    firstName: null,
    contacts: [],
    emailOn: true,
    smsOn: false,
    restaurantCount: 0,
  }
}

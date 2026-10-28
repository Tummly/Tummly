import { AxiosError, AxiosHeaders } from "axios"
import { describe, expect, it } from "vitest"

import { GuestPreferencesSessionExpiredError } from "@/api/publicGuestPreferencesApi"
import {
  classifyLinkFailure,
  confirmLink,
  guestPreferencesErrorMessage,
  resolveLinkSession,
  routeGuestPreferencesRequestError,
} from "@/lib/guestPreferences/guestPreferencesRequestErrors"

const FALLBACK = "Fallback copy."

function axiosErrorWithBody(data: unknown, status = 400): AxiosError {
  return new AxiosError("Request failed", "ERR_BAD_REQUEST", undefined, undefined, {
    status,
    statusText: "",
    headers: {},
    config: { headers: new AxiosHeaders() },
    data,
  })
}

describe("guestPreferencesErrorMessage", () => {
  it("reads the API error message from the response body", () => {
    expect(
      guestPreferencesErrorMessage(
        axiosErrorWithBody({ error: "First name is too long." }),
        FALLBACK
      )
    ).toBe("First name is too long.")
  })

  it("falls back when the error has no API message", () => {
    expect(guestPreferencesErrorMessage(new Error("network"), FALLBACK)).toBe(
      FALLBACK
    )
    expect(
      guestPreferencesErrorMessage(axiosErrorWithBody({ error: "  " }), FALLBACK)
    ).toBe(FALLBACK)
  })
})

describe("classifyLinkFailure", () => {
  it("a 400 means the link is invalid, used or expired", () => {
    expect(classifyLinkFailure(axiosErrorWithBody({ error: "expired" }, 400))).toBe(
      "invalid"
    )
  })

  it("a 5xx is unavailable, so the link may still be valid", () => {
    expect(classifyLinkFailure(axiosErrorWithBody({}, 500))).toBe("unavailable")
  })

  it("a network failure is unavailable", () => {
    expect(classifyLinkFailure(new Error("network"))).toBe("unavailable")
  })
})

describe("routeGuestPreferencesRequestError", () => {
  it("session expired routes to the entry form", () => {
    expect(
      routeGuestPreferencesRequestError(
        new GuestPreferencesSessionExpiredError(),
        FALLBACK
      )
    ).toEqual({ view: "entry" })
  })

  it("other errors route to the error view with the API message", () => {
    expect(
      routeGuestPreferencesRequestError(
        axiosErrorWithBody({ error: "Try again later." }, 500),
        FALLBACK
      )
    ).toEqual({ view: "error", message: "Try again later." })
  })

  it("other errors without an API message use the fallback copy", () => {
    expect(
      routeGuestPreferencesRequestError(new Error("network"), FALLBACK)
    ).toEqual({ view: "error", message: FALLBACK })
  })
})

describe("resolveLinkSession", () => {
  it("a session reply is valid", async () => {
    await expect(resolveLinkSession(async () => ({ valid: true }))).resolves.toBe("valid")
  })

  it("a reply without a session is a dead link, not a failure", async () => {
    await expect(resolveLinkSession(async () => ({ valid: false }))).resolves.toBe("invalid")
  })

  it("a 400 is a dead link", async () => {
    await expect(
      resolveLinkSession(async () => {
        throw axiosErrorWithBody({ error: "invalid" }, 400)
      })
    ).resolves.toBe("invalid")
  })

  it("a 429 is unavailable, so the link is kept for Try again", async () => {
    await expect(
      resolveLinkSession(async () => {
        throw axiosErrorWithBody({}, 429)
      })
    ).resolves.toBe("unavailable")
  })

  it("a 5xx is unavailable", async () => {
    await expect(
      resolveLinkSession(async () => {
        throw axiosErrorWithBody({}, 503)
      })
    ).resolves.toBe("unavailable")
  })

  it("a network failure is unavailable", async () => {
    await expect(
      resolveLinkSession(async () => {
        throw new Error("network")
      })
    ).resolves.toBe("unavailable")
  })
})

describe("confirmLink", () => {
  it("resolves ok when the action succeeds", async () => {
    await expect(confirmLink(async () => undefined)).resolves.toBe("ok")
  })

  it("classifies a failed action the same way as a failed link", async () => {
    await expect(
      confirmLink(async () => {
        throw axiosErrorWithBody({ error: "expired" }, 400)
      })
    ).resolves.toBe("invalid")
    await expect(
      confirmLink(async () => {
        throw axiosErrorWithBody({}, 500)
      })
    ).resolves.toBe("unavailable")
  })
})

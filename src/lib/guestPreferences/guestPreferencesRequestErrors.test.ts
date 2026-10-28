import { AxiosError, AxiosHeaders } from "axios"
import { describe, expect, it } from "vitest"

import { GuestPreferencesSessionExpiredError } from "@/api/publicGuestPreferencesApi"
import {
  classifyLinkFailure,
  guestPreferencesErrorMessage,
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

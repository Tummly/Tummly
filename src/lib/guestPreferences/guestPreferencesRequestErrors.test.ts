import { AxiosError, AxiosHeaders } from "axios"
import { describe, expect, it } from "vitest"

import { GuestPreferencesSessionExpiredError } from "@/api/publicGuestPreferencesApi"
import {
  classifyLinkFailure,
  confirmLink,
  guestPreferencesErrorMessage,
  isNotFoundError,
  resolveLinkSession,
  routeGuestPreferencesRequestError,
  routeOverviewWriteError,
} from "@/lib/guestPreferences/guestPreferencesRequestErrors"

const INVALID_ENTRY = "Check your entry and try again."

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
  it("maps the invalid code to guest copy", () => {
    expect(
      guestPreferencesErrorMessage(axiosErrorWithBody({ error: "invalid" }, 400), FALLBACK)
    ).toBe(INVALID_ENTRY)
  })

  it("never shows a raw API code or sentence: other codes use the fallback", () => {
    expect(
      guestPreferencesErrorMessage(axiosErrorWithBody({ error: "not-found" }, 404), FALLBACK)
    ).toBe(FALLBACK)
    expect(
      guestPreferencesErrorMessage(
        axiosErrorWithBody({ error: "First name is too long." }, 400),
        FALLBACK
      )
    ).toBe(FALLBACK)
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

  it("other errors route to the error view with guest copy for a known code", () => {
    expect(
      routeGuestPreferencesRequestError(
        axiosErrorWithBody({ error: "invalid" }, 400),
        FALLBACK
      )
    ).toEqual({ view: "error", message: INVALID_ENTRY })
  })

  it("other errors with an unknown code use the fallback", () => {
    expect(
      routeGuestPreferencesRequestError(
        axiosErrorWithBody({ error: "Try again later." }, 500),
        FALLBACK
      )
    ).toEqual({ view: "error", message: FALLBACK })
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

describe("isNotFoundError", () => {
  it("is true only for a 404 with the not-found code", () => {
    expect(isNotFoundError(axiosErrorWithBody({ error: "not-found" }, 404))).toBe(true)
    expect(isNotFoundError(axiosErrorWithBody({ error: "other" }, 404))).toBe(false)
    expect(isNotFoundError(axiosErrorWithBody({ error: "not-found" }, 400))).toBe(false)
    expect(isNotFoundError(new Error("not-found"))).toBe(false)
  })
})

describe("routeOverviewWriteError", () => {
  it("a not-found write means the guest was merged away: route to the entry view", () => {
    expect(
      routeOverviewWriteError(axiosErrorWithBody({ error: "not-found" }, 404), FALLBACK)
    ).toEqual({ view: "entry" })
  })

  it("a session expiry routes to the entry view", () => {
    expect(
      routeOverviewWriteError(new GuestPreferencesSessionExpiredError(), FALLBACK)
    ).toEqual({ view: "entry" })
  })

  it("an invalid write shows a message, not the entry view", () => {
    expect(
      routeOverviewWriteError(axiosErrorWithBody({ error: "invalid" }, 400), FALLBACK)
    ).toEqual({ view: "error", message: INVALID_ENTRY })
  })
})

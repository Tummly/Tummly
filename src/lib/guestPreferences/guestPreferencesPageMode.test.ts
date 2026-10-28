import { describe, expect, it } from "vitest"

import { resolveGuestPreferencesPageMode } from "./guestPreferencesPageMode"

function resolve(query: string, hasSession = false) {
  return resolveGuestPreferencesPageMode(new URLSearchParams(query), hasSession)
}

describe("resolveGuestPreferencesPageMode", () => {
  it("resolves the signed link from t", () => {
    expect(resolve("?t=signed-token")).toEqual({
      kind: "link",
      token: "signed-token",
    })
  })

  it("resolves the entry email link from v", () => {
    expect(resolve("?v=123.456")).toEqual({ kind: "verify-email", v: "123.456" })
  })

  it("resolves the add-contact email link from link", () => {
    expect(resolve("?link=789.012")).toEqual({
      kind: "link-contact",
      v: "789.012",
    })
  })

  it("resolves a stored session when there are no params", () => {
    expect(resolve("", true)).toEqual({ kind: "session" })
  })

  it("resolves entry when there are no params and no session", () => {
    expect(resolve("", false)).toEqual({ kind: "entry" })
  })

  it("lets t win over a stored session", () => {
    expect(resolve("?t=signed-token", true)).toEqual({
      kind: "link",
      token: "signed-token",
    })
  })

  it("lets v win over a stored session", () => {
    expect(resolve("?v=123.456", true)).toEqual({
      kind: "verify-email",
      v: "123.456",
    })
  })

  it("lets link win over a stored session", () => {
    expect(resolve("?link=789.012", true)).toEqual({
      kind: "link-contact",
      v: "789.012",
    })
  })

  it("treats an empty param as absent", () => {
    expect(resolve("?t=", true)).toEqual({ kind: "session" })
    expect(resolve("?t=&v=", false)).toEqual({ kind: "entry" })
  })
})

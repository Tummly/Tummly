import { describe, expect, it, vi } from "vitest"

import {
  captureOAuthTicketToken,
  clearOAuthTicketToken,
  readOAuthTicketTokenFromLocation,
  stripOAuthTicketTokenFromLocation,
} from "./oauthTicketToken"

describe("readOAuthTicketTokenFromLocation", () => {
  it("reads token from query string", () => {
    expect(readOAuthTicketTokenFromLocation("?token=abc-123", "")).toBe(
      "abc-123"
    )
  })

  it("prefers query over hash", () => {
    expect(
      readOAuthTicketTokenFromLocation("?token=query", "#token=hash")
    ).toBe("query")
  })

  it("falls back to hash for legacy links", () => {
    expect(readOAuthTicketTokenFromLocation("", "#token=legacy")).toBe(
      "legacy"
    )
  })

  it("returns null when missing", () => {
    expect(readOAuthTicketTokenFromLocation("", "")).toBeNull()
  })
})

describe("stripOAuthTicketTokenFromLocation", () => {
  it("removes token from search via replaceState", () => {
    const replaceState = vi.fn()
    stripOAuthTicketTokenFromLocation(
      {
        pathname: "/signup/oauth/terms",
        search: "?token=secret&x=1",
        hash: "",
      },
      { replaceState }
    )
    expect(replaceState).toHaveBeenCalledWith(
      null,
      "",
      "/signup/oauth/terms?x=1"
    )
  })
})

describe("captureOAuthTicketToken", () => {
  it("stashes from URL then re-reads stash when URL empty", () => {
    const store = new Map<string, string>()
    const storage = {
      getItem: (k: string) => store.get(k) ?? null,
      setItem: (k: string, v: string) => {
        store.set(k, v)
      },
      removeItem: (k: string) => {
        store.delete(k)
      },
    }

    expect(
      captureOAuthTicketToken("signup", "?token=from-url", "", storage)
    ).toBe("from-url")
    expect(captureOAuthTicketToken("signup", "", "", storage)).toBe("from-url")
    clearOAuthTicketToken("signup", storage)
    expect(captureOAuthTicketToken("signup", "", "", storage)).toBe("")
  })
})

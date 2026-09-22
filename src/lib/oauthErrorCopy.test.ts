import { afterEach, describe, expect, it } from "vitest"

import {
  OAUTH_ERROR_STORAGE_KEY,
  clearOAuthErrorMessage,
  getOAuthErrorMessage,
  peekOAuthErrorMessage,
  resolveOAuthErrorBanner,
  stashOAuthErrorMessage,
} from "./oauthErrorCopy"

describe("getOAuthErrorMessage", () => {
  it("maps account_exists", () => {
    expect(getOAuthErrorMessage("account_exists", "login")).toBe(
      "An account with this email already exists. Sign in with email and password."
    )
  })

  it("maps cancelled softly", () => {
    expect(getOAuthErrorMessage("cancelled", "login")).toMatch(/cancelled/i)
    expect(getOAuthErrorMessage("cancelled", "signup")).toMatch(/cancelled/i)
  })

  it("maps failed and staff_password_only", () => {
    expect(getOAuthErrorMessage("failed", "login")).toMatch(/try again/i)
    expect(getOAuthErrorMessage("failed", "signup")).toMatch(
      /email and password/i
    )
    expect(getOAuthErrorMessage("provider_failed", "signup")).toMatch(
      /Google or Microsoft/i
    )
    expect(getOAuthErrorMessage("staff_password_only", "login")).toMatch(
      /password Sign-in/i
    )
  })

  it("returns null for empty code", () => {
    expect(getOAuthErrorMessage(null, "login")).toBeNull()
    expect(getOAuthErrorMessage("", "signup")).toBeNull()
  })
})

describe("resolveOAuthErrorBanner", () => {
  it("prefers oauthMessage over code mapping", () => {
    expect(
      resolveOAuthErrorBanner("login", {
        oauthError: "failed",
        oauthMessage: "Account is locked.",
      })
    ).toBe("Account is locked.")
  })

  it("ignores stale stash when query params are present", () => {
    expect(
      resolveOAuthErrorBanner("login", {
        oauthError: "failed",
        stashed: "Not approved yet.",
      })
    ).toMatch(/try again/i)

    expect(
      resolveOAuthErrorBanner("login", {
        oauthError: "cancelled",
        stashed: "Stale stash.",
      })
    ).toMatch(/cancelled/i)
  })

  it("uses stash only when both query params are absent", () => {
    expect(
      resolveOAuthErrorBanner("login", {
        stashed: "Not approved yet.",
      })
    ).toBe("Not approved yet.")

    expect(resolveOAuthErrorBanner("login", {})).toBeNull()
  })
})

describe("oauth error sessionStorage stash", () => {
  afterEach(() => {
    sessionStorage.removeItem(OAUTH_ERROR_STORAGE_KEY)
  })

  it("peek survives without consuming", () => {
    stashOAuthErrorMessage("login", "Account is locked.")
    expect(peekOAuthErrorMessage("login")).toBe("Account is locked.")
    expect(peekOAuthErrorMessage("signup")).toBeNull()
    clearOAuthErrorMessage()
    expect(peekOAuthErrorMessage("login")).toBeNull()
  })
})

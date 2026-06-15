import { describe, expect, it } from "vitest"

import { isResetTokenError } from "./resetPasswordFlow"

describe("isResetTokenError", () => {
  it("detects invalid reset token messages", () => {
    expect(isResetTokenError("Invalid reset token.")).toBe(true)
  })

  it("detects expired reset token messages", () => {
    expect(isResetTokenError("Reset token expired.")).toBe(true)
  })

  it("detects missing token copy", () => {
    expect(isResetTokenError("Invalid or missing token")).toBe(true)
  })

  it("returns false for unrelated errors", () => {
    expect(isResetTokenError("Passwords do not match.")).toBe(false)
  })
})

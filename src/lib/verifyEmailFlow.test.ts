import { describe, expect, it } from "vitest"
import { buildVerifyEmailPath, readVerifyEmail } from "./verifyEmailFlow"

describe("verifyEmailFlow", () => {
  it("builds a path with encoded email", () => {
    expect(buildVerifyEmailPath("Mehmet@Example.com")).toBe(
      "/verify-email?email=Mehmet%40Example.com"
    )
  })

  it("prefers query email then state email", () => {
    const params = new URLSearchParams("email=a%40b.com")
    expect(readVerifyEmail(params, "c@d.com")).toBe("a@b.com")
    expect(readVerifyEmail(new URLSearchParams(), "c@d.com")).toBe("c@d.com")
    expect(readVerifyEmail(new URLSearchParams(), null)).toBeNull()
  })
})

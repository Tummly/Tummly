import { afterEach, describe, expect, it, vi } from "vitest"

describe("getRecaptchaSiteKey", () => {
  afterEach(() => {
    vi.unstubAllEnvs()
    vi.resetModules()
  })

  it("returns null when VITE_RECAPTCHA_SITE_KEY is unset", async () => {
    vi.stubEnv("VITE_RECAPTCHA_SITE_KEY", "")
    const { getRecaptchaSiteKey } = await import(
      "@/lib/guestFeedback/executeGuestFeedbackRecaptcha"
    )
    expect(getRecaptchaSiteKey()).toBeNull()
  })

  it("returns trimmed site key when set", async () => {
    vi.stubEnv("VITE_RECAPTCHA_SITE_KEY", "  site-key-abc  ")
    const { getRecaptchaSiteKey } = await import(
      "@/lib/guestFeedback/executeGuestFeedbackRecaptcha"
    )
    expect(getRecaptchaSiteKey()).toBe("site-key-abc")
  })
})

import { afterEach, describe, expect, it, vi } from "vitest"

describe("marketingMediaUrl", () => {
  afterEach(() => {
    vi.unstubAllEnvs()
    vi.resetModules()
  })

  it("uses the QA marketing container by default", async () => {
    const { marketingMediaUrl } = await import("./marketingMedia")
    expect(marketingMediaUrl("cta/signup-cta.mp4")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/cta/signup-cta.mp4",
    )
  })

  it("honours VITE_MARKETING_MEDIA_BASE_URL without a trailing slash", async () => {
    vi.stubEnv(
      "VITE_MARKETING_MEDIA_BASE_URL",
      "https://cdn.example.com/marketing/",
    )
    const { marketingMediaUrl } = await import("./marketingMedia")
    expect(marketingMediaUrl("/cta/signup-cta.mp4")).toBe(
      "https://cdn.example.com/marketing/cta/signup-cta.mp4",
    )
  })

  it("builds the Sign-up CTA video URL", async () => {
    const { signUpCtaVideoUrl, SIGN_UP_CTA_VIDEO_PATH } =
      await import("./marketingMedia")
    expect(SIGN_UP_CTA_VIDEO_PATH).toBe("cta/signup-cta.mp4")
    expect(signUpCtaVideoUrl()).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/cta/signup-cta.mp4",
    )
  })

  it("builds AI Assistant video URLs", async () => {
    const { aiAssistantVideoUrl, AI_ASSISTANT_VIDEO_PATHS } =
      await import("./marketingMedia")
    expect(AI_ASSISTANT_VIDEO_PATHS.guestLoop).toBe(
      "ai-assistant/guest-loop.mp4",
    )
    expect(aiAssistantVideoUrl("guestLoop")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/ai-assistant/guest-loop.mp4",
    )
    expect(aiAssistantVideoUrl("heroMotion")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/ai-assistant/hero-motion.mp4",
    )
    expect(aiAssistantVideoUrl("stepPermissions")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/ai-assistant/step-context-check.mp4",
    )
  })

  it("builds the Home hero campaign video URL", async () => {
    const { homeHeroVideoUrl, HOME_HERO_VIDEO_PATH } =
      await import("./marketingMedia")
    expect(HOME_HERO_VIDEO_PATH).toBe("home/hero-campaign.mp4")
    expect(homeHeroVideoUrl()).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/home/hero-campaign.mp4",
    )
  })

})

import { afterEach, describe, expect, it, vi } from "vitest"

describe("marketingMediaUrl", () => {
  afterEach(() => {
    vi.unstubAllEnvs()
    vi.resetModules()
  })

  it("uses the QA marketing container by default", async () => {
    const { marketingMediaUrl } = await import("./marketingMedia")
    expect(marketingMediaUrl("cta/signup-cta-feedback-form.mp4")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/cta/signup-cta-feedback-form.mp4",
    )
  })

  it("honours VITE_MARKETING_MEDIA_BASE_URL without a trailing slash", async () => {
    vi.stubEnv(
      "VITE_MARKETING_MEDIA_BASE_URL",
      "https://cdn.example.com/marketing/",
    )
    const { marketingMediaUrl } = await import("./marketingMedia")
    expect(marketingMediaUrl("/cta/signup-cta-feedback-form.mp4")).toBe(
      "https://cdn.example.com/marketing/cta/signup-cta-feedback-form.mp4",
    )
  })

  it("builds the Sign-up CTA video URL", async () => {
    const { signUpCtaVideoUrl, SIGN_UP_CTA_VIDEO_PATH } =
      await import("./marketingMedia")
    expect(SIGN_UP_CTA_VIDEO_PATH).toBe("cta/signup-cta-feedback-form.mp4")
    expect(signUpCtaVideoUrl()).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/cta/signup-cta-feedback-form.mp4",
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

  it("builds the For Groups hero video URL", async () => {
    const { forGroupsHeroVideoUrl, FOR_GROUPS_HERO_VIDEO_PATH } =
      await import("./marketingMedia")
    expect(FOR_GROUPS_HERO_VIDEO_PATH).toBe("for-groups/hero.mp4")
    expect(forGroupsHeroVideoUrl()).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/for-groups/hero.mp4",
    )
  })

  it("builds the Home hero campaign video URL", async () => {
    const { homeHeroVideoUrl, HOME_HERO_VIDEO_PATH } =
      await import("./marketingMedia")
    expect(HOME_HERO_VIDEO_PATH).toBe("home/hero-campaign-results-loop-1080.mp4")
    expect(homeHeroVideoUrl()).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/home/hero-campaign-results-loop-1080.mp4",
    )
  })

  it("builds the Home platform card video URLs (864px loops)", async () => {
    const { homePlatformVideoUrl } = await import("./marketingMedia")
    expect(homePlatformVideoUrl("reasons-to-return")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/for-groups/product-e1dfdb-reasons-to-return-864.mp4",
    )
  })

  it("builds About restaurant-reality video URLs", async () => {
    const { aboutRealityVideoUrl, ABOUT_REALITY_VIDEO_PATHS } =
      await import("./marketingMedia")
    expect(ABOUT_REALITY_VIDEO_PATHS.return).toBe("about/reality-return.mp4")
    expect(aboutRealityVideoUrl("return")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/about/reality-return.mp4",
    )
    expect(aboutRealityVideoUrl("relationships")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/about/reality-relationships.mp4",
    )
    expect(aboutRealityVideoUrl("together")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/about/reality-together-on-white.mp4",
    )
    expect(aboutRealityVideoUrl("visibility")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/about/reality-visibility-on-white.mp4",
    )
  })

  it("builds For Groups product card video URLs", async () => {
    const { forGroupsProductVideoUrl, FOR_GROUPS_PRODUCT_VIDEO_PATHS } =
      await import("./marketingMedia")
    expect(FOR_GROUPS_PRODUCT_VIDEO_PATHS["hear-from-guests"]).toBe(
      "for-groups/product-e1dfdb-hear-from-guests.mp4",
    )
    expect(forGroupsProductVideoUrl("whats-working")).toBe(
      "https://sttummlyqavfavue.blob.core.windows.net/marketing/for-groups/product-e1dfdb-whats-working.mp4",
    )
  })
})

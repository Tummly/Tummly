import { describe, expect, it } from "vitest"

import { CAMPAIGN_CHANNEL_SHORTFALL_BANNER_CLASS } from "./campaignChannelPresentation"

describe("campaignChannelPresentation", () => {
  it("uses theme-aware Main Bg tokens for the shortfall banner", () => {
    expect(CAMPAIGN_CHANNEL_SHORTFALL_BANNER_CLASS).toContain(
      "bg-op-background-primary"
    )
    expect(CAMPAIGN_CHANNEL_SHORTFALL_BANNER_CLASS).not.toContain(
      "op-color-gray-995"
    )
  })
})

import { describe, expect, it } from "vitest"
import { mapCampaignTemplateSuggestions } from "./mapCampaignTemplateSuggestions"

describe("stored Campaign audience", () => {
  it("preserves the restricted audience when the Draft opens for review", () => {
    const audienceKey = "audience:1234567890abcdef1234567890abcdef"
    const result = mapCampaignTemplateSuggestions({
      goalId: "custom-campaign",
      audienceKey,
      channel: "email",
      offerStance: "no-offer",
    })
    expect(result.audienceId).toBe(audienceKey)
  })
})

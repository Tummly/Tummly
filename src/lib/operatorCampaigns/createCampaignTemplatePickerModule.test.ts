import { describe, expect, it, vi } from "vitest"

import { CAMPAIGN_TEMPLATE_PICKER_COPY } from "@/lib/operatorCampaigns/campaignTemplatePickerPresentation"
import { createCampaignTemplatePickerModule } from "@/lib/operatorCampaigns/createCampaignTemplatePickerModule"
import type { CampaignTemplateListItem } from "@/types/operatorCampaigns"

function sampleTemplates(): CampaignTemplateListItem[] {
  return [
    {
      id: "thank-recent-guests",
      version: 1,
      title: "Thank recent guests",
      description: "Welcome recently captured guests.",
      goalLabel: "Thank recent guests",
      audienceLabel: "New guests",
      channelLabel: "Email",
      offerLabel: "Optional",
      suggestsGoal: true,
      suggestsAudience: true,
      suggestsChannel: true,
      suggestsOffer: true,
    },
    {
      id: "we-miss-you",
      version: 1,
      title: "We miss you",
      description: "Reconnect with inactive guests.",
      goalLabel: "Re-engage inactive guests",
      audienceLabel: "No recent Tummly activity",
      channelLabel: "Email",
      offerLabel: "Optional",
      suggestsGoal: true,
      suggestsAudience: true,
      suggestsChannel: true,
      suggestsOffer: true,
    },
  ]
}

describe("createCampaignTemplatePickerModule", () => {
  it("loads catalogue cards when opened", async () => {
    const loadTemplates = vi.fn(async () => sampleTemplates())
    const picker = createCampaignTemplatePickerModule({ loadTemplates })

    await picker.open()

    expect(loadTemplates).toHaveBeenCalledTimes(1)
    const snapshot = picker.getSnapshot()
    expect(snapshot.open).toBe(true)
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel?.cards).toHaveLength(2)
    expect(snapshot.viewModel?.cards[0]).toMatchObject({
      id: "thank-recent-guests",
      title: "Thank recent guests",
      previewDisabled: true,
      useTemplateEnabled: false,
    })
  })

  it("treats an empty catalogue as an error with no fallback cards", async () => {
    const loadTemplates = vi.fn(async () => [])
    const picker = createCampaignTemplatePickerModule({ loadTemplates })

    await picker.open()

    const snapshot = picker.getSnapshot()
    expect(snapshot.loadStatus).toBe("error")
    expect(snapshot.loadError).toBe(CAMPAIGN_TEMPLATE_PICKER_COPY.emptyError)
    expect(snapshot.viewModel).toBeNull()
  })

  it("retries a failed load after open", async () => {
    const loadTemplates = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(sampleTemplates())

    const picker = createCampaignTemplatePickerModule({ loadTemplates })
    await picker.open()

    expect(picker.getSnapshot().loadStatus).toBe("error")
    expect(picker.getSnapshot().loadError).toBe(
      CAMPAIGN_TEMPLATE_PICKER_COPY.loadError
    )

    await picker.retryLoad()

    expect(loadTemplates).toHaveBeenCalledTimes(2)
    expect(picker.getSnapshot().loadStatus).toBe("loaded")
    expect(picker.getSnapshot().viewModel?.cards).toHaveLength(2)
  })

  it("filters cards by search query", async () => {
    const picker = createCampaignTemplatePickerModule({
      loadTemplates: async () => sampleTemplates(),
    })
    await picker.open()

    picker.setSearchQuery("miss")
    expect(picker.getSnapshot().viewModel?.cards).toHaveLength(1)
    expect(picker.getSnapshot().viewModel?.cards[0]?.id).toBe("we-miss-you")
    expect(picker.getSnapshot().viewModel?.showSearchMiss).toBe(false)

    picker.setSearchQuery("zzzz")
    expect(picker.getSnapshot().viewModel?.cards).toHaveLength(0)
    expect(picker.getSnapshot().viewModel?.showSearchMiss).toBe(true)
  })
})

import { describe, expect, it, vi } from "vitest"

import { CAMPAIGN_DETAIL_PREVIEW_COPY } from "@/lib/operatorCampaigns/campaignDetailPreviewPresentation"
import { createCampaignDetailPreviewModule } from "@/lib/operatorCampaigns/createCampaignDetailPreviewModule"
import type { CampaignDetailPreviewSource } from "@/lib/operatorCampaigns/createCampaignDetailPreviewModule"

function sampleCampaign(
  overrides: Partial<CampaignDetailPreviewSource> = {}
): CampaignDetailPreviewSource {
  return {
    id: 42,
    status: "draft",
    name: "Tuesday lunch reminder",
    goalId: "boost-quieter-time",
    audienceKey: "all-eligible-guests",
    channel: "email",
    offerStance: "no-offer",
    offerId: null,
    messageSubject: "Quiet Tuesday lunch?",
    messageBody: "Hi Sarah,\n\nThanks for visiting.",
    ...overrides,
  }
}

describe("createCampaignDetailPreviewModule", () => {
  it("opens Preview for a Draft and projects campaign summary fields", async () => {
    const loadCampaign = vi.fn(async () => sampleCampaign())
    const module = createCampaignDetailPreviewModule({ loadCampaign })

    const openPromise = module.open(42)
    expect(module.getSnapshot().open).toBe(true)
    expect(module.getSnapshot().loadStatus).toBe("loading")

    await openPromise

    const snapshot = module.getSnapshot()
    expect(loadCampaign).toHaveBeenCalledWith(42)
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel).toMatchObject({
      campaignId: 42,
      title: "Tuesday lunch reminder",
      subtitle: CAMPAIGN_DETAIL_PREVIEW_COPY.subtitle,
      summary: {
        goal: "Boost a quieter time",
        audience: "All eligible guests",
        channel: "Email",
        offer: "No offer",
      },
      selectedChannelId: "email",
      activeMessage: {
        channel: "email",
        body: "Hi Sarah,\n\nThanks for visiting.",
        subject: "Quiet Tuesday lunch?",
      },
      sendLogicLabel: CAMPAIGN_DETAIL_PREVIEW_COPY.notScheduled,
      closeLabel: CAMPAIGN_DETAIL_PREVIEW_COPY.close,
    })
    expect(snapshot.viewModel).not.toHaveProperty("useThisTemplateLabel")
  })

  it("loads Preview for a non-Draft Campaign when the adapter returns one", async () => {
    const loadCampaign = vi.fn(async () =>
      sampleCampaign({
        id: 99,
        status: "scheduled",
        name: "Weekend brunch push",
        channel: "sms",
        messageSubject: null,
        messageBody: "Hi — brunch this weekend?",
      })
    )
    const module = createCampaignDetailPreviewModule({ loadCampaign })

    await module.open(99)

    const snapshot = module.getSnapshot()
    expect(loadCampaign).toHaveBeenCalledWith(99)
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel).toMatchObject({
      campaignId: 99,
      title: "Weekend brunch push",
      selectedChannelId: "sms",
      activeMessage: {
        channel: "sms",
        body: "Hi — brunch this weekend?",
        subject: null,
      },
    })
  })
})

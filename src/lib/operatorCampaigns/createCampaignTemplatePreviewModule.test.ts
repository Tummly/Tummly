import { describe, expect, it, vi } from "vitest"

import { CAMPAIGN_TEMPLATE_PREVIEW_COPY } from "@/lib/operatorCampaigns/campaignTemplatePreviewPresentation"
import { createCampaignTemplatePreviewModule } from "@/lib/operatorCampaigns/createCampaignTemplatePreviewModule"
import type { CampaignTemplateDetail } from "@/types/operatorCampaigns"

function sampleDetail(
  overrides: Partial<CampaignTemplateDetail> = {}
): CampaignTemplateDetail {
  return {
    id: "quiet-time-boost",
    version: 1,
    title: "Quiet-time boost",
    description: "Invite eligible guests back.",
    goalLabel: "Boost a quieter time",
    audienceLabel: "All eligible guests or saved group",
    channelLabel: "Email or SMS",
    offerLabel: "Recommended",
    suggestsGoal: true,
    suggestsAudience: true,
    suggestsChannel: true,
    suggestsOffer: true,
    suggestions: {
      goalId: "boost-quieter-time",
      audienceKey: "all-eligible-or-saved-group",
      channel: "email-or-sms",
      offerStance: "recommended",
    },
    preview: {
      summary: {
        goal: "Bring eligible guests back during a quiet day or time",
        bestFor: "Slower weekdays, lunch gaps, low-footfall periods",
        suggestedAudience: "All eligible guests or selected saved group",
        suggestedChannel: "Email or SMS",
        offer: "Recommended",
      },
      suggestedChannels: ["email", "sms"],
      messages: [
        {
          channel: "email",
          estimatedUsageLabel: "16 email messages",
          body: "Hi Sarah,\n\nThanks for visiting.",
          subject: null,
          offerBlock: {
            title: "15% off your next order",
            description: "Show this code to the team.",
            redemptionCode: "BURGERCO-4829",
            expiryLabel: "Expires: 31 July 2026",
          },
        },
        {
          channel: "sms",
          estimatedUsageLabel: "4 SMS messages",
          body: "Hi Sarah — quiet Tuesday lunch?",
          subject: null,
          offerBlock: {
            title: "15% off your next order",
            description: "Show this code to the team.",
            redemptionCode: "BURGERCO-4829",
            expiryLabel: "Expires: 31 July 2026",
          },
        },
      ],
      offerLogic: [
        { label: "Offer type:", value: "10% off" },
        { label: "Code type:", value: "Unique guest code" },
      ],
      eligibility: {
        emailCount: 16,
        smsCount: 4,
        totalUniqueGuests: 20,
      },
      suggestedTiming: "Send Monday 10am for Tuesday lunch.",
      footerDisclaimer: CAMPAIGN_TEMPLATE_PREVIEW_COPY.footerDisclaimer,
    },
    ...overrides,
  }
}

describe("createCampaignTemplatePreviewModule", () => {
  it("caches getSnapshot for useSyncExternalStore identity", () => {
    const preview = createCampaignTemplatePreviewModule({
      loadTemplateDetail: async () => sampleDetail(),
    })

    expect(preview.getSnapshot()).toBe(preview.getSnapshot())
  })

  it("opens Preview with a loading state then projects by-id payload", async () => {
    let resolveLoad!: (value: CampaignTemplateDetail) => void
    const loadTemplateDetail = vi.fn(
      () =>
        new Promise<CampaignTemplateDetail>((resolve) => {
          resolveLoad = resolve
        })
    )
    const preview = createCampaignTemplatePreviewModule({ loadTemplateDetail })

    const openPromise = preview.open("quiet-time-boost")
    expect(preview.getSnapshot()).toMatchObject({
      open: true,
      loadStatus: "loading",
      viewModel: null,
    })

    resolveLoad(sampleDetail())
    await openPromise

    const snapshot = preview.getSnapshot()
    expect(loadTemplateDetail).toHaveBeenCalledWith("quiet-time-boost")
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel?.title).toBe("Quiet-time boost")
    expect(snapshot.viewModel?.channelTabs.map((tab) => tab.id)).toEqual([
      "email",
      "sms",
    ])
    expect(snapshot.viewModel?.selectedChannelId).toBe("email")
    expect(snapshot.viewModel?.activeMessage?.estimatedUsageLabel).toBe(
      "16 email messages"
    )
    expect(snapshot.viewModel?.showOfferLogic).toBe(true)
    expect(snapshot.viewModel?.eligibility.totalUniqueGuests).toBe(20)
  })

  it("keeps Preview closed without Use — Close returns control to the picker stack", async () => {
    const preview = createCampaignTemplatePreviewModule({
      loadTemplateDetail: async () => sampleDetail(),
    })
    await preview.open("quiet-time-boost")
    expect(preview.getSnapshot().open).toBe(true)

    preview.close()

    expect(preview.getSnapshot()).toMatchObject({
      open: false,
      loadStatus: "idle",
      viewModel: null,
      useTemplateId: null,
    })
    expect(preview.useThisTemplate()).toBeNull()
  })

  it("Use this template returns the loaded id while Preview stays open until the page closes both", async () => {
    const preview = createCampaignTemplatePreviewModule({
      loadTemplateDetail: async () => sampleDetail(),
    })
    await preview.open("quiet-time-boost")

    const templateId = preview.useThisTemplate()

    expect(templateId).toBe("quiet-time-boost")
    // Module Use closes Preview; page Use path closes picker after load succeeds.
    expect(preview.getSnapshot().open).toBe(false)
  })

  it("refuses Use while Preview is still loading", async () => {
    let resolveLoad!: (value: CampaignTemplateDetail) => void
    const preview = createCampaignTemplatePreviewModule({
      loadTemplateDetail: () =>
        new Promise<CampaignTemplateDetail>((resolve) => {
          resolveLoad = resolve
        }),
    })

    const openPromise = preview.open("quiet-time-boost")
    expect(preview.useThisTemplate()).toBeNull()
    expect(preview.getSnapshot().open).toBe(true)

    resolveLoad(sampleDetail())
    await openPromise
    expect(preview.useThisTemplate()).toBe("quiet-time-boost")
  })

  it("switches channel tabs within suggested channels only", async () => {
    const preview = createCampaignTemplatePreviewModule({
      loadTemplateDetail: async () => sampleDetail(),
    })
    await preview.open("quiet-time-boost")

    preview.setSelectedChannel("sms")
    expect(preview.getSnapshot().viewModel?.selectedChannelId).toBe("sms")
    expect(preview.getSnapshot().viewModel?.activeMessage?.channel).toBe("sms")

    preview.setSelectedChannel("push" as "email")
    expect(preview.getSnapshot().viewModel?.selectedChannelId).toBe("sms")
  })

  it("hides offer logic when by-id seed omits it", async () => {
    const preview = createCampaignTemplatePreviewModule({
      loadTemplateDetail: async () =>
        sampleDetail({
          id: "thank-recent-guests",
          title: "Thank recent guests",
          preview: {
            ...sampleDetail().preview,
            suggestedChannels: ["email"],
            messages: [
              {
                channel: "email",
                estimatedUsageLabel: "12 email messages",
                body: "Thanks",
                subject: "Thanks",
                offerBlock: null,
              },
            ],
            offerLogic: null,
          },
        }),
    })
    await preview.open("thank-recent-guests")

    expect(preview.getSnapshot().viewModel?.showOfferLogic).toBe(false)
    expect(preview.getSnapshot().viewModel?.channelTabs).toHaveLength(1)
    expect(preview.getSnapshot().viewModel?.activeMessage?.offerBlock).toBeNull()
  })

  it("surfaces load errors inside the open Preview sheet", async () => {
    const preview = createCampaignTemplatePreviewModule({
      loadTemplateDetail: async () => {
        throw new Error("network")
      },
    })

    await preview.open("quiet-time-boost")

    expect(preview.getSnapshot()).toMatchObject({
      open: true,
      loadStatus: "error",
      loadError: CAMPAIGN_TEMPLATE_PREVIEW_COPY.loadError,
      viewModel: null,
    })
  })
})

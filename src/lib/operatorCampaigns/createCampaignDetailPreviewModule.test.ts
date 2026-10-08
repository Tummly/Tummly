import { describe, expect, it, vi } from "vitest"

import { CAMPAIGN_DETAIL_PREVIEW_COPY } from "@/lib/operatorCampaigns/campaignDetailPreviewPresentation"
import { createCampaignDetailPreviewModule } from "@/lib/operatorCampaigns/createCampaignDetailPreviewModule"
import type { CampaignDetailPreviewSource } from "@/lib/operatorCampaigns/createCampaignDetailPreviewModule"
import type { CatalogOfferDetail } from "@/types/operatorCampaigns"

function sampleCampaign(
  overrides: Partial<CampaignDetailPreviewSource> = {}
): CampaignDetailPreviewSource {
  return {
    id: 42,
    locationId: 7,
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

function sampleOffer(
  overrides: Partial<CatalogOfferDetail> = {}
): CatalogOfferDetail {
  return {
    id: 7,
    locationId: 7,
    status: "active",
    offerType: "percentage_discount",
    title: "10% off next visit",
    description: "Enjoy 10% off",
    validity: "30_days_after_issue",
    expiryDate: null,
    discountPercentage: 10,
    discountAmount: null,
    freeItemText: null,
    purchaseRequirement: null,
    minimumSpend: null,
    additionalExclusions: null,
    replacementItemText: null,
    staffInstructions: "Apply 10% off before payment.",
    issueCount: 0,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
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
      showOfferLogic: false,
      showAudienceEligibility: true,
      sendLogicLabel: CAMPAIGN_DETAIL_PREVIEW_COPY.notScheduled,
      editCampaignLabel: CAMPAIGN_DETAIL_PREVIEW_COPY.editCampaign,
      closeLabel: CAMPAIGN_DETAIL_PREVIEW_COPY.close,
    })
    expect(snapshot.viewModel).not.toHaveProperty("useThisTemplateLabel")
  })

  it("hides Offer logic and Audience eligibility when neither is configured", async () => {
    const loadCampaign = vi.fn(async () =>
      sampleCampaign({
        audienceKey: null,
        offerStance: null,
        offerId: null,
      })
    )
    const loadOffer = vi.fn(async () => sampleOffer())
    const loadAudienceEligibility = vi.fn(async () => ({
      matched: 20,
      currentlyEligible: 20,
      excluded: 0,
      emailEligible: 16,
      smsEligible: 4,
      excludedReasons: [],
      source: "live" as const,
    }))
    const module = createCampaignDetailPreviewModule({
      loadCampaign,
      loadOffer,
      loadAudienceEligibility,
    })

    await module.open(42)

    const viewModel = module.getSnapshot().viewModel
    expect(viewModel?.showOfferLogic).toBe(false)
    expect(viewModel?.showAudienceEligibility).toBe(false)
    expect(loadOffer).not.toHaveBeenCalled()
    expect(loadAudienceEligibility).not.toHaveBeenCalled()
  })

  it("shows Offer logic preview when an Offer is attached", async () => {
    const loadCampaign = vi.fn(async () =>
      sampleCampaign({
        audienceKey: null,
        offerStance: "existing-offer",
        offerId: 7,
        offerTitle: "10% off next visit",
      })
    )
    const loadOffer = vi.fn(async () => sampleOffer())
    const module = createCampaignDetailPreviewModule({
      loadCampaign,
      loadOffer,
    })

    await module.open(42)

    const viewModel = module.getSnapshot().viewModel
    expect(loadOffer).toHaveBeenCalledWith(7)
    expect(viewModel?.showOfferLogic).toBe(true)
    expect(viewModel?.offerLogic).toEqual([
      { label: CAMPAIGN_DETAIL_PREVIEW_COPY.offerType, value: "10% off" },
      {
        label: CAMPAIGN_DETAIL_PREVIEW_COPY.codeType,
        value: CAMPAIGN_DETAIL_PREVIEW_COPY.codeTypeValue,
      },
      {
        label: CAMPAIGN_DETAIL_PREVIEW_COPY.expiry,
        value: "30 days after issue",
      },
      {
        label: CAMPAIGN_DETAIL_PREVIEW_COPY.usage,
        value: CAMPAIGN_DETAIL_PREVIEW_COPY.usageValue,
      },
      {
        label: CAMPAIGN_DETAIL_PREVIEW_COPY.redemption,
        value: CAMPAIGN_DETAIL_PREVIEW_COPY.redemptionValue,
      },
    ])
    expect(viewModel?.showAudienceEligibility).toBe(false)
  })

  it("shows Audience eligibility when an audience is configured", async () => {
    const loadCampaign = vi.fn(async () =>
      sampleCampaign({
        offerStance: "no-offer",
        offerId: null,
      })
    )
    const loadAudienceEligibility = vi.fn(async () => ({
      matched: 20,
      currentlyEligible: 20,
      excluded: 0,
      emailEligible: 16,
      smsEligible: 4,
      excludedReasons: [],
      source: "live" as const,
    }))
    const module = createCampaignDetailPreviewModule({
      loadCampaign,
      loadAudienceEligibility,
    })

    await module.open(42)

    const viewModel = module.getSnapshot().viewModel
    expect(loadAudienceEligibility).toHaveBeenCalledWith({
      locationId: 7,
      audienceKey: "all-eligible-guests",
    })
    expect(viewModel?.showAudienceEligibility).toBe(true)
    expect(viewModel?.eligibility).toEqual({
      emailCount: 16,
      smsCount: 4,
      totalUniqueGuests: 20,
    })
    expect(viewModel?.showOfferLogic).toBe(false)
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

  it("shows the attached catalog Offer title instead of Existing offer", async () => {
    const loadCampaign = vi.fn(async () =>
      sampleCampaign({
        offerStance: "existing-offer",
        offerId: 7,
        offerTitle: "10% off next visit",
      })
    )
    const module = createCampaignDetailPreviewModule({ loadCampaign })

    await module.open(42)

    expect(module.getSnapshot().viewModel?.summary.offer).toBe(
      "10% off next visit"
    )
  })

  it("opens Preview from a loaded Campaign without fetching the campaign again", async () => {
    const loadCampaign = vi.fn(async () => sampleCampaign())
    const loadAudienceEligibility = vi.fn(async () => ({
      matched: 10,
      currentlyEligible: 10,
      excluded: 0,
      emailEligible: 8,
      smsEligible: 2,
      excludedReasons: [],
      source: "live" as const,
    }))
    const module = createCampaignDetailPreviewModule({
      loadCampaign,
      loadAudienceEligibility,
    })
    const campaign = sampleCampaign({ id: 41, name: "Win-back" })

    module.openLoaded(campaign)
    expect(module.getSnapshot().open).toBe(true)
    expect(module.getSnapshot().loadStatus).toBe("loading")

    await vi.waitFor(() => {
      expect(module.getSnapshot().loadStatus).toBe("loaded")
    })

    expect(loadCampaign).not.toHaveBeenCalled()
    expect(module.getSnapshot().viewModel).toMatchObject({
      campaignId: 41,
      title: "Win-back",
      showAudienceEligibility: true,
      eligibility: {
        emailCount: 8,
        smsCount: 2,
        totalUniqueGuests: 10,
      },
    })
  })
})
